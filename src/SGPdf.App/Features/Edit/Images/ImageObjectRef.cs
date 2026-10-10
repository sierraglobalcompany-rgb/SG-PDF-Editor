using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Edit.Images;

internal readonly record struct ImageObjectKey(
    int PageIndex,
    int PageObjectIndex);

internal sealed record ImageObjectRef(
    ImageObjectKey Key,
    PdfImageObjectInfo Original);
