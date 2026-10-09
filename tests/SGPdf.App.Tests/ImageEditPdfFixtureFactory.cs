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
    internal static ImageEditPdfFixture CreateImageWithVectorNeighbor()
    {
        var directory = NewDirectory();
        var imagePath = System.IO.Path.Combine(directory, "source.png");
        CreatePng(imagePath, 16, 10);

        var pdfPath = System.IO.Path.Combine(directory, "image-vector.pdf");
        using (var document = new PdfDocument())
        {
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(300);
            page.Height = XUnit.FromPoint(400);

            using var graphics = XGraphics.FromPdfPage(page);
            using var image = XImage.FromFile(imagePath);
            graphics.DrawImage(image, 40, 60, 120, 75);
            graphics.DrawRectangle(XPens.Black, 180, 60, 60, 60);
            document.Save(pdfPath);
        }

        return new ImageEditPdfFixture(directory, pdfPath);
    }

    private static void CreatePng(string path, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        using var paint = new SKPaint { Color = SKColors.CornflowerBlue };
        canvas.DrawRect(0, 0, width, height, paint);
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
