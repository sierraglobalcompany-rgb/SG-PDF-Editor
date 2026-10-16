using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using PdfSharp.Pdf;
using SGPdf.App.Features.Sign;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowSignaturePhotoTests
{
    [Fact]
    public void CreateFromPhoto_ActionExistsAndIsVisibleOnlyWithFirmarPanel()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                var button = Assert.IsType<Button>(window.FindName("CreateSignatureFromPhotoButton"));
                var panel = Assert.IsType<StackPanel>(window.FindName("SignaturePropertiesPanel"));
                Assert.Equal(Visibility.Collapsed, panel.Visibility);

                AttachPdfWorkspace(window, fixture.SourcePath);
                Assert.True(OrganizeWindowTestHost.InvokeTaskSync<bool>(window, "TryEnterSignModeAsync"));
                Assert.Equal(Visibility.Visible, panel.Visibility);
                Assert.True(button.IsEnabled);
            }
            finally
            {
                ClearDirtyState(window);
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void PhotoCancel_ReturnsNoAssetAndKeepsPriorPlacementSelectionAndDirtyState()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                AttachPdfWorkspace(window, fixture.SourcePath);
                Assert.True(OrganizeWindowTestHost.InvokeTaskSync<bool>(window, "TryEnterSignModeAsync"));
                Invoke(window, "AddSignatureAsset", CreateAsset("existing.png"));
                var state = GetState(window)!;
                var before = state.Placements.ToArray();
                var selected = state.SelectedId;
                var dirty = state.IsDirty;

                SetField(window, "_prepareSignaturePhoto", (Func<Window, string, SignatureAsset?>)((_, _) => null));
                var result = (bool)Invoke(window, "TryPrepareSignaturePhotoFromPath", "photo.jpg")!;

                Assert.False(result);
                Assert.Equal(before, state.Placements);
                Assert.Equal(selected, state.SelectedId);
                Assert.Equal(dirty, state.IsDirty);
            }
            finally
            {
                ClearDirtyState(window);
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void PhotoFailure_KeepsPriorPlacementSelectionAndDirtyState()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                AttachPdfWorkspace(window, fixture.SourcePath);
                Assert.True(OrganizeWindowTestHost.InvokeTaskSync<bool>(window, "TryEnterSignModeAsync"));
                Invoke(window, "AddSignatureAsset", CreateAsset("existing.png"));
                var state = GetState(window)!;
                var before = state.Placements.ToArray();
                var selected = state.SelectedId;
                var dirty = state.IsDirty;

                SetField(window, "_prepareSignaturePhoto",
                    (Func<Window, string, SignatureAsset?>)((_, _) => throw new InvalidDataException("forced")));
                var result = (bool)Invoke(window, "TryPrepareSignaturePhotoFromPath", "photo.jpg")!;

                Assert.False(result);
                Assert.Equal(before, state.Placements);
                Assert.Equal(selected, state.SelectedId);
                Assert.Equal(dirty, state.IsDirty);
            }
            finally
            {
                ClearDirtyState(window);
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void PhotoApply_AddsExactlyOneNormalSelectedDirtyPlacement()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                AttachPdfWorkspace(window, fixture.SourcePath);
                Assert.True(OrganizeWindowTestHost.InvokeTaskSync<bool>(window, "TryEnterSignModeAsync"));
                var prepared = CreateAsset("prepared.jpg");
                SetField(window, "_prepareSignaturePhoto", (Func<Window, string, SignatureAsset?>)((_, _) => prepared));

                Assert.True((bool)Invoke(window, "TryPrepareSignaturePhotoFromPath", "photo.jpg")!);
                var state = GetState(window)!;
                var placement = Assert.Single(state.Placements);
                Assert.Same(prepared, placement.Asset);
                Assert.Equal(placement.Id, state.SelectedId);
                Assert.True(state.IsDirty);
            }
            finally
            {
                ClearDirtyState(window);
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    private static void AttachPdfWorkspace(MainWindow window, string path)
    {
        Assert.True(OrganizeWindowTestHost.OpenReader(window, path));
        SetField(window, "_currentPdfDeviceTransform",
            new PdfPageDeviceTransform(0d, 300d, 1d, 0d, 0d, -1d, 300, 300));
    }

    private static SignatureAsset CreateAsset(string name)
    {
        var pixels = new byte[200 * 100 * 4];
        for (var index = 3; index < pixels.Length; index += 4)
            pixels[index] = 255;
        pixels[3] = 0;
        return new SignatureAsset(200, 100, 800, pixels, name);
    }

    private static SignatureEditState? GetState(MainWindow window)
        => (SignatureEditState?)GetField(window, "_signatureEditState");

    private static void ClearDirtyState(MainWindow window) => GetState(window)?.DiscardAll();

    private static object? Invoke(MainWindow window, string method, params object?[] args)
    {
        var candidate = typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(item => item.Name == method && item.GetParameters().Length == args.Length);
        return candidate.Invoke(window, args);
    }

    private static object? GetField(MainWindow window, string name)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field.GetValue(window);
    }

    private static void SetField(MainWindow window, string name, object value)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(window, value);
    }

    private static void RunInSta(Action action)
        => OrganizeWindowTestHost.RunInSta(action);

    private sealed class PdfFixture : IDisposable
    {
        private PdfFixture(string directory, string sourcePath)
        {
            DirectoryPath = directory;
            SourcePath = sourcePath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }

        internal static PdfFixture Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-sign-photo-ui-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = Path.Combine(directory, "source.pdf");
            var document = new PdfDocument();
            var page = document.AddPage();
            page.Width = PdfSharp.Drawing.XUnit.FromPoint(300);
            page.Height = PdfSharp.Drawing.XUnit.FromPoint(300);
            document.Save(source);
            document.Close();
            return new PdfFixture(directory, source);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, true);
        }
    }
}
