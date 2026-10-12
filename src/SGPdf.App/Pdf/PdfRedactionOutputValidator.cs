using System.IO;
using SGPdf.App.Features.Edit.Redaction;

namespace SGPdf.App.Pdf;

internal sealed class PdfRedactionOutputValidator
{
    internal void Validate(
        string outputPath,
        RedactionWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(workspace);
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(outputPath))
            throw new FileNotFoundException("No existe el PDF temporal para validar la redacción.", outputPath);

        using var source = PdfDocumentSession.Open(workspace.SourcePath);
        using var output = PdfDocumentSession.Open(outputPath);
        if (source.PageCount != output.PageCount)
            throw new InvalidOperationException("La redacción cambió la cantidad de páginas del PDF.");

        foreach (var group in workspace.Targets.GroupBy(target => target.Original.Key.PageIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageIndex = group.Key;
            if (pageIndex < 0 || pageIndex >= source.PageCount)
                throw new InvalidOperationException("La redacción referencia una página inválida.");

            var targets = group.ToArray();
            var sourceText = source.GetTextObjects(pageIndex, cancellationToken);
            var outputText = output.GetTextObjects(pageIndex, cancellationToken);
            var expectedCount = sourceText.Count - targets.Length;
            if (expectedCount < 0 || outputText.Count != expectedCount)
                throw new InvalidOperationException("La salida no contiene la cantidad esperada de objetos de texto después de redactar.");

            foreach (var target in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (outputText.Any(item =>
                        string.Equals(item.Text, target.Original.Text, StringComparison.Ordinal) &&
                        BoundsOverlap(item.Bounds, target.Original.Bounds)))
                {
                    throw new InvalidOperationException("El texto redactado todavía es extraíble en su región original.");
                }
            }

            var rendered = output.RenderPage(pageIndex, 36d, cancellationToken);
            if (rendered.PixelWidth <= 0 || rendered.PixelHeight <= 0 || rendered.Pixels.Length == 0)
                throw new InvalidOperationException("La página redactada no se pudo renderizar después de guardar.");
        }
    }

    private static bool BoundsOverlap(PdfObjectBounds left, PdfObjectBounds right)
    {
        var horizontal = Math.Min(left.Right, right.Right) - Math.Max(left.Left, right.Left);
        var vertical = Math.Min(left.Top, right.Top) - Math.Max(left.Bottom, right.Bottom);
        return horizontal > 0d && vertical > 0d;
    }
}
