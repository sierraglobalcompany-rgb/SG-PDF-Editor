using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PdfSharp.Pdf;
using SGPdf.App.Features.Sign;
using SGPdf.App.Navigation;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowSignatureLibraryTests
{
    [Fact]
    public void LibraryAction_ExistsExactlyOnceInFirmarPanel()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var panel = Assert.IsType<StackPanel>(window.FindName("SignaturePropertiesPanel"));
                var named = Assert.IsType<Button>(window.FindName("SignatureLibraryButton"));
                var matches = panel.Children.OfType<Button>()
                    .Where(button => Equals(button.Content, "Biblioteca de firmas..."))
                    .ToArray();

                Assert.Single(matches);
                Assert.Same(named, matches[0]);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LibraryOpenClose_PreservesPriorPlacementsSelectionAndDirtyState()
    {
        RunInSta(() =>
        {
            var window = CreateWindowWithExistingSignature(out var fixture, out var state, out _);
            using (fixture)
            try
            {
                var before = state.Placements.ToArray();
                var selected = state.SelectedId;
                var dirty = state.IsDirty;
                SetField(window, "_openSignatureLibrary",
                    (Func<Window, SignatureAsset?, SignatureAsset?>)((_, _) => null));

                Assert.False((bool)Invoke(window, "TryOpenSignatureLibrary")!);
                Assert.Equal(before, state.Placements);
                Assert.Equal(selected, state.SelectedId);
                Assert.Equal(dirty, state.IsDirty);
            }
            finally
            {
                ClearDirtyState(window);
                window.Close();
            }
        });
    }

    [Fact]
    public void LibraryOpen_ReceivesExactSelectedPlacementAsset_NotPlacementRasterization()
    {
        RunInSta(() =>
        {
            var window = CreateWindowWithExistingSignature(out var fixture, out _, out var existing);
            using (fixture)
            try
            {
                SignatureAsset? received = null;
                SetField(window, "_openSignatureLibrary",
                    (Func<Window, SignatureAsset?, SignatureAsset?>)((owner, selected) =>
                    {
                        Assert.Same(window, owner);
                        received = selected;
                        return null;
                    }));

                Assert.False((bool)Invoke(window, "TryOpenSignatureLibrary")!);
                Assert.Same(existing, received);
            }
            finally
            {
                ClearDirtyState(window);
                window.Close();
            }
        });
    }

    [Fact]
    public void LibraryOpen_WithNoSelectedPlacement_ReceivesNull()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                AttachPdfWorkspace(window, fixture.SourcePath);
                Click(window, "SignModeButton");
                var called = false;
                SetField(window, "_openSignatureLibrary",
                    (Func<Window, SignatureAsset?, SignatureAsset?>)((owner, selected) =>
                    {
                        called = true;
                        Assert.Same(window, owner);
                        Assert.Null(selected);
                        return null;
                    }));

                Assert.False((bool)Invoke(window, "TryOpenSignatureLibrary")!);
                Assert.True(called);
                Assert.Null(GetState(window));
            }
            finally
            {
                ClearDirtyState(window);
                window.Close();
            }
        });
    }

    [Fact]
    public void LibraryFailure_PreservesPriorPdfSignatureState()
    {
        RunInSta(() =>
        {
            var window = CreateWindowWithExistingSignature(out var fixture, out var state, out _);
            using (fixture)
            try
            {
                var before = state.Placements.ToArray();
                var selected = state.SelectedId;
                var dirty = state.IsDirty;
                SetField(window, "_openSignatureLibrary",
                    (Func<Window, SignatureAsset?, SignatureAsset?>)((_, _) => throw new IOException("forced library failure")));

                Assert.False((bool)Invoke(window, "TryOpenSignatureLibrary")!);
                Assert.Equal(before, state.Placements);
                Assert.Equal(selected, state.SelectedId);
                Assert.Equal(dirty, state.IsDirty);
                Assert.Contains("biblioteca", GetStatusText(window).Text, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                ClearDirtyState(window);
                window.Close();
            }
        });
    }

    [Fact]
    public void LibraryUse_AddsExactlyOneNormalCenteredSelectedDirtyPlacement()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                AttachPdfWorkspace(window, fixture.SourcePath);
                Click(window, "SignModeButton");
                var asset = CreateAsset("library.png");
                var calls = 0;
                SetField(window, "_openSignatureLibrary",
                    (Func<Window, SignatureAsset?, SignatureAsset?>)((_, selected) =>
                    {
                        calls++;
                        Assert.Null(selected);
                        return asset;
                    }));

                Assert.True((bool)Invoke(window, "TryOpenSignatureLibrary")!);
                Assert.Equal(1, calls);
                var state = GetState(window)!;
                var placement = Assert.Single(state.Placements);
                Assert.Same(asset, placement.Asset);
                Assert.Equal(placement.Id, state.SelectedId);
                Assert.True(state.IsDirty);
            }
            finally
            {
                ClearDirtyState(window);
                window.Close();
            }
        });
    }

    [Fact]
    public void LibraryUse_UsesSameAddSignatureAssetGeometryAsDirectSource()
    {
        RunInSta(() =>
        {
            var libraryWindow = new MainWindow();
            var directWindow = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                AttachPdfWorkspace(libraryWindow, fixture.SourcePath);
                AttachPdfWorkspace(directWindow, fixture.SourcePath);
                Click(libraryWindow, "SignModeButton");
                Click(directWindow, "SignModeButton");
                var asset = CreateAsset("same-library.png");

                SetField(libraryWindow, "_openSignatureLibrary",
                    (Func<Window, SignatureAsset?, SignatureAsset?>)((_, _) => asset));
                Assert.True((bool)Invoke(libraryWindow, "TryOpenSignatureLibrary")!);
                Invoke(directWindow, "AddSignatureAsset", asset);

                var libraryPlacement = Assert.Single(GetState(libraryWindow)!.Placements);
                var directPlacement = Assert.Single(GetState(directWindow)!.Placements);
                Assert.Equal(directPlacement.Bounds, libraryPlacement.Bounds);
                Assert.Equal(directPlacement.PageIndex, libraryPlacement.PageIndex);
                Assert.Same(asset, libraryPlacement.Asset);
            }
            finally
            {
                ClearDirtyState(libraryWindow);
                ClearDirtyState(directWindow);
                libraryWindow.Close();
                directWindow.Close();
            }
        });
    }

    [Fact]
    public void LibraryRenameDeleteDoNotMutateAlreadyPlacedAsset()
    {
        RunInSta(() => WithRoot(root =>
        {
            var window = CreateWindowWithExistingSignature(out var fixture, out var state, out var placedAsset);
            using (fixture)
            try
            {
                var store = new SignatureLibraryStore(root);
                var item = store.Add("Guardada", placedAsset);
                store.Rename(item.Id, "Renombrada");
                store.Delete(item.Id);

                var placement = Assert.Single(state.Placements);
                Assert.Same(placedAsset, placement.Asset);
                Assert.Equal(placedAsset.BgraPixels.ToArray(), placement.Asset.BgraPixels.ToArray());
                Assert.Equal(placement.Id, state.SelectedId);
                Assert.True(state.IsDirty);
            }
            finally
            {
                ClearDirtyState(window);
                window.Close();
            }
        }));
    }

    [Fact]
    public void ExistingPngPhotoDrawActionsRemainAvailableAndUnchanged()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var panel = Assert.IsType<StackPanel>(window.FindName("SignaturePropertiesPanel"));
                Assert.Contains(panel.Children.OfType<Button>(), button => Equals(button.Content, "Cargar PNG transparente..."));
                Assert.Equal("Crear desde foto...", Assert.IsType<Button>(window.FindName("CreateSignatureFromPhotoButton")).Content);
                Assert.Equal("Dibujar firma...", Assert.IsType<Button>(window.FindName("DrawSignatureButton")).Content);
                Assert.Equal("Biblioteca de firmas...", Assert.IsType<Button>(window.FindName("SignatureLibraryButton")).Content);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static MainWindow CreateWindowWithExistingSignature(
        out PdfFixture fixture,
        out SignatureEditState state,
        out SignatureAsset asset)
    {
        var window = new MainWindow();
        fixture = PdfFixture.Create();
        AttachPdfWorkspace(window, fixture.SourcePath);
        Click(window, "SignModeButton");
        asset = CreateAsset("existing-library.png");
        Invoke(window, "AddSignatureAsset", asset);
        state = GetState(window)!;
        return window;
    }

    private static void AttachPdfWorkspace(MainWindow window, string path)
    {
        var session = PdfDocumentSession.Open(path);
        SetField(window, "_session", session);
        SetField(window, "_navigation", new PageNavigationState(1));
        SetField(window, "_currentPdfDeviceTransform",
            new PdfPageDeviceTransform(0d, 300d, 1d, 0d, 0d, -1d, 300, 300));

        var pixels = new byte[300 * 300 * 4];
        Array.Fill(pixels, (byte)255);
        var bitmap = BitmapSource.Create(300, 300, 96, 96, PixelFormats.Bgra32, null, pixels, 300 * 4);
        bitmap.Freeze();
        var image = Assert.IsType<Image>(window.FindName("PdfImage"));
        image.Source = bitmap;
        image.Width = 300;
        image.Height = 300;
        image.Visibility = Visibility.Visible;
        Invoke(window, "UpdateViewerControlsUi");
    }

    private static SignatureAsset CreateAsset(string name)
    {
        var pixels = new byte[200 * 100 * 4];
        for (var index = 3; index < pixels.Length; index += 4)
            pixels[index] = 255;
        pixels[3] = 0;
        return new SignatureAsset(200, 100, 800, pixels, name);
    }

    private static TextBlock GetStatusText(MainWindow window)
        => Assert.IsType<TextBlock>(window.FindName("StatusText"));

    private static void Click(MainWindow window, string name)
        => Assert.IsType<Button>(window.FindName(name)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static SignatureEditState? GetState(MainWindow window)
        => (SignatureEditState?)GetField(window, "_signatureEditState");

    private static void ClearDirtyState(MainWindow window) => GetState(window)?.DiscardAll();

    private static object? Invoke(MainWindow window, string method, params object?[] args)
    {
        var candidates = typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(item => item.Name == method && item.GetParameters().Length == args.Length)
            .ToArray();
        Assert.Single(candidates);
        return candidates[0].Invoke(window, args);
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

    private static void WithRoot(Action<string> action)
    {
        var root = Path.Combine(Path.GetTempPath(), $"sgpdf-sign-library-main-{Guid.NewGuid():N}");
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
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-sign-library-ui-{Guid.NewGuid():N}");
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
