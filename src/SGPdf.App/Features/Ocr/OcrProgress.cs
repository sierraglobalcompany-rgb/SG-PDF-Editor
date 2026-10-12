namespace SGPdf.App.Features.Ocr;

internal enum OcrProgressStage
{
    Rendering,
    Recognizing,
    Writing,
    Validating
}

internal sealed record OcrProgress(
    int CurrentOrdinal,
    int TotalEligiblePages,
    int PageIndex,
    OcrProgressStage Stage);
