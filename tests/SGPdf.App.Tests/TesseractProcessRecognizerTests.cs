using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using SkiaSharp;
using SGPdf.App.Features.Ocr;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class TesseractProcessRecognizerTests
{
    [Fact]
    public void Task3_process_adapter_contract_is_present_and_isolated_under_ocr()
    {
        var assembly = typeof(TesseractRuntime).Assembly;
        var recognizerType = assembly.GetType("SGPdf.App.Features.Ocr.TesseractProcessRecognizer");
        var resultType = assembly.GetType("SGPdf.App.Features.Ocr.TesseractProcessResult");

        Assert.NotNull(recognizerType);
        Assert.NotNull(resultType);

        Assert.NotNull(recognizerType!.GetMethod(
            "RecognizeAsync",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        Assert.NotNull(recognizerType.GetMethod(
            "BuildStartInfo",
            BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(recognizerType.GetMethod(
            "RunProcessAsync",
            BindingFlags.Static | BindingFlags.NonPublic));

        Assert.NotNull(resultType!.GetProperty("TsvPath"));
        Assert.NotNull(resultType.GetProperty("StandardOutput"));
        Assert.NotNull(resultType.GetProperty("StandardError"));
    }

    [Fact]
    public void Process_adapter_does_not_reuse_the_labels_namespace_runner()
    {
        var assembly = typeof(TesseractRuntime).Assembly;
        var recognizerType = assembly.GetType("SGPdf.App.Features.Ocr.TesseractProcessRecognizer");
        Assert.NotNull(recognizerType);

        var fields = recognizerType!.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
        Assert.DoesNotContain(fields, field =>
            string.Equals(field.FieldType.FullName, "SGPdf.App.Features.Labels.ChildProcessRunner", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Spanish", "spa")]
    [InlineData("English", "eng")]
    [InlineData("SpanishEnglish", "spa+eng")]
    public void BuildStartInfo_uses_only_explicit_private_paths_and_exact_tsv_arguments(
        string languageName,
        string expectedLanguage)
    {
        var language = Enum.Parse<OcrLanguage>(languageName);
        var root = CreateTempDirectory();
        try
        {
            var executable = Path.Combine(root, "private", "tesseract.exe");
            var tessdata = Path.Combine(root, "private", "tessdata");
            var image = Path.Combine(root, "input page.png");
            var outputBase = Path.Combine(root, "result", "ocr page");
            var recognizer = new TesseractProcessRecognizer(executable, tessdata);

            var method = typeof(TesseractProcessRecognizer).GetMethod(
                "BuildStartInfo",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var startInfo = Assert.IsType<ProcessStartInfo>(method!.Invoke(
                recognizer,
                new object?[] { image, outputBase, language }));

            Assert.Equal(Path.GetFullPath(executable), startInfo.FileName);
            Assert.False(startInfo.UseShellExecute);
            Assert.True(startInfo.RedirectStandardOutput);
            Assert.True(startInfo.RedirectStandardError);
            Assert.True(startInfo.CreateNoWindow);
            Assert.Equal(
                new[]
                {
                    Path.GetFullPath(image),
                    Path.GetFullPath(outputBase),
                    "--tessdata-dir",
                    Path.GetFullPath(tessdata),
                    "-l",
                    expectedLanguage,
                    "--oem",
                    "1",
                    "tsv"
                },
                startInfo.ArgumentList.ToArray());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RecognizeAsync_runs_bundled_runtime_and_returns_fresh_TSV()
    {
        var root = CreateTempDirectory();
        try
        {
            var image = Path.Combine(root, "fixture.png");
            var outputBase = Path.Combine(root, "ocr");
            WriteTextPng(image, "Factura número 12345 NIÑO Bogotá información útil");

            var recognizer = new TesseractProcessRecognizer();
            var result = await recognizer.RecognizeAsync(
                image,
                outputBase,
                OcrLanguage.Spanish,
                TimeSpan.FromSeconds(30),
                CancellationToken.None);

            Assert.Equal(Path.GetFullPath(outputBase + ".tsv"), result.TsvPath);
            Assert.True(File.Exists(result.TsvPath));
            var tsv = await File.ReadAllTextAsync(result.TsvPath);
            Assert.Contains("NIÑO", tsv, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(result.StandardOutput);
            Assert.NotNull(result.StandardError);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Nonzero_exit_rejects_and_removes_stale_TSV()
    {
        var root = CreateTempDirectory();
        try
        {
            var missingImage = Path.Combine(root, "missing.png");
            var outputBase = Path.Combine(root, "ocr");
            var staleTsv = outputBase + ".tsv";
            await File.WriteAllTextAsync(staleTsv, "stale-success-must-not-survive");

            var recognizer = new TesseractProcessRecognizer();
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                recognizer.RecognizeAsync(
                    missingImage,
                    outputBase,
                    OcrLanguage.Spanish,
                    TimeSpan.FromSeconds(30),
                    CancellationToken.None));

            Assert.Contains("Tesseract", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(staleTsv));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Process_runner_captures_stdout_and_stderr()
    {
        var powershell = GetWindowsPowerShellPath();
        var startInfo = new ProcessStartInfo(powershell);
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add("[Console]::Out.Write('out-ok'); [Console]::Error.Write('err-ok')");

        var result = await InvokeRunProcessAsync(
            startInfo,
            TimeSpan.FromSeconds(10),
            CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("out-ok", result.StandardOutput);
        Assert.Equal("err-ok", result.StandardError);
    }

    [Fact]
    public async Task Timeout_kills_entire_process_tree()
    {
        var fixture = await CreateProcessTreeFixtureAsync();
        try
        {
            var startInfo = BuildPowerShellFileStartInfo(fixture.ParentScriptPath);
            await Assert.ThrowsAsync<TimeoutException>(() =>
                InvokeRunProcessAsync(startInfo, TimeSpan.FromMilliseconds(700), CancellationToken.None));

            await Task.Delay(TimeSpan.FromSeconds(3));
            Assert.False(File.Exists(fixture.MarkerPath), "A child process survived the timeout and wrote its marker.");
        }
        finally
        {
            Directory.Delete(fixture.Root, recursive: true);
        }
    }

    [Fact]
    public async Task Caller_cancellation_kills_entire_process_tree()
    {
        var fixture = await CreateProcessTreeFixtureAsync();
        try
        {
            var startInfo = BuildPowerShellFileStartInfo(fixture.ParentScriptPath);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(700));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                InvokeRunProcessAsync(startInfo, TimeSpan.FromSeconds(30), cancellation.Token));

            await Task.Delay(TimeSpan.FromSeconds(3));
            Assert.False(File.Exists(fixture.MarkerPath), "A child process survived caller cancellation and wrote its marker.");
        }
        finally
        {
            Directory.Delete(fixture.Root, recursive: true);
        }
    }

    private static async Task<TesseractChildProcessResult> InvokeRunProcessAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var method = typeof(TesseractProcessRecognizer).GetMethod(
            "RunProcessAsync",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        try
        {
            var task = Assert.IsAssignableFrom<Task<TesseractChildProcessResult>>(method!.Invoke(
                null,
                new object?[] { startInfo, timeout, cancellationToken }));
            return await task;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static async Task<ProcessTreeFixture> CreateProcessTreeFixtureAsync()
    {
        var root = CreateTempDirectory();
        var marker = Path.Combine(root, "child-survived.txt");
        var child = Path.Combine(root, "child.ps1");
        var parent = Path.Combine(root, "parent.ps1");
        var powershell = GetWindowsPowerShellPath();

        await File.WriteAllTextAsync(
            child,
            $"Start-Sleep -Seconds 2\n[System.IO.File]::WriteAllText('{EscapePowerShell(marker)}', 'survived')\n");
        await File.WriteAllTextAsync(
            parent,
            $"Start-Process -FilePath '{EscapePowerShell(powershell)}' -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File','{EscapePowerShell(child)}') | Out-Null\nStart-Sleep -Seconds 30\n");

        return new ProcessTreeFixture(root, parent, marker);
    }

    private static ProcessStartInfo BuildPowerShellFileStartInfo(string scriptPath)
    {
        var startInfo = new ProcessStartInfo(GetWindowsPowerShellPath());
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        return startInfo;
    }

    private static string GetWindowsPowerShellPath()
    {
        var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var path = Path.Combine(systemRoot, "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
        Assert.True(File.Exists(path), $"Windows PowerShell was not found at {path}.");
        return path;
    }

    private static string EscapePowerShell(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f10-process-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
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
            throw new InvalidOperationException("Could not encode deterministic OCR process PNG.");
        File.WriteAllBytes(path, data.ToArray());
    }

    private sealed record ProcessTreeFixture(string Root, string ParentScriptPath, string MarkerPath);
}
