using System.ComponentModel;
using System.Diagnostics;

namespace SGPdf.App.Features.Ocr;

internal sealed record TesseractProcessResult(
    string TsvPath,
    string StandardOutput,
    string StandardError);

internal sealed record TesseractChildProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);

internal sealed class TesseractProcessRecognizer
{
    private const int MaximumDiagnosticLength = 1500;

    private readonly string _executablePath;
    private readonly string _tessdataDirectory;

    internal TesseractProcessRecognizer()
        : this(
            TesseractRuntime.ResolveExecutablePath(AppContext.BaseDirectory),
            TesseractRuntime.ResolveTessdataDirectory(AppContext.BaseDirectory))
    {
    }

    internal TesseractProcessRecognizer(string executablePath, string tessdataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(tessdataDirectory);

        _executablePath = Path.GetFullPath(executablePath);
        _tessdataDirectory = Path.GetFullPath(tessdataDirectory);
    }

    internal async Task<TesseractProcessResult> RecognizeAsync(
        string imagePath,
        string outputBasePath,
        OcrLanguage language,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputBasePath);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));
        cancellationToken.ThrowIfCancellationRequested();

        var fullOutputBasePath = Path.GetFullPath(outputBasePath);
        var outputDirectory = Path.GetDirectoryName(fullOutputBasePath)
            ?? throw new InvalidOperationException("No se pudo resolver el directorio temporal de salida OCR.");
        Directory.CreateDirectory(outputDirectory);

        var tsvPath = fullOutputBasePath + ".tsv";
        DeleteIfExists(tsvPath);

        var completed = false;
        try
        {
            var startInfo = BuildStartInfo(imagePath, fullOutputBasePath, language);
            var processResult = await RunProcessAsync(startInfo, timeout, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (processResult.ExitCode != 0)
            {
                throw new InvalidDataException(
                    $"Tesseract OCR falló (código {processResult.ExitCode}): {FormatDiagnostic(processResult.StandardError)}");
            }

            if (!File.Exists(tsvPath) || new FileInfo(tsvPath).Length == 0)
                throw new InvalidDataException("Tesseract OCR finalizó sin producir un TSV válido.");

            completed = true;
            return new TesseractProcessResult(
                tsvPath,
                processResult.StandardOutput,
                processResult.StandardError);
        }
        finally
        {
            if (!completed)
                DeleteIfExists(tsvPath);
        }
    }

    private ProcessStartInfo BuildStartInfo(
        string imagePath,
        string outputBasePath,
        OcrLanguage language)
    {
        var fullImagePath = Path.GetFullPath(imagePath);
        var fullOutputBasePath = Path.GetFullPath(outputBasePath);
        var startInfo = new ProcessStartInfo(_executablePath)
        {
            WorkingDirectory = Path.GetDirectoryName(fullOutputBasePath) ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add(fullImagePath);
        startInfo.ArgumentList.Add(fullOutputBasePath);
        startInfo.ArgumentList.Add("--tessdata-dir");
        startInfo.ArgumentList.Add(_tessdataDirectory);
        startInfo.ArgumentList.Add("-l");
        startInfo.ArgumentList.Add(language.ToTesseractCode());
        startInfo.ArgumentList.Add("--oem");
        startInfo.ArgumentList.Add("1");
        startInfo.ArgumentList.Add("tsv");
        return startInfo;
    }

    private static async Task<TesseractChildProcessResult> RunProcessAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));
        cancellationToken.ThrowIfCancellationRequested();

        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException($"No se pudo iniciar el proceso OCR: {startInfo.FileName}");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var timeoutCancellation = new CancellationTokenSource(timeout);
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCancellation.Token);

        try
        {
            await process.WaitForExitAsync(linkedCancellation.Token);
            var standardOutput = await stdoutTask;
            var standardError = await stderrTask;
            return new TesseractChildProcessResult(process.ExitCode, standardOutput, standardError);
        }
        catch (OperationCanceledException)
        {
            TryKillProcessTree(process);
            try
            {
                await process.WaitForExitAsync(CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                // Process already unavailable after cancellation/kill.
            }

            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException("La operación OCR fue cancelada.", cancellationToken);

            if (timeoutCancellation.IsCancellationRequested)
                throw new TimeoutException("Tesseract OCR excedió el tiempo máximo permitido.");

            throw;
        }
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
        catch (Win32Exception)
        {
        }
        catch (NotSupportedException)
        {
        }
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string FormatDiagnostic(string diagnostic)
    {
        if (string.IsNullOrWhiteSpace(diagnostic))
            return "sin diagnóstico adicional";

        var text = diagnostic.Trim();
        return text.Length <= MaximumDiagnosticLength
            ? text
            : text[..MaximumDiagnosticLength] + "…";
    }
}
