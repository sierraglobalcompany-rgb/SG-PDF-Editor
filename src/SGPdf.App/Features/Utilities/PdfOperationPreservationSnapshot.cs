using System.Globalization;
using System.Text;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Utilities;

internal sealed record PdfOperationPreservationSnapshot(
    int PageCount,
    string TextByPage,
    string Metadata,
    string Structure,
    IReadOnlyDictionary<string, string> MetadataFields)
{
    private static readonly string[] MetadataTags =
    [
        "Title",
        "Author",
        "Subject",
        "Keywords",
        "Creator",
        "Producer",
        "CreationDate",
        "ModDate"
    ];

    internal static PdfOperationPreservationSnapshot Capture(
        PdfDocumentSession session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        var pageCount = session.PageCount;
        var text = new StringBuilder();
        var structure = new StringBuilder();

        for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendField(text, session.GetPageText(pageIndex, cancellationToken));
            structure.Append(session.GetPageAnnotationCount(pageIndex, cancellationToken))
                .Append(':');
            foreach (var link in session.GetPageLinks(pageIndex, cancellationToken))
            {
                structure.Append(link.ActionKind).Append(',')
                    .Append(link.DestinationPageIndex?.ToString(CultureInfo.InvariantCulture) ?? "-").Append(',');
                AppendField(structure, link.Uri ?? string.Empty);
            }
            structure.Append('|');
        }

        var metadata = new StringBuilder();
        var metadataFields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var tag in MetadataTags)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var value = session.GetOrganizeMetadataText(tag, cancellationToken);
            metadataFields.Add(tag, value);
            metadata.Append(tag).Append('=');
            AppendField(metadata, value);
        }

        structure.Append("form=").Append(session.GetOrganizeFormType(cancellationToken))
            .Append(";named=").Append(session.HasOrganizeNamedDestinations(cancellationToken))
            .Append(";tagged=").Append(session.IsOrganizeTagged(cancellationToken))
            .Append(";labels=").Append(session.HasOrganizePageLabels(cancellationToken))
            .Append(";attachments=").Append(session.HasOrganizeAttachments(cancellationToken))
            .Append(";bookmarks=");
        AppendBookmarks(structure, session.GetBookmarks(cancellationToken));

        return new PdfOperationPreservationSnapshot(
            pageCount,
            text.ToString(),
            metadata.ToString(),
            structure.ToString(),
            metadataFields);
    }

    internal IReadOnlyList<string> GetChangedMetadataFields(
        PdfOperationPreservationSnapshot other,
        bool includeModificationDate = true)
        => MetadataFields.Keys
            .Where(tag => (includeModificationDate || !string.Equals(tag, "ModDate", StringComparison.Ordinal)) &&
                          !MetadataValueMatches(
                              tag,
                              MetadataFields[tag],
                              other.MetadataFields.GetValueOrDefault(tag) ?? string.Empty))
            .ToArray();

    internal bool HasSameContentAndStructuresAs(
        PdfOperationPreservationSnapshot other,
        bool includeModificationDate = true)
        => PageCount == other.PageCount &&
           string.Equals(TextByPage, other.TextByPage, StringComparison.Ordinal) &&
           string.Equals(Structure, other.Structure, StringComparison.Ordinal) &&
           GetChangedMetadataFields(other, includeModificationDate).Count == 0;

    private static bool MetadataValueMatches(string tag, string left, string right)
    {
        if (string.Equals(left, right, StringComparison.Ordinal))
            return true;
        if (!string.Equals(tag, "CreationDate", StringComparison.Ordinal))
            return false;

        return TryParseMetadataDate(left, out var leftDate) &&
               TryParseMetadataDate(right, out var rightDate) &&
               Math.Abs((leftDate - rightDate).TotalSeconds) < 1d;
    }

    private static bool TryParseMetadataDate(string value, out DateTimeOffset date)
    {
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
                out date))
        {
            return true;
        }

        var raw = value.StartsWith("D:", StringComparison.Ordinal) ? value[2..] : value;
        if (raw.Length < 14 ||
            !DateTime.TryParseExact(
                raw[..14],
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var localDate))
        {
            date = default;
            return false;
        }

        var suffix = raw[14..];
        if (suffix.Length == 0 || suffix.Equals("Z", StringComparison.OrdinalIgnoreCase))
        {
            date = new DateTimeOffset(DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified), TimeSpan.Zero);
            return true;
        }

        if (suffix.Length is < 5 or > 7 || suffix[0] is not ('+' or '-') ||
            !int.TryParse(suffix.AsSpan(1, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var hours))
        {
            date = default;
            return false;
        }

        var minuteText = suffix[3..].Replace("'", string.Empty, StringComparison.Ordinal);
        if (!int.TryParse(minuteText, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) ||
            hours > 14 || minutes > 59)
        {
            date = default;
            return false;
        }

        var totalMinutes = (hours * 60) + minutes;
        if (suffix[0] == '-')
            totalMinutes = -totalMinutes;
        date = new DateTimeOffset(
            DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified),
            TimeSpan.FromMinutes(totalMinutes));
        return true;
    }

    private static void AppendBookmarks(StringBuilder builder, IReadOnlyList<PdfBookmarkNode> nodes)
    {
        foreach (var node in nodes)
        {
            AppendField(builder, node.Title);
            builder.Append(node.DestinationPageIndex?.ToString(CultureInfo.InvariantCulture) ?? "-")
                .Append('{');
            AppendBookmarks(builder, node.Children);
            builder.Append('}');
        }
        builder.Append('|');
    }

    private static void AppendField(StringBuilder builder, string value)
        => builder.Append(value.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(value)
            .Append(';');
}
