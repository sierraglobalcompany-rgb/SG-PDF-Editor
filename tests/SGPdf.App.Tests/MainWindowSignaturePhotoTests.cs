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
                Click(window, "SignModeButton");
                Assert.Equal(Visibility.Visible, panel.Visibility);
                Assert.True(button.IsEnabled);
            }
            finally
            {
                ClearDirtyState(window);
                window.Close();
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
                Click(window, "SignModeButton");
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
                window.Close();
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
                Click(window, "SignModeButton");
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
                window.Close();
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
                Click(window, "SignModeButton");
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
                window.Close();
            }
        });
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
