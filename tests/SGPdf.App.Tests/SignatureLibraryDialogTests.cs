using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SGPdf.App.Features.Sign;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class SignatureLibraryDialogTests
{
    [Fact]
    public void Dialog_EmptyLibrary_ShowsEmptyStateAndCorrectButtonStates()
    {
        RunInSta(() => WithRoot(root =>
        {
            var dialog = new SignatureLibraryDialog(new SignatureLibraryStore(root), null);

            Assert.Equal("No hay firmas guardadas.", Text(dialog, "SignatureLibraryEmptyText").Text);
            Assert.True(Text(dialog, "SignatureLibraryEmptyText").Visibility == Visibility.Visible);
            Assert.False(Button(dialog, "SignatureLibraryUseButton").IsEnabled);
            Assert.False(Button(dialog, "SignatureLibrarySaveSelectedButton").IsEnabled);
            Assert.False(Button(dialog, "SignatureLibraryRenameButton").IsEnabled);
            Assert.False(Button(dialog, "SignatureLibraryDeleteButton").IsEnabled);
            Assert.True(Button(dialog, "SignatureLibraryCloseButton").IsEnabled);
            dialog.Close();
        }));
    }

    [Fact]
    public void Dialog_SaveSelectedDisabledWhenNoSelectedPlacementAsset()
    {
        RunInSta(() => WithRoot(root =>
        {
            var dialog = new SignatureLibraryDialog(new SignatureLibraryStore(root), null);
            Assert.False(Button(dialog, "SignatureLibrarySaveSelectedButton").IsEnabled);
            dialog.Close();
        }));
    }

    [Fact]
    public void Dialog_StructurallyInvalidManifest_DisablesMutationAndUseButCanClose()
    {
        RunInSta(() => WithRoot(root =>
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "library.json"), "{ invalid");
            var dialog = new SignatureLibraryDialog(new SignatureLibraryStore(root), CreateAsset());

            Assert.False(Button(dialog, "SignatureLibraryUseButton").IsEnabled);
            Assert.False(Button(dialog, "SignatureLibrarySaveSelectedButton").IsEnabled);
            Assert.False(Button(dialog, "SignatureLibraryRenameButton").IsEnabled);
            Assert.False(Button(dialog, "SignatureLibraryDeleteButton").IsEnabled);
            Assert.True(Button(dialog, "SignatureLibraryCloseButton").IsEnabled);
            Assert.Contains("biblioteca", Text(dialog, "SignatureLibraryStatusText").Text, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("{ invalid", File.ReadAllText(Path.Combine(root, "library.json")));
            dialog.Close();
        }));
    }

    [Fact]
    public void Dialog_HealthyEntry_ShowsNameAndThumbnail()
    {
        RunInSta(() => WithRoot(root =>
        {
            var store = new SignatureLibraryStore(root);
            var item = store.Add("Firma principal", CreateAsset());
            var dialog = new SignatureLibraryDialog(store, null);
            var list = List(dialog);

            var row = Assert.IsType<SignatureLibraryDialogItem>(Assert.Single(list.Items));
            Assert.Equal(item.Id, row.Id);
            Assert.Equal("Firma principal", row.DisplayName);
            Assert.True(row.IsAvailable);
            Assert.NotNull(row.Thumbnail);
            dialog.Close();
        }));
    }

    [Fact]
    public void Dialog_MissingPng_MarksOnlyThatItemUnavailableAndLeavesHealthyEntryUsable()
    {
        RunInSta(() => WithRoot(root =>
        {
            var store = new SignatureLibraryStore(root);
            var broken = store.Add("Rota", CreateAsset());
            var healthy = store.Add("Sana", CreateAsset());
            File.Delete(Path.Combine(root, broken.FileName));
            var dialog = new SignatureLibraryDialog(store, null);
            var list = List(dialog);

            var brokenRow = Assert.IsType<SignatureLibraryDialogItem>(list.Items[0]);
            var healthyRow = Assert.IsType<SignatureLibraryDialogItem>(list.Items[1]);
            Assert.False(brokenRow.IsAvailable);
            Assert.Null(brokenRow.Thumbnail);
            Assert.True(healthyRow.IsAvailable);
            Assert.NotNull(healthyRow.Thumbnail);

            list.SelectedIndex = 1;
            Assert.True(Button(dialog, "SignatureLibraryUseButton").IsEnabled);
            Assert.True(Button(dialog, "SignatureLibraryRenameButton").IsEnabled);
            Assert.True(Button(dialog, "SignatureLibraryDeleteButton").IsEnabled);
            Assert.Equal(healthy.Id, Assert.IsType<SignatureLibraryDialogItem>(list.SelectedItem).Id);
            dialog.Close();
        }));
    }

    [Fact]
    public void Dialog_CorruptPng_MarksOnlyThatItemUnavailable()
    {
        RunInSta(() => WithRoot(root =>
        {
            var store = new SignatureLibraryStore(root);
            var item = store.Add("Firma", CreateAsset());
            File.WriteAllBytes(Path.Combine(root, item.FileName), new byte[] { 1, 2, 3, 4 });
            var dialog = new SignatureLibraryDialog(store, null);

            var row = Assert.IsType<SignatureLibraryDialogItem>(Assert.Single(List(dialog).Items));
            Assert.False(row.IsAvailable);
            Assert.NotNull(row.UnavailableText);
            dialog.Close();
        }));
    }

    [Fact]
    public void Dialog_UseDisabledWithoutSelectionOrForBrokenItem()
    {
        RunInSta(() => WithRoot(root =>
        {
            var store = new SignatureLibraryStore(root);
            var item = store.Add("Firma", CreateAsset());
            File.Delete(Path.Combine(root, item.FileName));
            var dialog = new SignatureLibraryDialog(store, null);
            var list = List(dialog);

            Assert.False(Button(dialog, "SignatureLibraryUseButton").IsEnabled);
            list.SelectedIndex = 0;
            Assert.False(Button(dialog, "SignatureLibraryUseButton").IsEnabled);
            Assert.True(Button(dialog, "SignatureLibraryRenameButton").IsEnabled);
            Assert.True(Button(dialog, "SignatureLibraryDeleteButton").IsEnabled);
            dialog.Close();
        }));
    }

    [Fact]
    public void Dialog_SaveSelected_UsesSuppliedExactAssetRefreshesAndStaysOpen()
    {
        RunInSta(() => WithRoot(root =>
        {
            var store = new SignatureLibraryStore(root);
            var asset = CreateAsset();
            var dialog = new SignatureLibraryDialog(store, asset);
            SetField(dialog, "_promptName", (Func<string, string?, string?>)((_, _) => "  Mi firma  "));

            Click(dialog, "SignatureLibrarySaveSelectedButton");

            var saved = Assert.Single(store.Load());
            Assert.Equal("Mi firma", saved.DisplayName);
            var loaded = store.LoadAsset(saved.Id);
            Assert.Equal(asset.PixelWidth, loaded.PixelWidth);
            Assert.Equal(asset.PixelHeight, loaded.PixelHeight);
            Assert.Equal(asset.BgraPixels.ToArray(), loaded.BgraPixels.ToArray());
            Assert.Equal(saved.Id, Assert.IsType<SignatureLibraryDialogItem>(List(dialog).SelectedItem).Id);
            Assert.Null(dialog.SelectedAsset);
            dialog.Close();
        }));
    }

    [Fact]
    public void Dialog_SaveSelected_DuplicateName_KeepsExistingState()
    {
        RunInSta(() => WithRoot(root =>
        {
            var store = new SignatureLibraryStore(root);
            var existing = store.Add("Firma", CreateAsset());
            var dialog = new SignatureLibraryDialog(store, CreateAsset());
            SetField(dialog, "_promptName", (Func<string, string?, string?>)((_, _) => " firma "));

            Click(dialog, "SignatureLibrarySaveSelectedButton");

            Assert.Single(store.Load());
            Assert.Equal(existing.Id, store.Load()[0].Id);
            Assert.Contains("existe", Text(dialog, "SignatureLibraryStatusText").Text, StringComparison.OrdinalIgnoreCase);
            dialog.Close();
        }));
    }

    [Fact]
    public void Dialog_Rename_KeepsSelectedGuidAndDoesNotRewriteImage()
    {
        RunInSta(() => WithRoot(root =>
        {
            var store = new SignatureLibraryStore(root);
            var item = store.Add("Antes", CreateAsset());
            var pngPath = Path.Combine(root, item.FileName);
            var pngBytes = File.ReadAllBytes(pngPath);
            var dialog = new SignatureLibraryDialog(store, null);
            List(dialog).SelectedIndex = 0;
            SetField(dialog, "_promptName", (Func<string, string?, string?>)((_, current) => current == "Antes" ? "Después" : null));

            Click(dialog, "SignatureLibraryRenameButton");

            var renamed = Assert.Single(store.Load());
            Assert.Equal(item.Id, renamed.Id);
            Assert.Equal(item.FileName, renamed.FileName);
            Assert.Equal("Después", renamed.DisplayName);
            Assert.Equal(pngBytes, File.ReadAllBytes(pngPath));
            Assert.Equal(item.Id, Assert.IsType<SignatureLibraryDialogItem>(List(dialog).SelectedItem).Id);
            dialog.Close();
        }));
    }

    [Fact]
    public void Dialog_DeleteCancel_IsStateSafe()
    {
        RunInSta(() => WithRoot(root =>
        {
            var store = new SignatureLibraryStore(root);
            var item = store.Add("Firma", CreateAsset());
            var dialog = new SignatureLibraryDialog(store, null);
            List(dialog).SelectedIndex = 0;
            SetField(dialog, "_confirmDelete", (Func<string, bool>)(_ => false));

            Click(dialog, "SignatureLibraryDeleteButton");

            Assert.Single(store.Load());
            Assert.True(File.Exists(Path.Combine(root, item.FileName)));
            Assert.Single(List(dialog).Items);
            dialog.Close();
        }));
    }

    [Fact]
    public void Dialog_DeleteConfirmed_RemovesMetadataAndRefreshes()
    {
        RunInSta(() => WithRoot(root =>
        {
            var store = new SignatureLibraryStore(root);
            var item = store.Add("Firma", CreateAsset());
            var dialog = new SignatureLibraryDialog(store, null);
            List(dialog).SelectedIndex = 0;
            SetField(dialog, "_confirmDelete", (Func<string, bool>)(_ => true));

            Click(dialog, "SignatureLibraryDeleteButton");

            Assert.Empty(store.Load());
            Assert.False(File.Exists(Path.Combine(root, item.FileName)));
            Assert.Empty(List(dialog).Items);
            Assert.Equal(Visibility.Visible, Text(dialog, "SignatureLibraryEmptyText").Visibility);
            dialog.Close();
        }));
    }

    [Fact]
    public void Dialog_DeleteCleanupWarning_LeavesItemLogicallyRemovedAndShowsWarning()
    {
        RunInSta(() => WithRoot(root =>
        {
            var ops = new SignatureLibraryFileOps();
            var store = new SignatureLibraryStore(root, ops);
            var item = store.Add("Firma", CreateAsset());
            var dialog = new SignatureLibraryDialog(store, null);
            List(dialog).SelectedIndex = 0;
            SetField(dialog, "_confirmDelete", (Func<string, bool>)(_ => true));
            ops.DeleteFile = _ => throw new IOException("forced cleanup");

            Click(dialog, "SignatureLibraryDeleteButton");

            Assert.Empty(store.Load());
            Assert.True(File.Exists(Path.Combine(root, item.FileName)));
            Assert.Contains("PNG", Text(dialog, "SignatureLibraryStatusText").Text, StringComparison.OrdinalIgnoreCase);
            dialog.Close();
        }));
    }

    [Fact]
    public void Dialog_UseHealthyItem_ReturnsLoadedSignatureAsset()
    {
        RunInSta(() => WithRoot(root =>
        {
            var store = new SignatureLibraryStore(root);
            var asset = CreateAsset();
            store.Add("Firma", asset);
            var dialog = new SignatureLibraryDialog(store, null);
            List(dialog).SelectedIndex = 0;

            Click(dialog, "SignatureLibraryUseButton");

            Assert.NotNull(dialog.SelectedAsset);
            Assert.Equal(asset.BgraPixels.ToArray(), dialog.SelectedAsset!.BgraPixels.ToArray());
            dialog.Close();
        }));
    }

    [Fact]
    public void Dialog_UseTimeFailure_ReturnsNothingAndKeepsDialogManageable()
    {
        RunInSta(() => WithRoot(root =>
        {
            var store = new SignatureLibraryStore(root);
            var item = store.Add("Firma", CreateAsset());
            var dialog = new SignatureLibraryDialog(store, null);
            List(dialog).SelectedIndex = 0;
            File.Delete(Path.Combine(root, item.FileName));

            Click(dialog, "SignatureLibraryUseButton");

            Assert.Null(dialog.SelectedAsset);
            Assert.Single(List(dialog).Items);
            Assert.Contains("encontr", Text(dialog, "SignatureLibraryStatusText").Text, StringComparison.OrdinalIgnoreCase);
            dialog.Close();
        }));
    }

    [Fact]
    public void Dialog_CloseOrWindowX_ReturnsNoAsset()
    {
        RunInSta(() => WithRoot(root =>
        {
            var store = new SignatureLibraryStore(root);
            store.Add("Firma", CreateAsset());
            var dialog = new SignatureLibraryDialog(store, null);
            List(dialog).SelectedIndex = 0;

            Click(dialog, "SignatureLibraryCloseButton");
            Assert.Null(dialog.SelectedAsset);
            if (dialog.IsLoaded)
                dialog.Close();

            var second = new SignatureLibraryDialog(store, null);
            second.Close();
            Assert.Null(second.SelectedAsset);
        }));
    }

    [Fact]
    public void NameDialog_InitialValueAndAcceptExposeEnteredName()
    {
        RunInSta(() =>
        {
            var dialog = new SignatureNameDialog("Renombrar firma", "Inicial");
            var box = Assert.IsType<TextBox>(dialog.FindName("SignatureNameTextBox"));
            Assert.Equal("Inicial", box.Text);
            box.Text = "Nueva";
            Click(dialog, "SignatureNameAcceptButton");
            Assert.Equal("Nueva", dialog.EnteredName);
            if (dialog.IsLoaded)
                dialog.Close();
        });
    }

    private static SignatureAsset CreateAsset()
    {
        const int width = 3;
        const int height = 2;
        const int stride = width * 4;
        return new SignatureAsset(
            width,
            height,
            stride,
            new byte[]
            {
                0, 0, 0, 0,    10, 20, 30, 100,   40, 50, 60, 255,
                70, 80, 90, 50, 100, 110, 120, 200, 130, 140, 150, 0
            },
            "test.png");
    }

    private static ListBox List(SignatureLibraryDialog dialog)
        => Assert.IsType<ListBox>(dialog.FindName("SignatureLibraryItemsList"));

    private static Button Button(FrameworkElement dialog, string name)
        => Assert.IsType<Button>(dialog.FindName(name));

    private static TextBlock Text(FrameworkElement dialog, string name)
        => Assert.IsType<TextBlock>(dialog.FindName(name));

    private static void Click(FrameworkElement dialog, string name)
        => Button(dialog, name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void SetField(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(target, value);
    }

    private static void WithRoot(Action<string> action)
    {
        var root = Path.Combine(Path.GetTempPath(), $"sgpdf-signature-library-dialog-{Guid.NewGuid():N}");
        try
        {
            action(root);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static void RunInSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
