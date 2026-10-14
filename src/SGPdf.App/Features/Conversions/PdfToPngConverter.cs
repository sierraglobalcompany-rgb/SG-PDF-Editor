using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Conversions;

public sealed class PdfToPngConverter
{
    public const double DefaultDpi = 150d;

    public IReadOnlyList<string> Convert(
        string sourcePdfPath,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePdfPath))
            throw new ArgumentException("La ruta del PDF es obligatoria.", nameof(sourcePdfPath));
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("La carpeta de salida es obligatoria.", nameof(outputDirectory));

        string source = Path.GetFullPath(sourcePdfPath);
        string output = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(output);

        using var session = PdfDocumentSession.Open(source);
        int pageCount = session.PageCount;
        int digits = Math.Max(4, pageCount.ToString(CultureInfo.InvariantCulture).Length);
        string[] targets = Enumerable.Range(1, pageCount)
            .Select(page => Path.Combine(output, $"page-{page.ToString($"D{digits}", CultureInfo.InvariantCulture)}.png"))
            .ToArray();

        string? existing = targets.FirstOrDefault(File.Exists);
        if (existing is not null)
            throw new InvalidOperationException($"Ya existe el archivo de salida '{Path.GetFileName(existing)}'.");

        var written = new List<string>(pageCount);
        try
        {
            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                PdfRenderedPage rendered = session.RenderPage(pageIndex, DefaultDpi, cancellationToken);
                SavePng(rendered, targets[pageIndex]);
                written.Add(targets[pageIndex]);
            }

            return written;
        }
        catch
        {
            foreach (string path in written)
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                    // Best-effort cleanup; preserve the original exception.
                }
            }

            throw;
        }
    }

    private static void SavePng(PdfRenderedPage rendered, string destinationPath)
    {
        var bitmap = BitmapSource.Create(
            rendered.PixelWidth,
            rendered.PixelHeight,
            rendered.Dpi,
            rendered.Dpi,
            PixelFormats.Bgra32,
            null,
            rendered.Pixels,
            rendered.Stride);
        bitmap.Freeze();

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        encoder.Save(stream);
    }
}
