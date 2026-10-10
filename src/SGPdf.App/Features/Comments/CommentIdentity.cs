using System.Security.Cryptography;
using System.Text;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Comments;

internal sealed record CommentIdentity(
    int PageIndex,
    int NativeSubtype,
    string StructuralFingerprint,
    int DuplicateOrdinal,
    int DuplicateCount)
{
    internal static IReadOnlyList<CommentIdentity> CreateForPage(IReadOnlyList<PdfCommentInfo> comments)
    {
        ArgumentNullException.ThrowIfNull(comments);
        if (comments.Count == 0)
            return Array.Empty<CommentIdentity>();

        var entries = comments
            .Select((comment, position) => new IdentityEntry(
                comment ?? throw new ArgumentException("La colección no puede contener comentarios null.", nameof(comments)),
                position,
                ComputeStructuralFingerprint(comment)))
            .ToArray();

        var result = new CommentIdentity[entries.Length];
        foreach (var group in entries.GroupBy(entry => (entry.Comment.PageIndex, entry.Comment.NativeSubtype, entry.Fingerprint)))
        {
            var ordered = group
                .OrderBy(entry => entry.Comment.AnnotationIndex)
                .ThenBy(entry => entry.Position)
                .ToArray();

            for (var ordinal = 0; ordinal < ordered.Length; ordinal++)
            {
                var entry = ordered[ordinal];
                result[entry.Position] = new CommentIdentity(
                    entry.Comment.PageIndex,
                    entry.Comment.NativeSubtype,
                    entry.Fingerprint,
                    ordinal,
                    ordered.Length);
            }
        }

        return result;
    }

    internal static PdfCommentInfo? Resolve(
        CommentIdentity identity,
        IReadOnlyList<PdfCommentInfo> comments)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(comments);

        if (identity.DuplicateOrdinal < 0 ||
            identity.DuplicateCount <= 0 ||
            identity.DuplicateOrdinal >= identity.DuplicateCount ||
            string.IsNullOrWhiteSpace(identity.StructuralFingerprint))
        {
            return null;
        }

        var matches = comments
            .Where(comment => comment is not null)
            .Select((comment, position) => new IdentityEntry(
                comment!,
                position,
                ComputeStructuralFingerprint(comment!)))
            .Where(entry =>
                entry.Comment.PageIndex == identity.PageIndex &&
                entry.Comment.NativeSubtype == identity.NativeSubtype &&
                string.Equals(entry.Fingerprint, identity.StructuralFingerprint, StringComparison.Ordinal))
            .OrderBy(entry => entry.Comment.AnnotationIndex)
            .ThenBy(entry => entry.Position)
            .ToArray();

        if (matches.Length != identity.DuplicateCount)
            return null;

        return matches[identity.DuplicateOrdinal].Comment;
    }

    private static string ComputeStructuralFingerprint(PdfCommentInfo comment)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(comment.PageIndex);
            writer.Write(comment.NativeSubtype);
            WriteNullableRect(writer, comment.Rect);
            WriteNullableColor(writer, comment.Color);

            writer.Write(comment.Quads.Count);
            foreach (var quad in comment.Quads)
            {
                WritePoint(writer, quad.P1);
                WritePoint(writer, quad.P2);
                WritePoint(writer, quad.P3);
                WritePoint(writer, quad.P4);
            }

            writer.Write(comment.Contents ?? string.Empty);

            writer.Write(comment.Strokes.Count);
            foreach (var stroke in comment.Strokes)
            {
                writer.Write(stroke.Points.Count);
                foreach (var point in stroke.Points)
                    WritePoint(writer, point);
            }

            writer.Write(comment.BorderWidth.HasValue);
            if (comment.BorderWidth.HasValue)
                writer.Write(comment.BorderWidth.Value);
        }

        return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }

    private static void WriteNullableRect(BinaryWriter writer, CommentRect? rect)
    {
        writer.Write(rect.HasValue);
        if (!rect.HasValue)
            return;

        writer.Write(rect.Value.Left);
        writer.Write(rect.Value.Bottom);
        writer.Write(rect.Value.Right);
        writer.Write(rect.Value.Top);
    }

    private static void WriteNullableColor(BinaryWriter writer, CommentColor? color)
    {
        writer.Write(color.HasValue);
        if (!color.HasValue)
            return;

        writer.Write(color.Value.Red);
        writer.Write(color.Value.Green);
        writer.Write(color.Value.Blue);
        writer.Write(color.Value.Alpha);
    }

    private static void WritePoint(BinaryWriter writer, CommentPoint point)
    {
        writer.Write(point.X);
        writer.Write(point.Y);
    }

    private sealed record IdentityEntry(
        PdfCommentInfo Comment,
        int Position,
        string Fingerprint);
}
