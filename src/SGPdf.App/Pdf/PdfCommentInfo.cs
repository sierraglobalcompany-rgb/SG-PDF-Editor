using SGPdf.App.Features.Comments;

namespace SGPdf.App.Pdf;

internal sealed record PdfCommentInfo(
    int PageIndex,
    int AnnotationIndex,
    int NativeSubtype,
    CommentSubtype? Subtype,
    bool IsEditable,
    CommentRect? Rect,
    CommentColor? Color,
    IReadOnlyList<CommentQuad> Quads,
    string Contents,
    IReadOnlyList<CommentStroke> Strokes,
    double? BorderWidth);
