using System.Diagnostics;
using SGPdf.App.Features.Labels;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ChildProcessRunnerTests
{
    [Fact]
    public async Task RunAsync_CapturesStandardOutputAndError()
    {
        var startInfo = CreateCmdStartInfo("echo SG-OUT & echo SG-ERR 1>&2");

        var result = await ChildProcessRunner.RunAsync(
            startInfo,
            TimeSpan.FromSeconds(5),
            CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("SG-OUT", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("SG-ERR", result.StandardError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WhenTimeoutExpires_KillsProcessAndThrowsTimeout()
    {
        var startInfo = CreatePingStartInfo();
        var stopwatch = Stopwatch.StartNew();

        await Assert.ThrowsAsync<TimeoutException>(() => ChildProcessRunner.RunAsync(
            startInfo,
            TimeSpan.FromMilliseconds(75),
            CancellationToken.None));

        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), $"Timeout took {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_KillsProcessAndThrowsCancellation()
    {
        var startInfo = CreatePingStartInfo();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(75));
        var stopwatch = Stopwatch.StartNew();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ChildProcessRunner.RunAsync(
            startInfo,
            TimeSpan.FromSeconds(5),
            cancellation.Token));

        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), $"Cancellation took {stopwatch.Elapsed}.");
    }

    private static ProcessStartInfo CreateCmdStartInfo(string command)
    {
        var startInfo = new ProcessStartInfo("cmd.exe");
        startInfo.ArgumentList.Add("/d");
        startInfo.ArgumentList.Add("/s");
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add(command);
        return startInfo;
    }

    private static ProcessStartInfo CreatePingStartInfo()
    {
        var startInfo = new ProcessStartInfo("ping.exe");
        startInfo.ArgumentList.Add("127.0.0.1");
        startInfo.ArgumentList.Add("-n");
        startInfo.ArgumentList.Add("6");
        return startInfo;
    }
}
