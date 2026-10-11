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

    internal Task<TesseractProcessResult> RecognizeAsync(
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

        throw new NotImplementedException("Task 3 process behavior RED has not been implemented yet.");
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

    private static Task<TesseractChildProcessResult> RunProcessAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));
        cancellationToken.ThrowIfCancellationRequested();

        throw new NotImplementedException("Task 3 process behavior RED has not been implemented yet.");
    }
}
