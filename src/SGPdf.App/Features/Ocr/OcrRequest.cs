namespace SGPdf.App.Features.Ocr;

internal sealed record OcrRequest(OcrPageScope Scope, OcrLanguage Language)
{
    internal static OcrRequest Create(OcrPageScope scope, OcrLanguage language = OcrLanguage.Spanish)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return new OcrRequest(scope, language);
    }
}
