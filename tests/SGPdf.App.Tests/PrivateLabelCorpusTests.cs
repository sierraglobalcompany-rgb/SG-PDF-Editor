using Xunit;
using Xunit.Abstractions;

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
