using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Edit.Redaction;

internal static class RedactionPolicy
{
    private const int FillRenderMode = 0;

    internal static RedactionDecision Evaluate(
        IReadOnlyList<PdfTextObjectInfo> pageObjects,
        IReadOnlyList<TextObjectKey> selectedKeys,
        PdfEditPreflightResult preflight)
    {
        ArgumentNullException.ThrowIfNull(pageObjects);
        ArgumentNullException.ThrowIfNull(selectedKeys);
        ArgumentNullException.ThrowIfNull(preflight);

        if (!preflight.CanProceed)
            return RedactionDecision.Reject("El preflight general bloquea la redacción de este PDF.");

        if (selectedKeys.Count == 0)
            return RedactionDecision.Reject("No hay una selección de texto para redactar.");

        if (selectedKeys.Distinct().Count() != selectedKeys.Count)
            return RedactionDecision.Reject("La selección contiene objetos duplicados y no es segura para redactar.");

        var selectedPages = selectedKeys.Select(key => key.PageIndex).Distinct().ToArray();
        if (selectedPages.Length != 1)
            return RedactionDecision.Reject("La selección debe pertenecer a una sola pagina del PDF.");

        var pageIndex = selectedPages[0];
        if (pageObjects.Any(item => item.Key.PageIndex != pageIndex))
            return RedactionDecision.Reject("El snapshot de texto mezcla páginas y no es seguro para redactar.");

        var byKey = new Dictionary<TextObjectKey, PdfTextObjectInfo>();
        foreach (var item in pageObjects)
        {
            if (!byKey.TryAdd(item.Key, item))
                return RedactionDecision.Reject("El snapshot de texto contiene objetos duplicados.");
        }

        var selected = new List<PdfTextObjectInfo>(selectedKeys.Count);
        foreach (var key in selectedKeys)
        {
            if (!byKey.TryGetValue(key, out var item))
                return RedactionDecision.Reject("La selección ya no coincide con el snapshot actual de la página.");

            if (string.IsNullOrWhiteSpace(item.Text))
                return RedactionDecision.Reject("El objeto seleccionado no contiene texto visible verificable.");

            if (item.TextRenderMode != FillRenderMode || item.FillColor.Alpha == 0)
                return RedactionDecision.Reject("El objeto seleccionado no es texto visible seguro para redactar.");

            if (!IsValidMatrix(item.Matrix) || !IsHorizontal(item.Matrix))
                return RedactionDecision.Reject("La redacción inicial solo admite texto horizontal con transformación simple.");

            if (!IsValidBounds(item.Bounds))
                return RedactionDecision.Reject("La geometría del objeto seleccionado no es válida para redactar.");

            selected.Add(item);
        }

        foreach (var item in pageObjects)
        {
            if (!IsValidBounds(item.Bounds))
                return RedactionDecision.Reject("La geometría del snapshot de texto no es suficientemente segura para redactar.");
        }

        var selectedKeySet = selectedKeys.ToHashSet();
        foreach (var target in selected)
        {
            foreach (var other in pageObjects)
            {
                if (other.Key == target.Key)
                    continue;

                if (!BoundsOverlap(target.Bounds, other.Bounds))
                    continue;

                var relation = selectedKeySet.Contains(other.Key)
                    ? "objetos seleccionados"
                    : "texto que debe conservarse";
                return RedactionDecision.Reject($"La selección contiene un solapamiento con {relation} y no es segura para redactar.");
            }
        }

        var targets = selected
            .OrderBy(item => item.Key.PageObjectIndex)
            .Select(item => new RedactionTarget(item))
            .ToArray();

        return RedactionDecision.Allow(
            new RedactionPlan(pageIndex, Array.AsReadOnly(targets)));
    }

    private static bool IsValidMatrix(PdfObjectMatrix matrix)
        => double.IsFinite(matrix.A) &&
           double.IsFinite(matrix.B) &&
           double.IsFinite(matrix.C) &&
           double.IsFinite(matrix.D) &&
           double.IsFinite(matrix.E) &&
           double.IsFinite(matrix.F);

    private static bool IsHorizontal(PdfObjectMatrix matrix)
    {
        var primaryScale = Math.Max(Math.Max(Math.Abs(matrix.A), Math.Abs(matrix.D)), 1e-9d);
        return Math.Abs(matrix.B) <= primaryScale * 0.05d &&
               Math.Abs(matrix.C) <= primaryScale * 0.05d;
    }

    private static bool IsValidBounds(PdfObjectBounds bounds)
        => double.IsFinite(bounds.Left) &&
           double.IsFinite(bounds.Bottom) &&
           double.IsFinite(bounds.Right) &&
           double.IsFinite(bounds.Top) &&
           bounds.Right > bounds.Left &&
           bounds.Top > bounds.Bottom;

    private static bool BoundsOverlap(PdfObjectBounds left, PdfObjectBounds right)
    {
        var horizontalOverlap = Math.Min(left.Right, right.Right) - Math.Max(left.Left, right.Left);
        var verticalOverlap = Math.Min(left.Top, right.Top) - Math.Max(left.Bottom, right.Bottom);
        return horizontalOverlap > 0d && verticalOverlap > 0d;
    }
}
