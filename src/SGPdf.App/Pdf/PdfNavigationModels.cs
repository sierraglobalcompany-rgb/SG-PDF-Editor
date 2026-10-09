namespace SGPdf.App.Pdf;

public sealed record PdfBookmarkNode(
    string Title,
    int? DestinationPageIndex,
    IReadOnlyList<PdfBookmarkNode> Children);

public enum PdfLinkActionKind
{
    None,
    InternalGoto,
    Uri,
    Unsupported
}

public sealed record PdfPageLink(
    int PageIndex,
    PdfTextRect Rect,
    PdfLinkActionKind ActionKind,
    int? DestinationPageIndex,
    string? Uri);
