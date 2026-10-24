using System.Collections;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Reader;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowImageSelectionPixelCompositionTests
{
    private const int ChannelTolerance = 3;

    [Fact]
    public void SelectingImage_RealPdfiumCompositionDoesNotTintInterior_AndNegativeControlIsDetected()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                OrganizeWindowTestHost.SetField(
                    window,
                    "_recentPdfStore",
                    new RecentPdfStore(System.IO.Path.Combine(fixture.DirectoryPath, "recents")));
                OrganizeWindowTestHost.EnsureLoaded(window);
                OrganizeWindowTestHost.SetField(window, "_thumbnailRefreshQueued", true);
                Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryOpenPdfPathAsync", fixture.Path, null));
                OrganizeWindowTestHost.SetField(window, "_thumbnailRefreshQueued", false);
                OrganizeWindowTestHost.SetField(window, "_getCryptographicSignatureCount", (Func<PdfDocumentSession, int>)(_ => 0));

                Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterEditModeAsync"));

                var pdfImage = OrganizeWindowTestHost.Element<System.Windows.Controls.Image>(window, "PdfImage");
                var source = Assert.IsAssignableFrom<BitmapSource>(pdfImage.Source);
                Assert.True(source.PixelWidth > 100 && source.PixelHeight > 100,
                    $"Expected the actual PDFium page bitmap, got {source.PixelWidth}x{source.PixelHeight}.");
                var renderer = Assert.IsType<Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage>>(
                    OrganizeWindowTestHost.GetField(window, "_renderReaderPage"));

                var pageGrid = Assert.IsType<Grid>(pdfImage.Parent);
                LayoutOffscreen(pageGrid, window);
                var before = Capture(pageGrid);
                SaveRender("00-before-selection.png", before);
                var imageInfo = Assert.IsType<PdfImageObjectInfo>(
                    ((IEnumerable)OrganizeWindowTestHost.GetField(window, "_activeImageObjects")!).Cast<object>().Single());
                var transform = Assert.IsType<PdfPageDeviceTransform>(
                    OrganizeWindowTestHost.GetField(window, "_currentPdfDeviceTransform"));
                var pdfCenter = ImageEditCommandTestHost.PointAt(imageInfo.Matrix, .5d, .5d);
                var deviceCenter = ImageHitTester.PdfToDevice(pdfCenter, transform);
                var imageOrigin = pdfImage.TranslatePoint(new Point(0d, 0d), pageGrid);
                var samplePoint = new Point(imageOrigin.X + deviceCenter.X, imageOrigin.Y + deviceCenter.Y);
                var beforePixel = ReadPixel(before, samplePoint);
                Assert.True(beforePixel.G > beforePixel.R && beforePixel.G > beforePixel.B,
                    $"The center of the fixture image should remain SeaGreen; sampled {Format(beforePixel)}.");

                Assert.True((bool)OrganizeWindowTestHost.Invoke(
                    window,
                    "SelectImageAtDevicePoint",
                    deviceCenter.X,
                    deviceCenter.Y)!);
                var overlay = OrganizeWindowTestHost.Element<Canvas>(window, "EditOverlayCanvas");
                var outline = Assert.Single(overlay.Children.OfType<Polygon>());
                var selected = Capture(pageGrid);
                SaveRender("01-after-selection.png", selected);
                var selectedPixel = ReadPixel(selected, samplePoint);
                AssertChannelsNear(beforePixel, selectedPixel, ChannelTolerance, "normal selection overlay");

                var overlayOrigin = overlay.TranslatePoint(new Point(0d, 0d), pageGrid);
                var topLeft = outline.Points[3];
                var topRight = outline.Points[2];
                var edgeMidpoint = new Point(
                    overlayOrigin.X + (topLeft.X + topRight.X) / 2d,
                    overlayOrigin.Y + (topLeft.Y + topRight.Y) / 2d);
                var blueEdgeSamples = CountBluePixelsNear(selected, edgeMidpoint, radius: 3);
                Assert.True(blueEdgeSamples > 0, "The selected image's actual WPF outline was not visible in the composed bitmap.");

                var originalFill = outline.Fill;
                outline.Fill = new SolidColorBrush(Color.FromArgb(96, 0, 85, 255));
                var negativeControl = Capture(pageGrid);
                SaveRender("02-negative-blue-fill.png", negativeControl);
                var negativePixel = ReadPixel(negativeControl, samplePoint);
                Assert.True(ColorDistance(beforePixel, negativePixel) > 50,
                    $"The in-memory blue-fill negative control did not change the image center enough: before {Format(beforePixel)}, after {Format(negativePixel)}.");
                outline.Fill = originalFill;

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "RotateSelectedImage", 90d)!);
                var rotatedOutline = Assert.Single(overlay.Children.OfType<Polygon>());
                Assert.NotSame(outline, rotatedOutline);
                Assert.Same(Brushes.Transparent, rotatedOutline.Fill);
                var rotated = Capture(pageGrid);
                SaveRender("03-after-rotation.png", rotated);
                var rotatedPixel = ReadPixel(rotated, samplePoint);
                AssertChannelsNear(beforePixel, rotatedPixel, ChannelTolerance, "rotation overlay rebuild");

                OrganizeWindowTestHost.Invoke(window, "HandleImageEditEscape");
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_selectedImageKey"));
                Assert.Empty(overlay.Children);
                var escaped = Capture(pageGrid);
                SaveRender("04-after-escape.png", escaped);
                var escapedPixel = ReadPixel(escaped, samplePoint);
                AssertChannelsNear(beforePixel, escapedPixel, ChannelTolerance, "selection cleared with Escape");

                SaveEvidence(new PixelEvidence(
                    Environment.OSVersion.VersionString,
                    Environment.Version.ToString(),
                    renderer.Method.ToString() ?? renderer.Method.Name,
                    source.PixelWidth,
                    source.PixelHeight,
                    before.PixelWidth,
                    before.PixelHeight,
                    samplePoint.X,
                    samplePoint.Y,
                    Format(beforePixel),
                    Format(selectedPixel),
                    Format(negativePixel),
                    Format(rotatedPixel),
                    Format(escapedPixel),
                    ColorDistance(beforePixel, selectedPixel),
                    ColorDistance(beforePixel, negativePixel),
                    blueEdgeSamples,
                    ChannelTolerance));
            }
            finally
            {
                ImageEditCommandTestHost.CloseClean(window);
            }
        });
    }

    private static void LayoutOffscreen(Grid pageGrid, MainWindow window)
    {
        window.Width = 1280d;
        window.Height = 900d;
        if (window.Content is not FrameworkElement root)
            throw new InvalidOperationException("MainWindow content is not a layout root.");

        root.Measure(new Size(1280d, 900d));
        root.Arrange(new Rect(0d, 0d, 1280d, 900d));
        root.UpdateLayout();
        window.UpdateLayout();
        pageGrid.UpdateLayout();
        Assert.True(pageGrid.RenderSize.Width > 0d && pageGrid.RenderSize.Height > 0d,
            $"Page grid has no offscreen render size: {pageGrid.RenderSize}.");
    }

    private static RenderTargetBitmap Capture(Grid pageGrid)
    {
        pageGrid.UpdateLayout();
        var width = (int)Math.Ceiling(pageGrid.RenderSize.Width);
        var height = (int)Math.Ceiling(pageGrid.RenderSize.Height);
        Assert.True(width > 0 && height > 0, $"Invalid composition size {width}x{height}.");
        var bitmap = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Pbgra32);
        bitmap.Render(pageGrid);
        bitmap.Freeze();
        return bitmap;
    }

    private static Color ReadPixel(BitmapSource bitmap, Point point)
    {
        var x = (int)Math.Round(point.X, MidpointRounding.AwayFromZero);
        var y = (int)Math.Round(point.Y, MidpointRounding.AwayFromZero);
        Assert.InRange(x, 0, bitmap.PixelWidth - 1);
        Assert.InRange(y, 0, bitmap.PixelHeight - 1);
        var pixel = new byte[4];
        bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
        return Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
    }

    private static int CountBluePixelsNear(BitmapSource bitmap, Point point, int radius)
    {
        var centerX = (int)Math.Round(point.X, MidpointRounding.AwayFromZero);
        var centerY = (int)Math.Round(point.Y, MidpointRounding.AwayFromZero);
        var count = 0;
        for (var y = Math.Max(0, centerY - radius); y <= Math.Min(bitmap.PixelHeight - 1, centerY + radius); y++)
        for (var x = Math.Max(0, centerX - radius); x <= Math.Min(bitmap.PixelWidth - 1, centerX + radius); x++)
        {
            var color = ReadPixel(bitmap, new Point(x, y));
            if (color.B > 210 && color.G > 70 && color.G < 210 && color.R < 120)
                count++;
        }
        return count;
    }

    private static void AssertChannelsNear(Color expected, Color actual, int tolerance, string phase)
    {
        Assert.True(Math.Abs(expected.R - actual.R) <= tolerance &&
                    Math.Abs(expected.G - actual.G) <= tolerance &&
                    Math.Abs(expected.B - actual.B) <= tolerance &&
                    Math.Abs(expected.A - actual.A) <= tolerance,
            $"{phase} changed the sampled interior from {Format(expected)} to {Format(actual)} (tolerance {tolerance} per channel).");
    }

    private static int ColorDistance(Color left, Color right)
        => Math.Abs(left.R - right.R) + Math.Abs(left.G - right.G) + Math.Abs(left.B - right.B);

    private static string Format(Color color)
        => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    private static string? GetEvidenceDirectory()
    {
        var directory = Environment.GetEnvironmentVariable("SGPDF_R6_PIXEL_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(directory))
            return null;

        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void SaveRender(string name, RenderTargetBitmap render)
    {
        var directory = GetEvidenceDirectory();
        if (directory is null)
            return;

        using var stream = File.Create(System.IO.Path.Combine(directory, name));
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(render));
        encoder.Save(stream);
    }

    private static void SaveEvidence(PixelEvidence evidence)
    {
        var directory = GetEvidenceDirectory();
        if (directory is null)
            return;
        File.WriteAllText(
            System.IO.Path.Combine(directory, "pixel-results.json"),
            JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed record PixelEvidence(
        string OperatingSystem,
        string Runtime,
        string ReaderRenderer,
        int PdfiumSourceWidth,
        int PdfiumSourceHeight,
        int CompositionWidth,
        int CompositionHeight,
        double SampleX,
        double SampleY,
        string Before,
        string AfterSelection,
        string NegativeBlueFill,
        string AfterRotation,
        string AfterEscape,
        int SelectionDistance,
        int NegativeControlDistance,
        int VisibleBlueOutlinePixelCount,
        int PerChannelTolerance);
}
