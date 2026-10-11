using System.Reflection;
using SGPdf.App.Features.Ocr;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OcrPageEligibilityPolicyTests
{
    private const string OcrNamespace = "SGPdf.App.Features.Ocr";

    [Fact]
    public void Task3_contract_exposes_language_scope_policy_and_whole_page_text_primitives()
    {
        var appAssembly = typeof(TesseractRuntime).Assembly;

        var languageType = appAssembly.GetType($"{OcrNamespace}.OcrLanguage");
        Assert.NotNull(languageType);
        Assert.True(languageType!.IsEnum);
        Assert.Equal(new[] { "Spanish", "English", "SpanishEnglish" }, Enum.GetNames(languageType));

        var languageExtensions = appAssembly.GetType($"{OcrNamespace}.OcrLanguageExtensions");
        Assert.NotNull(languageExtensions);
        var toTesseractCode = languageExtensions!.GetMethod(
            "ToTesseractCode",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(toTesseractCode);

        Assert.Equal("spa", toTesseractCode!.Invoke(null, new[] { Enum.Parse(languageType, "Spanish") }));
        Assert.Equal("eng", toTesseractCode.Invoke(null, new[] { Enum.Parse(languageType, "English") }));
        Assert.Equal("spa+eng", toTesseractCode.Invoke(null, new[] { Enum.Parse(languageType, "SpanishEnglish") }));

        var scopeKindType = appAssembly.GetType($"{OcrNamespace}.OcrPageScopeKind");
        Assert.NotNull(scopeKindType);
        Assert.Equal(new[] { "Automatic", "CurrentPage", "Range" }, Enum.GetNames(scopeKindType!));

        var scopeType = appAssembly.GetType($"{OcrNamespace}.OcrPageScope");
        Assert.NotNull(scopeType);
        Assert.NotNull(scopeType!.GetProperty("Automatic", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(scopeType.GetMethod("Current", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(scopeType.GetMethod("Range", BindingFlags.Static | BindingFlags.Public));

        Assert.NotNull(appAssembly.GetType($"{OcrNamespace}.OcrRequest"));
        Assert.NotNull(appAssembly.GetType($"{OcrNamespace}.OcrPageEligibilityPolicy"));
        Assert.NotNull(appAssembly.GetType($"{OcrNamespace}.OcrPageEligibilityResult"));

        var nativeType = appAssembly.GetType("SGPdf.App.Pdf.PdfiumNative");
        Assert.NotNull(nativeType);
        Assert.NotNull(nativeType!.GetMethod(
            "FPDFText_CountChars",
            BindingFlags.Static | BindingFlags.NonPublic));

        var sessionType = appAssembly.GetType("SGPdf.App.Pdf.PdfDocumentSession");
        Assert.NotNull(sessionType);
        Assert.NotNull(sessionType!.GetMethod(
            "GetPageText",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
    }
}
