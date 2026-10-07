using System.Diagnostics;
using System.IO;

namespace SGPdf.App.Features.Labels;

internal sealed record ChildProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);

internal static class ChildProcessRunner
{
    public static async Task<ChildProcessResult> RunAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Process timeout must be positive.");

        cancellationToken.ThrowIfCancellationRequested();

        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException($"Could not start process '{startInfo.FileName}'.");

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        using var timeoutCancellation = new CancellationTokenSource(timeout);
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCancellation.Token);

        try
        {
            await process.WaitForExitAsync(linkedCancellation.Token).ConfigureAwait(false);

            var standardOutput = await standardOutputTask.ConfigureAwait(false);
            var standardError = await standardErrorTask.ConfigureAwait(false);
            return new ChildProcessResult(process.ExitCode, standardOutput, standardError);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested || timeoutCancellation.IsCancellationRequested)
        {
            TryKillProcessTree(process);

            try
            {
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                // The process may already have exited between cancellation and cleanup.
            }

            await DrainAsync(standardOutputTask).ConfigureAwait(false);
            await DrainAsync(standardErrorTask).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            throw new TimeoutException(
                $"Process '{startInfo.FileName}' exceeded the timeout of {timeout.TotalMilliseconds:0} ms.");
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
            // The process already exited.
        }
    }

    private static async Task DrainAsync(Task<string> streamTask)
    {
        try
        {
            await streamTask.ConfigureAwait(false);
        }
        catch (IOException)
        {
            // A killed process can close redirected pipes while they are being drained.
        }
        catch (ObjectDisposedException)
        {
            // Process cleanup can dispose redirected streams after termination.
        }
    }
}
