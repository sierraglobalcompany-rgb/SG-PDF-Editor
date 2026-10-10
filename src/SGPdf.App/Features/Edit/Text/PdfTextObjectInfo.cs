using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Edit.Text;

internal readonly record struct TextObjectKey(
    int PageIndex,
    int PageObjectIndex);

internal readonly record struct PdfTextFillColor(
    uint Red,
    uint Green,
    uint Blue,
    uint Alpha);

internal readonly record struct PdfTextObjectQuad(
    double X1,
    double Y1,
    double X2,
    double Y2,
    double X3,
    double Y3,
    double X4,
    double Y4);

internal sealed record PdfTextObjectInfo(
    TextObjectKey Key,
    string Text,
    PdfObjectMatrix Matrix,
    PdfObjectBounds Bounds,
    PdfTextObjectQuad Quad,
    string FontName,
    double FontSize,
    PdfTextFillColor FillColor,
    int TextRenderMode);
