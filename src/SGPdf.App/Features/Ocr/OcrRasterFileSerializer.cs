using System.IO;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Ocr;

internal static class OcrRasterFileSerializer
{
    private const int BitmapFileHeaderSize = 14;
    private const int BitmapInfoHeaderSize = 40;
    private const int BitsPerPixel = 24;

    internal static void WriteBmp(
        PdfRenderedPage renderedPage,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(renderedPage);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        if (renderedPage.PixelWidth <= 0 || renderedPage.PixelHeight <= 0 ||
            renderedPage.Stride < checked(renderedPage.PixelWidth * 4) ||
            renderedPage.Pixels.Length < checked(renderedPage.Stride * renderedPage.PixelHeight))
        {
            throw new ArgumentException("El raster OCR no contiene un bitmap BGRA válido.", nameof(renderedPage));
        }

        var destination = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(destination)
            ?? throw new InvalidOperationException("No se pudo resolver el directorio del raster OCR.");
        Directory.CreateDirectory(directory);

        var rawRowBytes = checked(renderedPage.PixelWidth * 3);
        var bmpRowBytes = checked((rawRowBytes + 3) & ~3);
        var imageSize = checked(bmpRowBytes * renderedPage.PixelHeight);
        var pixelOffset = BitmapFileHeaderSize + BitmapInfoHeaderSize;
        var fileSize = checked(pixelOffset + imageSize);
        var pixelsPerMeter = checked((int)Math.Round(renderedPage.Dpi / 0.0254d));
        var row = new byte[bmpRowBytes];

        try
        {
            using var stream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream);

            writer.Write((ushort)0x4D42);
            writer.Write(fileSize);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(pixelOffset);

            writer.Write(BitmapInfoHeaderSize);
            writer.Write(renderedPage.PixelWidth);
            writer.Write(-renderedPage.PixelHeight);
            writer.Write((ushort)1);
            writer.Write((ushort)BitsPerPixel);
            writer.Write(0);
            writer.Write(imageSize);
            writer.Write(pixelsPerMeter);
            writer.Write(pixelsPerMeter);
            writer.Write(0);
            writer.Write(0);

            for (var y = 0; y < renderedPage.PixelHeight; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Array.Clear(row);
                var sourceRow = checked(y * renderedPage.Stride);
                for (var x = 0; x < renderedPage.PixelWidth; x++)
                {
                    var source = checked(sourceRow + (x * 4));
                    var target = x * 3;
                    row[target] = renderedPage.Pixels[source];
                    row[target + 1] = renderedPage.Pixels[source + 1];
                    row[target + 2] = renderedPage.Pixels[source + 2];
                }

                writer.Write(row);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
        catch
        {
            TryDelete(destination);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
