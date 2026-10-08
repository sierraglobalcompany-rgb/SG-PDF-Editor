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

public sealed class MainWindowSignatureDrawTests
{
    [Fact]
    public void DrawSignature_ActionExistsAndIsVisibleOnlyWithFirmarPanel()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                var panel = Assert.IsType<StackPanel>(window.FindName("SignaturePropertiesPanel"));
                Assert.Null(window.FindName("DrawSignatureButton"));
                AttachPdfWorkspace(window, fixture.SourcePath);
                Click(window, "SignModeButton");
                Assert.Equal(Visibility.Visible, panel.Visibility);
                Assert.IsType<Button>(window.FindName("DrawSignatureButton"));
            }
            finally
            {
                ClearDirtyState(window);
                window.Close();
            }
        });
    }

    [Fact]
    public void DrawCancel_KeepsPriorPlacementSelectionAndDirtyState()
    {
        RunInSta(() =>
        {
            var window = CreateWindowWithExistingSignature(out var fixture, out var state);
            using (fixture)
            try
            {
                var before = state.Placements.ToArray();
                var selected = state.SelectedId;
                var dirty = state.IsDirty;
                SetField(window, "_drawSignature", (Func<Window, SignatureAsset?>)(_ => null));

                Assert.False((bool)Invoke(window, "TryDrawSignature")!);
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
    public void DrawFailure_KeepsPriorPlacementSelectionAndDirtyState()
    {
        RunInSta(() =>
        {
            var window = CreateWindowWithExistingSignature(out var fixture, out var state);
            using (fixture)
            try
            {
                var before = state.Placements.ToArray();
                var selected = state.SelectedId;
                var dirty = state.IsDirty;
                SetField(window, "_drawSignature", (Func<Window, SignatureAsset?>)(_ => throw new InvalidOperationException("forced")));

                Assert.False((bool)Invoke(window, "TryDrawSignature")!);
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
    public void DrawApply_AddsExactlyOneNormalSelectedDirtyPlacement()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                AttachPdfWorkspace(window, fixture.SourcePath);
                Click(window, "SignModeButton");
                var drawn = CreateAsset("drawn-signature");
                SetField(window, "_drawSignature", (Func<Window, SignatureAsset?>)(_ => drawn));

                Assert.True((bool)Invoke(window, "TryDrawSignature")!);
                var state = GetState(window)!;
                var placement = Assert.Single(state.Placements);
                Assert.Same(drawn, placement.Asset);
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
    public void DrawApply_UsesSameAddSignatureAssetFlowAsOtherSources()
    {
        RunInSta(() =>
        {
            var drawnWindow = new MainWindow();
            var directWindow = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                AttachPdfWorkspace(drawnWindow, fixture.SourcePath);
                AttachPdfWorkspace(directWindow, fixture.SourcePath);
                Click(drawnWindow, "SignModeButton");
                Click(directWindow, "SignModeButton");
                var asset = CreateAsset("same.png");

                SetField(drawnWindow, "_drawSignature", (Func<Window, SignatureAsset?>)(_ => asset));
                Assert.True((bool)Invoke(drawnWindow, "TryDrawSignature")!);
                Invoke(directWindow, "AddSignatureAsset", asset);

                var drawnPlacement = Assert.Single(GetState(drawnWindow)!.Placements);
                var directPlacement = Assert.Single(GetState(directWindow)!.Placements);
                Assert.Equal(directPlacement.Bounds, drawnPlacement.Bounds);
                Assert.Equal(directPlacement.PageIndex, drawnPlacement.PageIndex);
                Assert.Same(asset, drawnPlacement.Asset);
            }
            finally
            {
                ClearDirtyState(drawnWindow);
                ClearDirtyState(directWindow);
                drawnWindow.Close();
                directWindow.Close();
            }
        });
    }

    [Fact]
    public void ExistingPngAndPhotoSourceActionsRemainAvailable()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var panel = Assert.IsType<StackPanel>(window.FindName("SignaturePropertiesPanel"));
                Assert.Contains(panel.Children.OfType<Button>(), button => Equals(button.Content, "Cargar PNG transparente..."));
                Assert.IsType<Button>(window.FindName("CreateSignatureFromPhotoButton"));
                Assert.IsType<Button>(window.FindName("DrawSignatureButton"));
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static MainWindow CreateWindowWithExistingSignature(out PdfFixture fixture, out SignatureEditState state)
    {
        var window = new MainWindow();
        fixture = PdfFixture.Create();
        AttachPdfWorkspace(window, fixture.SourcePath);
        Click(window, "SignModeButton");
        Invoke(window, "AddSignatureAsset", CreateAsset("existing.png"));
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
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-sign-draw-ui-{Guid.NewGuid():N}");
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
