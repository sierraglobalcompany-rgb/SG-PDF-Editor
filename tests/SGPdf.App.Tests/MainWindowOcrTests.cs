using System.Reflection;
using System.Windows.Controls;
using SGPdf.App.Features.Ocr;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowOcrTests
{
    [Fact]
    public void Herramientas_ContainsExactlyOneOcrCommandWithApprovedLabel()
    {
        OrganizeWindowTestHost.RunInSta(() =>
        {
            var window = new MainWindow();
            OrganizeWindowTestHost.EnsureLoaded(window);
            try
            {
                var tools = OrganizeWindowTestHost.Element<MenuItem>(window, "UtilitiesMenuItem");
                var matches = tools.Items.OfType<MenuItem>()
                    .Where(item => string.Equals(item.Header as string, "OCR / Hacer PDF buscable…", StringComparison.Ordinal))
                    .ToArray();

                var ocr = Assert.Single(matches);
                Assert.Equal("OcrMenuItem", ocr.Name);
                Assert.False(ocr.IsEnabled);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void SaveAsCancellation_DoesNotInvokeWorkflow()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            var calls = 0;
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                SetRunnerOverride(window, (_, _, _, _, _) =>
                {
                    calls++;
                    return Task.FromResult(OcrResult.Success(new[] { 0 }, Array.Empty<int>()));
                });

                var result = await InvokeRunAsync(window, OcrRequest.Create(OcrPageScope.Automatic), null);

                Assert.False(result);
                Assert.Equal(0, calls);
                Assert.Contains("cancel", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void NoOp_ShowsAlreadySearchableMessage_AndRecoversUi()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                SetRunnerOverride(window, (_, _, _, _, _) =>
                    Task.FromResult(OcrResult.NoOp(new[] { 0, 1 })));

                var result = await InvokeRunAsync(window, OcrRequest.Create(OcrPageScope.Automatic), Path.Combine(fixture.DirectoryPath, "noop.pdf"));

                Assert.False(result);
                Assert.True(OrganizeWindowTestHost.Element<MenuItem>(window, "OcrMenuItem").IsEnabled);
                Assert.Contains("buscable", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void Success_ShowsProcessedSkippedSummary_AndRecoversUi()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                SetRunnerOverride(window, (_, _, _, progress, _) =>
                {
                    progress?.Report(new OcrProgress(1, 1, 0, OcrProgressStage.Recognizing));
                    return Task.FromResult(OcrResult.Success(new[] { 0 }, new[] { 1 }));
                });

                var result = await InvokeRunAsync(window, OcrRequest.Create(OcrPageScope.Automatic), fixture.Path);

                Assert.True(result);
                Assert.True(OrganizeWindowTestHost.Element<MenuItem>(window, "OcrMenuItem").IsEnabled);
                var status = OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text;
                Assert.Contains("1", status, StringComparison.Ordinal);
                Assert.Contains("omit", status, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void RunningOcr_DisablesCommand_CancelCancelsToken_AndRecoversUi()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                SetRunnerOverride(window, async (_, _, _, progress, token) =>
                {
                    progress?.Report(new OcrProgress(1, 2, 0, OcrProgressStage.Recognizing));
                    entered.TrySetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    return OcrResult.Success(new[] { 0 }, Array.Empty<int>());
                });

                var run = InvokeRunAsync(window, OcrRequest.Create(OcrPageScope.Automatic), Path.Combine(fixture.DirectoryPath, "cancelled.pdf"));
                await entered.Task;

                Assert.False(OrganizeWindowTestHost.Element<MenuItem>(window, "OcrMenuItem").IsEnabled);
                InvokeByName(window, "CancelOcr");

                Assert.False(await run);
                Assert.True(OrganizeWindowTestHost.Element<MenuItem>(window, "OcrMenuItem").IsEnabled);
                Assert.Contains("cancel", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void WorkflowFailure_ReportsError_AndRecoversUi()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                SetRunnerOverride(window, (_, _, _, _, _) =>
                    Task.FromException<OcrResult>(new InvalidOperationException("boom")));

                Assert.False(await InvokeRunAsync(window, OcrRequest.Create(OcrPageScope.Automatic), Path.Combine(fixture.DirectoryPath, "failed.pdf")));
                Assert.True(OrganizeWindowTestHost.Element<MenuItem>(window, "OcrMenuItem").IsEnabled);
                Assert.Contains("no se pudo", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    private static void SetRunnerOverride(
        MainWindow window,
        Func<string, string, OcrRequest, IProgress<OcrProgress>?, CancellationToken, Task<OcrResult>> runner)
    {
        var field = typeof(MainWindow).GetField("_ocrWorkflowRunnerOverride", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(window, runner);
    }

    private static Task<bool> InvokeRunAsync(MainWindow window, OcrRequest request, string? destinationPath)
        => Assert.IsType<Task<bool>>(InvokeByName(window, "TryRunOcrRequestAsync", request, destinationPath));

    private static object? InvokeByName(MainWindow window, string name, params object?[] args)
    {
        var method = typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(candidate => candidate.Name == name && candidate.GetParameters().Length == args.Length);
        Assert.NotNull(method);
        return method.Invoke(window, args);
    }
}
