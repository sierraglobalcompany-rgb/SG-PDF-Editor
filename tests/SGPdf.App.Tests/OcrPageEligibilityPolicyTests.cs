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

    [Fact]
    public void Language_codes_are_exact_and_scope_factories_are_zero_based()
    {
        Assert.Equal("spa", OcrLanguage.Spanish.ToTesseractCode());
        Assert.Equal("eng", OcrLanguage.English.ToTesseractCode());
        Assert.Equal("spa+eng", OcrLanguage.SpanishEnglish.ToTesseractCode());

        Assert.Equal(OcrPageScopeKind.Automatic, OcrPageScope.Automatic.Kind);

        var current = OcrPageScope.Current(2);
        Assert.Equal(OcrPageScopeKind.CurrentPage, current.Kind);
        Assert.Equal(2, current.StartPageIndex);
        Assert.Equal(2, current.EndPageIndex);

        var range = OcrPageScope.Range(1, 3);
        Assert.Equal(OcrPageScopeKind.Range, range.Kind);
        Assert.Equal(1, range.StartPageIndex);
        Assert.Equal(3, range.EndPageIndex);

        Assert.Throws<ArgumentOutOfRangeException>(() => OcrPageScope.Current(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => OcrPageScope.Range(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => OcrPageScope.Range(3, 2));
    }

    [Fact]
    public void Meaningful_text_counts_only_unicode_letters_and_digits_at_19_20_boundary()
    {
        var method = typeof(OcrPageEligibilityPolicy).GetMethod(
            "CountMeaningfulCharacters",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        Assert.Equal(0, InvokeCount(method!, " \t\r\n—.,;:!?()[]{}"));
        Assert.Equal(19, InvokeCount(method!, "áéíóúüñÑ 1234567890 A !!!"));
        Assert.Equal(20, InvokeCount(method!, "áéíóúüñÑ 1234567890 AB !!!"));
        Assert.Equal(19, InvokeCount(method!, "1234567890123456789"));
        Assert.Equal(20, InvokeCount(method!, "12345678901234567890"));
    }

    [Fact]
    public void Resolve_core_keeps_sparse_pages_candidates_and_skips_searchable_pages_in_order()
    {
        var resolveCore = typeof(OcrPageEligibilityPolicy).GetMethod(
            "ResolveCore",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(resolveCore);

        var pages = new[]
        {
            string.Empty,
            "1234567890123456789",
            "12345678901234567890",
            "Página 7",
            "Documento normalmente buscable con bastante contenido 2026"
        };
        var provider = new Func<int, string>(index => pages[index]);

        var automatic = InvokeResolveCore(resolveCore!, pages.Length, provider, OcrPageScope.Automatic);
        Assert.Equal(new[] { 0, 1, 3 }, automatic.CandidatePageIndices);
        Assert.Equal(new[] { 2, 4 }, automatic.SkippedPageIndices);

        var current = InvokeResolveCore(resolveCore!, pages.Length, provider, OcrPageScope.Current(2));
        Assert.Empty(current.CandidatePageIndices);
        Assert.Equal(new[] { 2 }, current.SkippedPageIndices);

        var range = InvokeResolveCore(resolveCore!, pages.Length, provider, OcrPageScope.Range(1, 3));
        Assert.Equal(new[] { 1, 3 }, range.CandidatePageIndices);
        Assert.Equal(new[] { 2 }, range.SkippedPageIndices);
    }

    [Fact]
    public void Resolve_core_rejects_scope_outside_document()
    {
        var resolveCore = typeof(OcrPageEligibilityPolicy).GetMethod(
            "ResolveCore",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(resolveCore);

        var provider = new Func<int, string>(_ => string.Empty);
        AssertInvocationThrows<ArgumentOutOfRangeException>(
            resolveCore!, 3, provider, OcrPageScope.Current(3));
        AssertInvocationThrows<ArgumentOutOfRangeException>(
            resolveCore!, 3, provider, OcrPageScope.Range(1, 3));
    }

    private static int InvokeCount(MethodInfo method, string text) =>
        Assert.IsType<int>(method.Invoke(null, new object?[] { text }));

    private static OcrPageEligibilityResult InvokeResolveCore(
        MethodInfo method,
        int pageCount,
        Func<int, string> provider,
        OcrPageScope scope) =>
        Assert.IsType<OcrPageEligibilityResult>(
            method.Invoke(null, new object?[] { pageCount, provider, scope, CancellationToken.None }));

    private static void AssertInvocationThrows<TException>(
        MethodInfo method,
        int pageCount,
        Func<int, string> provider,
        OcrPageScope scope)
        where TException : Exception
    {
        var exception = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(null, new object?[] { pageCount, provider, scope, CancellationToken.None }));
        Assert.IsType<TException>(exception.InnerException);
    }
}
