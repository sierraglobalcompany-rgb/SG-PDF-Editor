using System.IO;
using SGPdf.App.Features.Comments;

namespace SGPdf.App.Pdf;

internal static class PdfCommentOutputValidator
{
    private const double GeometryTolerance = 0.001d;

    internal static void Validate(
        string outputPath,
        CommentWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("La ruta de salida es obligatoria.", nameof(outputPath));
        ArgumentNullException.ThrowIfNull(workspace);
        cancellationToken.ThrowIfCancellationRequested();

        using var source = PdfDocumentSession.Open(workspace.SourcePath);
        using var output = PdfDocumentSession.Open(outputPath);
        if (source.PageCount != output.PageCount)
            throw new InvalidDataException("La validación detectó un cambio inesperado en la cantidad de páginas.");

        var edited = workspace.EditedStates;
        var touchedPages = edited.Select(state => state.PageIndex)
            .Distinct()
            .OrderBy(index => index)
            .ToArray();

        foreach (var pageIndex in touchedPages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourceComments = source.GetCommentsOnPage(pageIndex, cancellationToken);
            var outputComments = output.GetCommentsOnPage(pageIndex, cancellationToken);
            var expected = BuildExpectedPage(
                workspace,
                sourceComments,
                edited.Where(state => state.PageIndex == pageIndex).ToArray());

            AssertEquivalentPage(pageIndex, expected, outputComments);

            var rendered = output.RenderPage(pageIndex, 24d, cancellationToken);
            if (rendered.PixelWidth <= 0 || rendered.PixelHeight <= 0 || rendered.Pixels.Length == 0)
                throw new InvalidDataException($"La página {pageIndex + 1} no superó el smoke render posterior al guardado.");
        }
    }

    private static IReadOnlyList<PdfCommentInfo> BuildExpectedPage(
        CommentWorkspace workspace,
        IReadOnlyList<PdfCommentInfo> sourceComments,
        IReadOnlyList<CommentState> changes)
    {
        var expected = sourceComments.ToList();
        foreach (var state in changes.Where(item => !item.IsNew))
        {
            if (!workspace.TryGetSourceIdentity(state.Key, out var identity))
                throw new InvalidDataException("No se puede validar un comentario existente sin identidad estable.");

            var sourceComment = CommentIdentity.Resolve(identity, sourceComments);
            if (sourceComment is null)
                throw new InvalidDataException("La identidad estable del comentario fuente ya no puede validarse.");

            var index = expected.FindIndex(item => ReferenceEquals(item, sourceComment));
            if (index < 0)
                throw new InvalidDataException("No se encontró el comentario fuente dentro del plan de validación.");

            if (state.Deleted)
            {
                expected.RemoveAt(index);
            }
            else
            {
                expected[index] = FromState(state, sourceComment.NativeSubtype, sourceComment.AnnotationIndex);
            }
        }

        foreach (var state in changes.Where(item => item.IsNew && !item.Deleted))
            expected.Add(FromState(state, MapSubtype(state.Subtype), annotationIndex: -1));

        return expected;
    }

    private static PdfCommentInfo FromState(CommentState state, int nativeSubtype, int annotationIndex)
        => new(
            state.PageIndex,
            annotationIndex,
            nativeSubtype,
            state.Subtype,
            IsEditable: true,
            state.Rect,
            state.Color,
            state.QuadPoints,
            state.Contents ?? string.Empty,
            state.InkStrokes,
            state.BorderWidth);

    private static void AssertEquivalentPage(
        int pageIndex,
        IReadOnlyList<PdfCommentInfo> expected,
        IReadOnlyList<PdfCommentInfo> actual)
    {
        if (expected.Count != actual.Count)
        {
            throw new InvalidDataException(
                $"La página {pageIndex + 1} esperaba {expected.Count} anotaciones y contiene {actual.Count} tras reabrir.");
        }

        var remaining = actual.ToList();
        foreach (var expectedComment in expected)
        {
            var matchIndex = remaining.FindIndex(candidate => SemanticallyEquivalent(expectedComment, candidate));
            if (matchIndex < 0)
            {
                throw new InvalidDataException(
                    $"La página {pageIndex + 1} no contiene el conjunto esperado de anotaciones después de guardar y reabrir.");
            }
            remaining.RemoveAt(matchIndex);
        }

        if (remaining.Count != 0)
            throw new InvalidDataException($"La página {pageIndex + 1} contiene anotaciones inesperadas tras el guardado.");
    }

    private static bool SemanticallyEquivalent(PdfCommentInfo expected, PdfCommentInfo actual)
    {
        if (expected.NativeSubtype != actual.NativeSubtype || expected.Subtype != actual.Subtype)
            return false;
        if (!RectEquals(expected.Rect, actual.Rect) || !ColorEquals(expected.Color, actual.Color))
            return false;
        if (!NullableNearlyEqual(expected.BorderWidth, actual.BorderWidth))
            return false;
        if (!string.Equals(expected.Contents, actual.Contents, StringComparison.Ordinal))
            return false;
        if (!QuadsEqual(expected.Quads, actual.Quads))
            return false;
        return StrokesEqual(expected.Strokes, actual.Strokes);
    }

    private static bool RectEquals(CommentRect? left, CommentRect? right)
    {
        if (!left.HasValue || !right.HasValue)
            return left.HasValue == right.HasValue;
        return NearlyEqual(left.Value.Left, right.Value.Left) &&
               NearlyEqual(left.Value.Bottom, right.Value.Bottom) &&
               NearlyEqual(left.Value.Right, right.Value.Right) &&
               NearlyEqual(left.Value.Top, right.Value.Top);
    }

    private static bool ColorEquals(CommentColor? left, CommentColor? right)
        => left.HasValue == right.HasValue && (!left.HasValue || left.Value == right!.Value);

    private static bool QuadsEqual(IReadOnlyList<CommentQuad> left, IReadOnlyList<CommentQuad> right)
    {
        if (left.Count != right.Count)
            return false;
        for (var index = 0; index < left.Count; index++)
        {
            if (!PointEquals(left[index].P1, right[index].P1) ||
                !PointEquals(left[index].P2, right[index].P2) ||
                !PointEquals(left[index].P3, right[index].P3) ||
                !PointEquals(left[index].P4, right[index].P4))
            {
                return false;
            }
        }
        return true;
    }

    private static bool StrokesEqual(IReadOnlyList<CommentStroke> left, IReadOnlyList<CommentStroke> right)
    {
        if (left.Count != right.Count)
            return false;
        for (var strokeIndex = 0; strokeIndex < left.Count; strokeIndex++)
        {
            var leftPoints = left[strokeIndex].Points;
            var rightPoints = right[strokeIndex].Points;
            if (leftPoints.Count != rightPoints.Count)
                return false;
            for (var pointIndex = 0; pointIndex < leftPoints.Count; pointIndex++)
            {
                if (!PointEquals(leftPoints[pointIndex], rightPoints[pointIndex]))
                    return false;
            }
        }
        return true;
    }

    private static bool PointEquals(CommentPoint left, CommentPoint right)
        => NearlyEqual(left.X, right.X) && NearlyEqual(left.Y, right.Y);

    private static bool NullableNearlyEqual(double? left, double? right)
    {
        if (!left.HasValue || !right.HasValue)
            return left.HasValue == right.HasValue;
        return NearlyEqual(left.Value, right.Value);
    }

    private static bool NearlyEqual(double left, double right)
        => double.IsFinite(left) && double.IsFinite(right) && Math.Abs(left - right) <= GeometryTolerance;

    private static int MapSubtype(CommentSubtype subtype) => subtype switch
    {
        CommentSubtype.Highlight => PdfiumNative.FPDF_ANNOT_HIGHLIGHT,
        CommentSubtype.Underline => PdfiumNative.FPDF_ANNOT_UNDERLINE,
        CommentSubtype.Strikeout => PdfiumNative.FPDF_ANNOT_STRIKEOUT,
        CommentSubtype.Text => PdfiumNative.FPDF_ANNOT_TEXT,
        CommentSubtype.Ink => PdfiumNative.FPDF_ANNOT_INK,
        CommentSubtype.Square => PdfiumNative.FPDF_ANNOT_SQUARE,
        CommentSubtype.Circle => PdfiumNative.FPDF_ANNOT_CIRCLE,
        _ => throw new ArgumentOutOfRangeException(nameof(subtype))
    };
}
