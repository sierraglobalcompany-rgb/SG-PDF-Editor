using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Conversions;

public sealed class PdfToPngExporter
{
    private const double ExportDpi = 144d;

    public IReadOnlyList<string> ExportAllPages(
        string sourcePdfPath,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePdfPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        cancellationToken.ThrowIfCancellationRequested();

        string source = Path.GetFullPath(sourcePdfPath);
        string destination = Path.GetFullPath(outputDirectory);
        using var session = PdfDocumentSession.Open(source);

        int pageCount = session.PageCount;
        if (pageCount <= 0)
            throw new InvalidOperationException("El PDF no contiene páginas para exportar.");

        string fileStem = Path.GetFileNameWithoutExtension(source);
        string[] targets = Enumerable.Range(0, pageCount)
            .Select(index => Path.Combine(
                destination,
                $"{fileStem}-pagina-{index + 1:000}.png"))
            .ToArray();

        Directory.CreateDirectory(destination);
        foreach (string target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(target))
                throw new IOException($"El archivo de salida ya existe: {target}");
        }

        var written = new List<string>(targets.Length);
        try
        {
            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                PdfRenderedPage rendered = session.RenderPage(pageIndex, ExportDpi, cancellationToken);
                WritePng(rendered, targets[pageIndex], cancellationToken);
                written.Add(targets[pageIndex]);
            }

            return targets;
        }
        catch
        {
            foreach (string path in written)
                TryDelete(path);
            throw;
        }
    }

    private static void WritePng(
        PdfRenderedPage rendered,
        string targetPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool created = false;
        try
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

            using var stream = new FileStream(
                targetPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            created = true;
            encoder.Save(stream);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch
        {
            if (created)
                TryDelete(targetPath);
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
