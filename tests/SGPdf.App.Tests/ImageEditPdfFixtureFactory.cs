using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SkiaSharp;

namespace SGPdf.App.Tests;

internal sealed class ImageEditPdfFixture : IDisposable
{
    internal ImageEditPdfFixture(string directoryPath, string path)
    {
        DirectoryPath = directoryPath;
        Path = path;
    }

    internal string DirectoryPath { get; }
    internal string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }
}

internal static class ImageEditPdfFixtureFactory
{
    internal static ImageEditPdfFixture CreateSingleImage()
    {
        var directory = NewDirectory();
        var imagePath = System.IO.Path.Combine(directory, "single.png");
        CreatePng(imagePath, 14, 9, SKColors.SeaGreen);

        var pdfPath = System.IO.Path.Combine(directory, "single-image.pdf");
        using (var document = CreateDocument(out var page))
        {
            using var graphics = XGraphics.FromPdfPage(page);
            using var image = XImage.FromFile(imagePath);
            graphics.DrawImage(image, 75, 90, 105, 68);
            document.Save(pdfPath);
        }

        return new ImageEditPdfFixture(directory, pdfPath);
    }

    internal static ImageEditPdfFixture CreateImageWithVectorNeighbor()
    {
        var directory = NewDirectory();
        var imagePath = System.IO.Path.Combine(directory, "source.png");
        CreatePng(imagePath, 16, 10, SKColors.CornflowerBlue);

        var pdfPath = System.IO.Path.Combine(directory, "image-vector.pdf");
        using (var document = CreateDocument(out var page))
        {
            using var graphics = XGraphics.FromPdfPage(page);
            using var image = XImage.FromFile(imagePath);
            graphics.DrawImage(image, 40, 60, 120, 75);
            graphics.DrawRectangle(XPens.Black, 180, 60, 60, 60);
            document.Save(pdfPath);
        }

        return new ImageEditPdfFixture(directory, pdfPath);
    }

    internal static ImageEditPdfFixture CreateMultipleImages()
    {
        var directory = NewDirectory();
        var firstPath = System.IO.Path.Combine(directory, "first.png");
        var secondPath = System.IO.Path.Combine(directory, "second.png");
        CreatePng(firstPath, 12, 8, SKColors.OrangeRed);
        CreatePng(secondPath, 10, 14, SKColors.RoyalBlue);

        var pdfPath = System.IO.Path.Combine(directory, "multiple-images.pdf");
        using (var document = CreateDocument(out var page))
        {
            using var graphics = XGraphics.FromPdfPage(page);
            using var first = XImage.FromFile(firstPath);
            using var second = XImage.FromFile(secondPath);
            graphics.DrawImage(first, 20, 30, 90, 60);
            graphics.DrawImage(second, 170, 210, 70, 98);
            document.Save(pdfPath);
        }

        return new ImageEditPdfFixture(directory, pdfPath);
    }

    internal static ImageEditPdfFixture CreateRotatedImage()
    {
        var directory = NewDirectory();
        var imagePath = System.IO.Path.Combine(directory, "rotated.png");
        CreatePng(imagePath, 20, 10, SKColors.MediumPurple);

        var pdfPath = System.IO.Path.Combine(directory, "rotated-image.pdf");
        using (var document = CreateDocument(out var page))
        {
            using var graphics = XGraphics.FromPdfPage(page);
            using var image = XImage.FromFile(imagePath);
            var state = graphics.Save();
            graphics.TranslateTransform(140, 160);
            graphics.RotateTransform(30);
            graphics.DrawImage(image, -60, -30, 120, 60);
            graphics.Restore(state);
            document.Save(pdfPath);
        }

        return new ImageEditPdfFixture(directory, pdfPath);
    }

    internal static ImageEditPdfFixture CreateOverlappingImagesAndVector()
    {
        var directory = NewDirectory();
        var redPath = System.IO.Path.Combine(directory, "red.png");
        var bluePath = System.IO.Path.Combine(directory, "blue.png");
        CreatePng(redPath, 16, 16, SKColors.Red);
        CreatePng(bluePath, 16, 16, SKColors.Blue);

        var pdfPath = System.IO.Path.Combine(directory, "overlap.pdf");
        using (var document = CreateDocument(out var page))
        {
            using var graphics = XGraphics.FromPdfPage(page);
            using var red = XImage.FromFile(redPath);
            using var blue = XImage.FromFile(bluePath);
            graphics.DrawImage(red, 60, 80, 140, 140);
            graphics.DrawRectangle(new XSolidBrush(XColors.Lime), 90, 110, 140, 140);
            graphics.DrawImage(blue, 120, 140, 140, 140);
            document.Save(pdfPath);
        }

        return new ImageEditPdfFixture(directory, pdfPath);
    }

    internal static ImageEditPdfFixture CreateNearEdgeImage()
    {
        var directory = NewDirectory();
        var imagePath = System.IO.Path.Combine(directory, "edge.png");
        CreatePng(imagePath, 9, 13, SKColors.Goldenrod);

        var pdfPath = System.IO.Path.Combine(directory, "near-edge.pdf");
        using (var document = CreateDocument(out var page))
        {
            using var graphics = XGraphics.FromPdfPage(page);
            using var image = XImage.FromFile(imagePath);
            graphics.DrawImage(image, 265, 355, 50, 65);
            document.Save(pdfPath);
        }

        return new ImageEditPdfFixture(directory, pdfPath);
    }

    private static PdfDocument CreateDocument(out PdfPage page)
    {
        var document = new PdfDocument();
        page = document.AddPage();
        page.Width = XUnit.FromPoint(300);
        page.Height = XUnit.FromPoint(400);
        return document;
    }

    private static void CreatePng(string path, int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    private static string NewDirectory()
    {
        var directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"sgpdf-f6-image-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
