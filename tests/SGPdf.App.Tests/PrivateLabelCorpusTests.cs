using System.Text;
using System.Text.Json;
using SGPdf.App.Features.Labels;
using Xunit;
using Xunit.Abstractions;
using ZXing;

namespace SGPdf.App.Tests;

public sealed class PrivateLabelCorpusTests
{
    private readonly ITestOutputHelper _output;

    public PrivateLabelCorpusTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ParseExpectation_MissingDesignCount_FailsClearly()
    {
        const string casePath = "case-001.zpl";

        var error = Assert.Throws<InvalidDataException>(() =>
            PrivateLabelCorpusQa.ParseExpectation(casePath, "{\"codes\":[]}"));

        Assert.Contains(casePath, error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("designCount", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseExpectation_UnsupportedBarcodeFormat_FailsClearly()
    {
        const string casePath = "case-002.zpl";
        const string json = "{\"designCount\":1,\"codes\":[{\"designIndex\":0,\"format\":\"EAN_13\",\"text\":\"123\"}]}";

        var error = Assert.Throws<InvalidDataException>(() =>
            PrivateLabelCorpusQa.ParseExpectation(casePath, json));

        Assert.Contains(casePath, error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EAN_13", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DiscoverCases_RequiresMatchingExpectedJson()
    {
        var root = CreateTempDirectory();
        var sourcePath = Path.Combine(root, "case-003.prn");
        File.WriteAllText(sourcePath, "^XA^FO20,20^FDPRIVATE QA SYNTHETIC^FS^XZ");

        try
        {
            var error = Assert.Throws<InvalidDataException>(() =>
                PrivateLabelCorpusQa.DiscoverCases(root));

            Assert.Contains("case-003.prn", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("expected.json", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DiscoverCases_EmptyRoot_FailsClearly()
    {
        var root = CreateTempDirectory();
        try
        {
            var error = Assert.Throws<InvalidDataException>(() =>
                PrivateLabelCorpusQa.DiscoverCases(root));

            Assert.Contains("no private label cases", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Trait("Category", "PrivateQA")]
    [Fact]
    public async Task PrivateCorpus_FromEnvironment_MatchesExpectations()
    {
        var root = Environment.GetEnvironmentVariable("SGPDF_PRIVATE_QA_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            _output.WriteLine("PRIVATE_QA: NOT RUN");
            return;
        }

        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Private QA root does not exist: {root}");

        var cases = PrivateLabelCorpusQa.DiscoverCases(root);
        foreach (var privateCase in cases)
            await PrivateLabelCorpusQa.ValidateCaseAsync(privateCase);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "sgpdf-private-corpus-contract-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}

internal sealed record PrivateExpectedCode(
    int DesignIndex,
    BarcodeFormat Format,
    string Text);

internal sealed record PrivateLabelExpectation(
    int DesignCount,
    double? WidthMm,
    double? HeightMm,
    int? Dpmm,
    IReadOnlyList<PrivateExpectedCode> Codes);

internal sealed record PrivateLabelCase(
    string SourcePath,
    PrivateLabelExpectation Expectation);

internal static class PrivateLabelCorpusQa
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    public static PrivateLabelExpectation ParseExpectation(string casePath, string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(casePath);
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("designCount", out var designCountElement) ||
                !designCountElement.TryGetInt32(out var designCount) ||
                designCount < 1)
            {
                throw Invalid(casePath, "designCount is required and must be a positive integer.");
            }

            var widthMm = ReadOptionalPositiveDouble(root, "widthMm", casePath);
            var heightMm = ReadOptionalPositiveDouble(root, "heightMm", casePath);
            var dpmm = ReadOptionalDpmm(root, casePath);
            var codes = ReadCodes(root, casePath);

            return new PrivateLabelExpectation(
                designCount,
                widthMm,
                heightMm,
                dpmm,
                codes);
        }
        catch (JsonException error)
        {
            throw Invalid(casePath, "expected JSON is invalid.", error);
        }
    }

    public static IReadOnlyList<PrivateLabelCase> DiscoverCases(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Private QA root does not exist: {root}");

        var sources = Directory
            .EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly)
            .Where(path => IsSupportedSource(Path.GetExtension(path)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (sources.Length == 0)
            throw new InvalidDataException("No private label cases were found in the requested QA root.");

        var cases = new List<PrivateLabelCase>(sources.Length);
        foreach (var sourcePath in sources)
        {
            var expectedPath = Path.Combine(
                Path.GetDirectoryName(sourcePath)!,
                $"{Path.GetFileNameWithoutExtension(sourcePath)}.expected.json");
            if (!File.Exists(expectedPath))
            {
                throw Invalid(
                    Path.GetFileName(sourcePath),
                    $"matching {Path.GetFileName(expectedPath)} expected.json is required.");
            }

            var json = File.ReadAllText(expectedPath, StrictUtf8);
            var expectation = ParseExpectation(Path.GetFileName(sourcePath), json);
            cases.Add(new PrivateLabelCase(sourcePath, expectation));
        }

        return cases;
    }

    public static async Task ValidateCaseAsync(PrivateLabelCase privateCase)
    {
        ArgumentNullException.ThrowIfNull(privateCase);

        var caseName = Path.GetFileName(privateCase.SourcePath);
        string source;
        try
        {
            source = File.ReadAllText(privateCase.SourcePath, StrictUtf8);
        }
        catch (DecoderFallbackException error)
        {
            throw Invalid(caseName, "source is not valid UTF-8.", error);
        }

        var document = ZplDocumentParser.Parse(privateCase.SourcePath, source);
        if (document.Designs.Count != privateCase.Expectation.DesignCount)
        {
            throw Invalid(
                caseName,
                $"design count mismatch: expected {privateCase.Expectation.DesignCount}, got {document.Designs.Count}.");
        }

        var expectation = privateCase.Expectation;
        var renderOptions = expectation.WidthMm.HasValue &&
                            expectation.HeightMm.HasValue &&
                            expectation.Dpmm.HasValue
            ? new ZplRenderOptions(
                expectation.WidthMm.Value,
                expectation.HeightMm.Value,
                expectation.Dpmm.Value)
            : ZplRenderOptions.Default;

        var rendered = await new LabelizeProcessRenderer().RenderAsync(document, renderOptions);
        if (rendered.Count != expectation.DesignCount)
            throw Invalid(caseName, "rendered design count does not match expectation.");

        if (expectation.WidthMm.HasValue && expectation.HeightMm.HasValue && expectation.Dpmm.HasValue)
        {
            foreach (var label in rendered)
            {
                if (label.WidthMm != expectation.WidthMm.Value ||
                    label.HeightMm != expectation.HeightMm.Value ||
                    label.Dpmm != expectation.Dpmm.Value)
                {
                    throw Invalid(caseName, $"render settings mismatch at design {label.DesignIndex}.");
                }
            }
        }

        foreach (var expectedCode in expectation.Codes)
        {
            if (expectedCode.DesignIndex < 0 || expectedCode.DesignIndex >= expectation.DesignCount)
                throw Invalid(caseName, $"code designIndex {expectedCode.DesignIndex} is out of range.");

            var label = rendered.SingleOrDefault(item => item.DesignIndex == expectedCode.DesignIndex);
            if (label is null)
                throw Invalid(caseName, $"rendered design {expectedCode.DesignIndex} was not found.");

            Result decoded;
            try
            {
                decoded = BarcodeDecodeAssert.DecodePng(label.PngBytes, expectedCode.Format);
            }
            catch (Exception error)
            {
                throw Invalid(
                    caseName,
                    $"could not decode {expectedCode.Format} at design {expectedCode.DesignIndex}.",
                    error);
            }

            if (decoded.BarcodeFormat != expectedCode.Format ||
                !string.Equals(decoded.Text, expectedCode.Text, StringComparison.Ordinal))
            {
                throw Invalid(
                    caseName,
                    $"decoded code mismatch at design {expectedCode.DesignIndex}.");
            }
        }
    }

    private static IReadOnlyList<PrivateExpectedCode> ReadCodes(JsonElement root, string casePath)
    {
        if (!root.TryGetProperty("codes", out var codesElement))
            return Array.Empty<PrivateExpectedCode>();
        if (codesElement.ValueKind != JsonValueKind.Array)
            throw Invalid(casePath, "codes must be an array when supplied.");

        var codes = new List<PrivateExpectedCode>();
        foreach (var code in codesElement.EnumerateArray())
        {
            if (!code.TryGetProperty("designIndex", out var designIndexElement) ||
                !designIndexElement.TryGetInt32(out var designIndex))
            {
                throw Invalid(casePath, "each code requires an integer designIndex.");
            }

            if (!code.TryGetProperty("format", out var formatElement) ||
                formatElement.ValueKind != JsonValueKind.String)
            {
                throw Invalid(casePath, "each code requires a format.");
            }

            var formatText = formatElement.GetString()!;
            var format = formatText switch
            {
                "CODE_128" => BarcodeFormat.CODE_128,
                "QR_CODE" => BarcodeFormat.QR_CODE,
                _ => throw Invalid(casePath, $"unsupported barcode format {formatText}.")
            };

            if (!code.TryGetProperty("text", out var textElement) ||
                textElement.ValueKind != JsonValueKind.String)
            {
                throw Invalid(casePath, "each code requires text.");
            }

            codes.Add(new PrivateExpectedCode(
                designIndex,
                format,
                textElement.GetString()!));
        }

        return codes;
    }

    private static double? ReadOptionalPositiveDouble(
        JsonElement root,
        string propertyName,
        string casePath)
    {
        if (!root.TryGetProperty(propertyName, out var element))
            return null;
        if (!element.TryGetDouble(out var value) || !double.IsFinite(value) || value <= 0)
            throw Invalid(casePath, $"{propertyName} must be a positive finite number when supplied.");
        return value;
    }

    private static int? ReadOptionalDpmm(JsonElement root, string casePath)
    {
        if (!root.TryGetProperty("dpmm", out var element))
            return null;
        if (!element.TryGetInt32(out var value) || value is not (6 or 8 or 12 or 24))
            throw Invalid(casePath, "dpmm must be one of 6, 8, 12, or 24 when supplied.");
        return value;
    }

    private static bool IsSupportedSource(string extension)
        => extension.Equals(".zpl", StringComparison.OrdinalIgnoreCase) ||
           extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) ||
           extension.Equals(".prn", StringComparison.OrdinalIgnoreCase);

    private static InvalidDataException Invalid(
        string casePath,
        string message,
        Exception? inner = null)
        => new($"{casePath}: {message}", inner);
}
