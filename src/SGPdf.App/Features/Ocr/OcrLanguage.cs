namespace SGPdf.App.Features.Ocr;

internal enum OcrLanguage
{
    Spanish,
    English,
    SpanishEnglish
}

internal static class OcrLanguageExtensions
{
    internal static string ToTesseractCode(this OcrLanguage language) => language switch
    {
        OcrLanguage.Spanish => "spa",
        OcrLanguage.English => "eng",
        OcrLanguage.SpanishEnglish => "spa+eng",
        _ => throw new ArgumentOutOfRangeException(nameof(language))
    };
}
