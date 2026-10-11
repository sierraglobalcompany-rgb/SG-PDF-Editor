using System.Diagnostics;
using System.IO;
using System.Text.Json;
using SGPdf.App.Features.Labels;

namespace SGPdf.App.Features.Utilities;

internal static class PdfProtectionCapabilityAdapter
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(30);

    internal static async Task CreateProtectedCopyAsync(
        string sourcePath,
        string destinationPath,
        string userPassword,
        string ownerPassword,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(userPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerPassword);

        cancellationToken.ThrowIfCancellationRequested();

        var sourceFullPath = Path.GetFullPath(sourcePath);
        var destinationFullPath = Path.GetFullPath(destinationPath);
        if (string.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The protected copy must use a different destination path.");
        if (!File.Exists(sourceFullPath))
            throw new FileNotFoundException("The source PDF was not found.", sourceFullPath);
        if (File.Exists(destinationFullPath))
            throw new IOException("The destination PDF already exists.");

        var destinationDirectory = Path.GetDirectoryName(destinationFullPath);
        if (destinationDirectory is null || !Directory.Exists(destinationDirectory))
            throw new DirectoryNotFoundException("The destination directory does not exist.");

        var qpdfPath = Path.Combine(AppContext.BaseDirectory, "qpdf", "qpdf.exe");
        if (!File.Exists(qpdfPath))
            throw new InvalidOperationException("The verified qpdf runtime is not available.");

        var jobPath = Path.Combine(
            destinationDirectory,
            $".{Path.GetFileName(destinationFullPath)}.sgpdf-qpdf-job-{Guid.NewGuid():N}.json");

        try
        {
            var job = new Dictionary<string, object?>
            {
                ["inputFile"] = sourceFullPath,
                ["outputFile"] = destinationFullPath,
                ["encrypt"] = new Dictionary<string, object?>
                {
                    ["userPassword"] = userPassword,
                    ["ownerPassword"] = ownerPassword,
                    ["256bit"] = new Dictionary<string, object?>()
                }
            };

            await File.WriteAllTextAsync(
                jobPath,
                JsonSerializer.Serialize(job),
                cancellationToken).ConfigureAwait(false);

            var startInfo = new ProcessStartInfo(qpdfPath);
            startInfo.ArgumentList.Add($"--job-json-file={jobPath}");

            var result = await ChildProcessRunner.RunAsync(
                startInfo,
                ProcessTimeout,
                cancellationToken).ConfigureAwait(false);

            if (result.ExitCode != 0 || !File.Exists(destinationFullPath))
            {
                TryDelete(destinationFullPath);
                throw new InvalidOperationException("qpdf could not create the protected PDF copy.");
            }
        }
        catch
        {
            TryDelete(destinationFullPath);
            throw;
        }
        finally
        {
            TryDelete(jobPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Best-effort cleanup. Product publication remains fail-closed.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup. Product publication remains fail-closed.
        }
    }
}
