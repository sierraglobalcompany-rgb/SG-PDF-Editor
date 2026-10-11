using System.Diagnostics;
using System.IO;
using System.Text.Json;
using SGPdf.App.Features.Labels;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Utilities;

internal enum PdfRepairProductDecision
{
    NoGo
}

internal sealed record PdfOptimizeCapabilityResult(
    bool Succeeded,
    long SourceLength,
    long OutputLength,
    bool HadWarnings,
    string? Error);

internal sealed record PdfRepairCapabilityResult(
    bool OutputCreated,
    int ExitCode,
    bool HadWarnings,
    string? Error);

internal static class PdfOptimizeRepairCapabilityAdapter
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(30);

    internal static PdfRepairProductDecision RepairProductDecision => PdfRepairProductDecision.NoGo;

    internal static async Task<PdfOptimizeCapabilityResult> OptimizeCopyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var paths = ValidatePaths(sourcePath, destinationPath);
        cancellationToken.ThrowIfCancellationRequested();
        var sourceLength = new FileInfo(paths.Source).Length;

        var job = new Dictionary<string, object?>
        {
            ["inputFile"] = paths.Source,
            ["outputFile"] = paths.Destination,
            ["objectStreams"] = "generate",
            ["recompressFlate"] = string.Empty,
            ["compressionLevel"] = "9",
            ["preserveUnreferenced"] = string.Empty
        };

        var execution = await RunJobAsync(
            job,
            paths.Destination,
            cancellationToken).ConfigureAwait(false);

        var hadWarnings = execution.ExitCode == 3;
        if (execution.ExitCode != 0 || !File.Exists(paths.Destination))
        {
            TryDelete(paths.Destination);
            return new PdfOptimizeCapabilityResult(
                false,
                sourceLength,
                0,
                hadWarnings,
                BuildError(execution, "qpdf could not create the optimized PDF copy."));
        }

        try
        {
            using var reopened = PdfDocumentSession.Open(paths.Destination);
            if (reopened.PageCount <= 0)
                throw new InvalidOperationException("The optimized PDF has no readable pages.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            TryDelete(paths.Destination);
            return new PdfOptimizeCapabilityResult(false, sourceLength, 0, hadWarnings, ex.Message);
        }

        return new PdfOptimizeCapabilityResult(
            true,
            sourceLength,
            new FileInfo(paths.Destination).Length,
            false,
            null);
    }

    internal static async Task<PdfRepairCapabilityResult> RepairCopyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var paths = ValidatePaths(sourcePath, destinationPath);
        cancellationToken.ThrowIfCancellationRequested();

        var job = new Dictionary<string, object?>
        {
            ["inputFile"] = paths.Source,
            ["outputFile"] = paths.Destination
        };

        var execution = await RunJobAsync(
            job,
            paths.Destination,
            cancellationToken).ConfigureAwait(false);

        var hadWarnings = execution.ExitCode == 3;
        if ((execution.ExitCode != 0 && execution.ExitCode != 3) || !File.Exists(paths.Destination))
        {
            TryDelete(paths.Destination);
            return new PdfRepairCapabilityResult(
                false,
                execution.ExitCode,
                hadWarnings,
                BuildError(execution, "qpdf could not recover the damaged PDF."));
        }

        try
        {
            using var reopened = PdfDocumentSession.Open(paths.Destination);
            if (reopened.PageCount <= 0)
                throw new InvalidOperationException("The recovered PDF has no readable pages.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            TryDelete(paths.Destination);
            return new PdfRepairCapabilityResult(false, execution.ExitCode, hadWarnings, ex.Message);
        }

        return new PdfRepairCapabilityResult(true, execution.ExitCode, hadWarnings, null);
    }

    private static (string Source, string Destination) ValidatePaths(string sourcePath, string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var sourceFullPath = Path.GetFullPath(sourcePath);
        var destinationFullPath = Path.GetFullPath(destinationPath);
        if (string.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The qpdf capability copy must use a different destination path.");
        if (!File.Exists(sourceFullPath))
            throw new FileNotFoundException("The source PDF was not found.", sourceFullPath);
        if (File.Exists(destinationFullPath))
            throw new IOException("The destination PDF already exists.");

        var destinationDirectory = Path.GetDirectoryName(destinationFullPath);
        if (destinationDirectory is null || !Directory.Exists(destinationDirectory))
            throw new DirectoryNotFoundException("The destination directory does not exist.");

        return (sourceFullPath, destinationFullPath);
    }

    private static async Task<ChildProcessResult> RunJobAsync(
        IReadOnlyDictionary<string, object?> job,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var qpdfPath = Path.Combine(AppContext.BaseDirectory, "qpdf", "qpdf.exe");
        if (!File.Exists(qpdfPath))
            throw new InvalidOperationException("The verified qpdf runtime is not available.");

        var directory = Path.GetDirectoryName(destinationPath)!;
        var jobPath = Path.Combine(
            directory,
            $".{Path.GetFileName(destinationPath)}.sgpdf-qpdf-job-{Guid.NewGuid():N}.json");

        try
        {
            await File.WriteAllTextAsync(
                jobPath,
                JsonSerializer.Serialize(job),
                cancellationToken).ConfigureAwait(false);

            var startInfo = new ProcessStartInfo(qpdfPath);
            startInfo.ArgumentList.Add($"--job-json-file={jobPath}");

            return await ChildProcessRunner.RunAsync(
                startInfo,
                ProcessTimeout,
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            TryDelete(destinationPath);
            throw;
        }
        finally
        {
            TryDelete(jobPath);
        }
    }

    private static string BuildError(ChildProcessResult result, string fallback)
    {
        var detail = result.StandardError.Trim();
        return string.IsNullOrWhiteSpace(detail) ? fallback : detail;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Best-effort cleanup for a capability gate. No product output is published here.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup for a capability gate. No product output is published here.
        }
    }
}
