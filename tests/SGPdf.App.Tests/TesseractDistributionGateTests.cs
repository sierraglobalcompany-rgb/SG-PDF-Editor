using System.Diagnostics;
using System.Text.Json;
using SkiaSharp;
using SGPdf.App.Features.Ocr;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class TesseractDistributionGateTests
{
    private const string ExpectedVersion = "tesseract v5.5.3.20260724";
    private const string ExpectedInstallerSha256 = "bee9e3434bd94fd65387d9be28cd467a41f61b1275383b55b0f59a1331270ae4";
    private const string ExpectedTessdataCommit = "87416418657359cb625c412a48b6e1d6d41c29bd";
    private const string ExpectedSpaBlob = "72e901f13ca52cfe34cf239a368b9ed3c0ddaf26";
    private const string ExpectedEngBlob = "bbef4675053b5b468cdb477053e28b1c698ba08e";
    private const string ExpectedSpaSha256 = "6f2e04d02774a18f01bed44b1111f2cd7f3ba7ac9dc4373cd3f898a40ea6b464";
    private const string ExpectedEngSha256 = "7d4322bd2a7749724879683fc3912cb542f19906c83bcc1a52132556427170b2";

    [Fact]
    public async Task Staged_private_runtime_has_exact_version_and_only_explicit_tessdata_path()
    {
        var executable = TesseractRuntime.ResolveExecutablePath(AppContext.BaseDirectory);
        var tessdata = TesseractRuntime.ResolveTessdataDirectory(AppContext.BaseDirectory);

        Assert.StartsWith(Path.GetFullPath(AppContext.BaseDirectory), executable, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(Path.GetFullPath(AppContext.BaseDirectory), tessdata, StringComparison.OrdinalIgnoreCase);

        var version = await RunAsync(executable, new[] { "--version" });
        Assert.Equal(0, version.ExitCode);
        Assert.StartsWith(ExpectedVersion, FirstNonEmptyLine(version.StandardOutput + Environment.NewLine + version.StandardError), StringComparison.Ordinal);

        var languages = await RunAsync(executable, new[] { "--tessdata-dir", tessdata, "--list-langs" });
        Assert.Equal(0, languages.ExitCode);
        var lines = (languages.StandardOutput + Environment.NewLine + languages.StandardError)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Contains("spa", lines);
        Assert.Contains("eng", lines);
    }

    [Fact]
    public void Staged_runtime_manifest_pins_official_installer_and_exact_tessdata_objects()
    {
        var manifestPath = Path.Combine(AppContext.BaseDirectory, "tesseract", "runtime-manifest.json");
        Assert.True(File.Exists(manifestPath), $"Missing staged Tesseract runtime manifest: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;
        Assert.Equal("5.5.3.20260724", root.GetProperty("version").GetString());
        Assert.Equal(ExpectedInstallerSha256, root.GetProperty("installerSha256").GetString());
        Assert.Equal(ExpectedTessdataCommit, root.GetProperty("tessdataFastCommit").GetString());
        Assert.Equal(ExpectedSpaBlob, root.GetProperty("spaGitBlobSha1").GetString());
        Assert.Equal(ExpectedEngBlob, root.GetProperty("engGitBlobSha1").GetString());
        Assert.Equal(ExpectedSpaSha256, root.GetProperty("spaSha256").GetString());
        Assert.Equal(ExpectedEngSha256, root.GetProperty("engSha256").GetString());
    }

    [Theory]
    [InlineData("spa", "Factura número 12345 NIÑO Bogotá información útil", "NIÑO")]
    [InlineData("eng", "Invoice number 67890 searchable document", "Invoice")]
    public async Task Bundled_runtime_recognizes_deterministic_text_offline(
        string language,
        string visibleText,
        string expectedToken)
    {
        var executable = TesseractRuntime.ResolveExecutablePath(AppContext.BaseDirectory);
        var tessdata = TesseractRuntime.ResolveTessdataDirectory(AppContext.BaseDirectory);
        var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f10-tesseract-gate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var imagePath = Path.Combine(directory, "fixture.png");
            var outputBase = Path.Combine(directory, "ocr");
            WriteTextPng(imagePath, visibleText);

            var result = await RunAsync(
                executable,
                new[]
                {
                    imagePath,
                    outputBase,
                    "--tessdata-dir", tessdata,
                    "-l", language,
                    "--oem", "1",
                    "--psm", "7"
                });

            Assert.Equal(0, result.ExitCode);
            var outputPath = outputBase + ".txt";
            Assert.True(File.Exists(outputPath), $"Tesseract did not create {outputPath}. stderr: {result.StandardError}");
            Assert.Contains(expectedToken, File.ReadAllText(outputPath), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void WriteTextPng(string path, string text)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(1800, 180, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal) ?? SKTypeface.Default;
        using var font = new SKFont(typeface, 64f);
        using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        canvas.DrawText(text, 25f, 115f, SKTextAlign.Left, font, paint);
        canvas.Flush();
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        if (data is null)
            throw new InvalidOperationException("Could not encode deterministic Tesseract gate PNG.");
        File.WriteAllBytes(path, data.ToArray());
    }

    private static async Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        Assert.True(process.Start(), $"Could not start {executable}.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await process.WaitForExitAsync(timeout.Token);
        return new ProcessResult(process.ExitCode, await stdout, await stderr);
    }

    private static string FirstNonEmptyLine(string text) =>
        text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
