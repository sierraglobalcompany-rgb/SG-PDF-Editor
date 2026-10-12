using System.IO;
using SGPdf.App.Features.Edit.Redaction;
using SGPdf.App.Features.Edit.Text;

namespace SGPdf.App.Pdf;

internal sealed class PdfRedactionOutputValidator
{
    private const double BoundsTolerance = 0.10d;

    internal void Validate(
        string outputPath,
        RedactionWorkspace workspace,
        CancellationToken cancellationToken = default)
        => Validate(outputPath, workspace.SourcePath, workspace, cancellationToken);

    internal void Validate(
        string outputPath,
        string baselinePath,
        RedactionWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(baselinePath);
        ArgumentNullException.ThrowIfNull(workspace);
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(outputPath))
            throw new FileNotFoundException("No existe el PDF temporal para validar la redacción.", outputPath);
        if (!File.Exists(baselinePath))
            throw new FileNotFoundException("No existe el PDF baseline para validar la redacción.", baselinePath);

        using (var baseline = PdfDocumentSession.Open(baselinePath))
        using (var output = PdfDocumentSession.Open(outputPath))
        {
            if (baseline.PageCount != output.PageCount)
                throw new InvalidOperationException("La redacción cambió la cantidad de páginas del PDF.");

            foreach (var group in workspace.Targets.GroupBy(target => target.Original.Key.PageIndex))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var pageIndex = group.Key;
                if (pageIndex < 0 || pageIndex >= baseline.PageCount)
                    throw new InvalidOperationException("La redacción referencia una página inválida.");

                var targets = group.ToArray();
                var baselineText = baseline.GetTextObjects(pageIndex, cancellationToken);
                var outputText = output.GetTextObjects(pageIndex, cancellationToken);
                var expectedCount = baselineText.Count - targets.Length;
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

                ValidateNeighbors(baselineText, outputText, targets, cancellationToken);

                var rendered = output.RenderPage(pageIndex, 36d, cancellationToken);
                if (rendered.PixelWidth <= 0 || rendered.PixelHeight <= 0 || rendered.Pixels.Length == 0)
                    throw new InvalidOperationException("La página redactada no se pudo renderizar después de guardar.");
            }
        }

        ValidateBlackRedactionMarks(outputPath, workspace.Targets, cancellationToken);
    }

    private static void ValidateNeighbors(
        IReadOnlyList<PdfTextObjectInfo> baselineText,
        IReadOnlyList<PdfTextObjectInfo> outputText,
        IReadOnlyList<RedactionTarget> targets,
        CancellationToken cancellationToken)
    {
        var neighbors = baselineText
            .Where(item => !targets.Any(target => MatchesTarget(item, target)))
            .ToArray();
        if (neighbors.Length != baselineText.Count - targets.Count)
            throw new InvalidOperationException("La baseline no coincide de forma inequívoca con los objetivos de redacción.");

        foreach (var neighbor in neighbors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var matches = outputText.Count(item =>
                string.Equals(item.Text, neighbor.Text, StringComparison.Ordinal) &&
                BoundsEquivalent(item.Bounds, neighbor.Bounds));
            if (matches != 1)
                throw new InvalidOperationException("La salida no preservó correctamente el texto vecino de la redacción.");
        }
    }

    private static bool MatchesTarget(PdfTextObjectInfo item, RedactionTarget target)
        => string.Equals(item.Text, target.Original.Text, StringComparison.Ordinal) &&
           BoundsEquivalent(item.Bounds, target.Original.Bounds);

    private static bool BoundsEquivalent(PdfObjectBounds left, PdfObjectBounds right)
        => NearlyEqual(left.Left, right.Left) &&
           NearlyEqual(left.Bottom, right.Bottom) &&
           NearlyEqual(left.Right, right.Right) &&
           NearlyEqual(left.Top, right.Top);

    private static void ValidateBlackRedactionMarks(
        string outputPath,
        IReadOnlyList<RedactionTarget> targets,
        CancellationToken cancellationToken)
    {
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait(cancellationToken);

        IntPtr document = IntPtr.Zero;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            document = PdfiumNative.FPDF_LoadDocument(outputPath, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException("No se pudo reabrir la salida para validar las marcas de redacción.");

            foreach (var group in targets.GroupBy(target => target.Original.Key.PageIndex))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var page = PdfiumNative.FPDF_LoadPage(document, group.Key);
                if (page == IntPtr.Zero)
                    throw new InvalidOperationException("No se pudo abrir una página para validar las marcas de redacción.");

                try
                {
                    var objectCount = PdfiumNative.FPDFPage_CountObjects(page);
                    foreach (var target in group)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var matches = 0;

                        for (var index = 0; index < objectCount; index++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var pageObject = PdfiumNative.FPDFPage_GetObject(page, index);
                            if (!IsExpectedBlackMark(pageObject, target.Original.Bounds))
                                continue;

                            matches++;
                        }

                        if (matches != 1)
                        {
                            throw new InvalidOperationException(
                                "La salida no contiene exactamente una marca negra opaca válida para la redacción.");
                        }
                    }
                }
                finally
                {
                    PdfiumNative.FPDF_ClosePage(page);
                }
            }
        }
        finally
        {
            if (document != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(document);
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static bool IsExpectedBlackMark(IntPtr pageObject, PdfObjectBounds expectedBounds)
    {
        if (pageObject == IntPtr.Zero ||
            PdfiumNative.FPDFPageObj_GetType(pageObject) != PdfiumNative.FPDF_PAGEOBJ_PATH ||
            PdfiumNative.FPDFPageObj_GetBounds(
                pageObject,
                out var left,
                out var bottom,
                out var right,
                out var top) == 0 ||
            !NearlyEqual(left, expectedBounds.Left) ||
            !NearlyEqual(bottom, expectedBounds.Bottom) ||
            !NearlyEqual(right, expectedBounds.Right) ||
            !NearlyEqual(top, expectedBounds.Top) ||
            PdfiumNative.FPDFPageObj_GetFillColor(
                pageObject,
                out var red,
                out var green,
                out var blue,
                out var alpha) == 0 ||
            red != 0u ||
            green != 0u ||
            blue != 0u ||
            alpha != 255u ||
            PdfiumNative.FPDFPath_GetDrawMode(pageObject, out var fillMode, out var stroke) == 0 ||
            fillMode != PdfiumNative.FPDF_FILLMODE_WINDING ||
            stroke != 0)
        {
            return false;
        }

        return true;
    }

    private static bool NearlyEqual(double left, double right)
        => double.IsFinite(left) &&
           double.IsFinite(right) &&
           Math.Abs(left - right) <= BoundsTolerance;

    private static bool BoundsOverlap(PdfObjectBounds left, PdfObjectBounds right)
    {
        var horizontal = Math.Min(left.Right, right.Right) - Math.Max(left.Left, right.Left);
        var vertical = Math.Min(left.Top, right.Top) - Math.Max(left.Bottom, right.Bottom);
        return horizontal > 0d && vertical > 0d;
    }
}
