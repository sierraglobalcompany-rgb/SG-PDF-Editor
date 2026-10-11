using System.Reflection;
using SGPdf.App.Features.Ocr;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfOcrOutputValidatorTests
{
    [Fact]
    public void Validate_TrueInvisibleSearchableOutput_AcceptsSemanticFixture()
    {
        using var snapshot = OcrInvisibleTextNativeHarness.Characterize(rotate90: false);
        var expectation = CreateExpectation(
            expectedPageCount: 1,
            placements: CreateRepresentativePlacements(ExpectedRegion),
            skippedPageTexts: new Dictionary<int, string>(),
            sourceRenders: new[] { snapshot.SourceRender });

        var result = InvokeValidate(snapshot.OutputPath, expectation);

        Assert.True(GetSucceeded(result), GetError(result));
    }

    [Fact]
    public void Validate_PageCountMismatch_FailsClosed()
    {
        using var snapshot = OcrInvisibleTextNativeHarness.Characterize(rotate90: false);
        var expectation = CreateExpectation(
            expectedPageCount: 2,
            placements: CreateRepresentativePlacements(ExpectedRegion),
            skippedPageTexts: new Dictionary<int, string>(),
            sourceRenders: new[] { snapshot.SourceRender });

        var result = InvokeValidate(snapshot.OutputPath, expectation);

        Assert.False(GetSucceeded(result));
    }

    [Fact]
    public void Validate_MissingExactSpanishUnicodeToken_FailsClosed()
    {
        using var snapshot = OcrInvisibleTextNativeHarness.Characterize(rotate90: false);
        var placements = CreatePlacements(
            OcrInvisibleTextNativeHarness.SearchableText,
            ExpectedRegion,
            "número",
            "NIÑO",
            "Bogotá",
            "información",
            "útil",
            "Ñandú");
        var expectation = CreateExpectation(
            expectedPageCount: 1,
            placements,
            skippedPageTexts: new Dictionary<int, string>(),
            sourceRenders: new[] { snapshot.SourceRender });

        var result = InvokeValidate(snapshot.OutputPath, expectation);

        Assert.False(GetSucceeded(result));
    }

    [Fact]
    public void Validate_SkippedSearchablePageTextMismatch_FailsClosed()
    {
        using var snapshot = OcrInvisibleTextNativeHarness.Characterize(rotate90: false);
        var expectation = CreateExpectation(
            expectedPageCount: 1,
            placements: Array.Empty<OcrTextLinePlacement>(),
            skippedPageTexts: new Dictionary<int, string>
            {
                [0] = snapshot.ExtractedText + " CAMBIADO"
            },
            sourceRenders: new[] { snapshot.SourceRender });

        var result = InvokeValidate(snapshot.OutputPath, expectation);

        Assert.False(GetSucceeded(result));
    }

    [Fact]
    public void Validate_RenderPixelsDifferFromSource_FailsClosed()
    {
        using var snapshot = OcrInvisibleTextNativeHarness.Characterize(rotate90: false);
        var mismatchedPixels = (byte[])snapshot.SourceRender.Pixels.Clone();
        mismatchedPixels[0] ^= 0xFF;
        var mismatchedReference = new PdfRenderedPage(
            snapshot.SourceRender.PageIndex,
            snapshot.SourceRender.PixelWidth,
            snapshot.SourceRender.PixelHeight,
            snapshot.SourceRender.Stride,
            snapshot.SourceRender.Dpi,
            mismatchedPixels);
        var expectation = CreateExpectation(
            expectedPageCount: 1,
            placements: CreateRepresentativePlacements(ExpectedRegion),
            skippedPageTexts: new Dictionary<int, string>(),
            sourceRenders: new[] { mismatchedReference });

        var result = InvokeValidate(snapshot.OutputPath, expectation);

        Assert.False(GetSucceeded(result));
    }

    [Fact]
    public void Validate_InsertedTextGeometryMissesExpectedOcrRegion_FailsClosed()
    {
        using var snapshot = OcrInvisibleTextNativeHarness.Characterize(rotate90: false);
        var expectation = CreateExpectation(
            expectedPageCount: 1,
            placements: CreateRepresentativePlacements(new PdfOcrRect(0d, 0d, 10d, 10d)),
            skippedPageTexts: new Dictionary<int, string>(),
            sourceRenders: new[] { snapshot.SourceRender });

        var result = InvokeValidate(snapshot.OutputPath, expectation);

        Assert.False(GetSucceeded(result));
    }

    [Fact]
    public void Validate_MalformedOutput_FailsClosedInsteadOfPublishingConfidence()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f10-validator-red-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "not-a-pdf.pdf");
            File.WriteAllText(path, "not a pdf");
            var reference = new PdfRenderedPage(0, 1, 1, 4, 72d, new byte[4]);
            var expectation = CreateExpectation(
                expectedPageCount: 1,
                placements: Array.Empty<OcrTextLinePlacement>(),
                skippedPageTexts: new Dictionary<int, string>(),
                sourceRenders: new[] { reference });

            var result = InvokeValidate(path, expectation);

            Assert.False(GetSucceeded(result));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static readonly PdfOcrRect ExpectedRegion = new(28d, 175d, 295d, 255d);

    private static IReadOnlyList<OcrTextLinePlacement> CreateRepresentativePlacements(PdfOcrRect bounds) =>
        CreatePlacements(
            OcrInvisibleTextNativeHarness.SearchableText,
            bounds,
            "número",
            "NIÑO",
            "Bogotá",
            "información",
            "útil",
            "12345");

    private static IReadOnlyList<OcrTextLinePlacement> CreatePlacements(
        string lineText,
        PdfOcrRect bounds,
        params string[] tokens)
    {
        var words = tokens
            .Select((token, index) => new OcrTextWordPlacement(token, 95d, bounds, index, index + 1))
            .ToArray();
        return new[]
        {
            new OcrTextLinePlacement(
                0,
                lineText,
                bounds,
                words,
                0,
                1,
                1,
                1)
        };
    }

    private static object CreateExpectation(
        int expectedPageCount,
        IReadOnlyList<OcrTextLinePlacement> placements,
        IReadOnlyDictionary<int, string> skippedPageTexts,
        IReadOnlyList<PdfRenderedPage> sourceRenders)
    {
        var expectationType = RequireType("SGPdf.App.Pdf.OcrValidationExpectation");
        var instance = Activator.CreateInstance(
            expectationType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { expectedPageCount, placements, skippedPageTexts, sourceRenders },
            culture: null);
        Assert.NotNull(instance);
        return instance!;
    }

    private static object InvokeValidate(string outputPath, object expectation)
    {
        var validatorType = RequireType("SGPdf.App.Pdf.PdfOcrOutputValidator");
        var method = validatorType.GetMethod(
            "Validate",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), expectation.GetType(), typeof(CancellationToken) },
            modifiers: null);
        Assert.NotNull(method);

        var result = method!.Invoke(null, new object[] { outputPath, expectation, CancellationToken.None });
        Assert.NotNull(result);
        Assert.Equal("PdfOcrValidationResult", result!.GetType().Name);
        return result;
    }

    private static Type RequireType(string fullName)
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType(fullName);
        Assert.NotNull(type);
        return type!;
    }

    private static bool GetSucceeded(object result)
    {
        var property = result.GetType().GetProperty("Succeeded");
        Assert.NotNull(property);
        return Assert.IsType<bool>(property!.GetValue(result));
    }

    private static string? GetError(object result)
    {
        var property = result.GetType().GetProperty("Error");
        Assert.NotNull(property);
        return property!.GetValue(result) as string;
    }
}
