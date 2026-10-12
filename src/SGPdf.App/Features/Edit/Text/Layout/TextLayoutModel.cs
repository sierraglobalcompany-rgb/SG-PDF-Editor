using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Edit.Text.Layout;

internal enum TextLayoutClassification
{
    Simple,
    Ambiguous,
    Unsupported
}

internal sealed record TextRun(
    TextObjectKey SourceKey,
    string Text,
    PdfObjectMatrix Matrix,
    PdfObjectBounds Bounds,
    PdfTextObjectQuad Quad,
    string FontName,
    double FontSize,
    PdfTextFillColor FillColor = default,
    int TextRenderMode = 0);

internal sealed record TextLine(
    PdfObjectBounds Bounds,
    IReadOnlyList<TextRun> Runs)
{
    internal string Text => string.Join(" ", Runs.Select(run => run.Text));
}

internal sealed record TextParagraph(
    PdfObjectBounds Bounds,
    IReadOnlyList<TextLine> Lines);

internal sealed record TextColumn(
    PdfObjectBounds Bounds,
    IReadOnlyList<TextParagraph> Paragraphs);

internal sealed record TextLayoutPage(
    double Width,
    double Height,
    IReadOnlyList<TextColumn> Columns,
    TextLayoutClassification Classification);
