using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Ocr;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OcrProgressTests
{
    [Fact]
    public async Task Workflow_ProgressIsMonotonicAndReportsValidationAfterSuccessfulWrite()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("ocr-progress-order.pdf");
        var events = new List<OcrProgress>();
        var progress = new InlineProgress<OcrProgress>(events.Add);
        var workflow = CreateWorkflow(
            fixture.SourcePath,
            destination,
            candidates: new[] { 0, 1 });

        var result = await workflow.RunAsync(
            OcrRequest.Create(OcrPageScope.Automatic),
            progress,
            CancellationToken.None);

        Assert.Equal(OcrResultKind.Succeeded, result.Kind);
        Assert.Collection(
            events,
            item => AssertProgress(item, 1, 2, 0, OcrProgressStage.Rendering),
            item => AssertProgress(item, 1, 2, 0, OcrProgressStage.Recognizing),
            item => AssertProgress(item, 2, 2, 1, OcrProgressStage.Rendering),
            item => AssertProgress(item, 2, 2, 1, OcrProgressStage.Recognizing),
            item => AssertProgress(item, 2, 2, 1, OcrProgressStage.Writing),
            item => AssertProgress(item, 2, 2, 1, OcrProgressStage.Validating));
    }

    [Fact]
    public async Task Workflow_DoesNotAdvanceToNextPageBeforeCurrentRecognitionCompletes()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("ocr-progress-page-boundary.pdf");
        var events = new List<OcrProgress>();
        var progress = new InlineProgress<OcrProgress>(events.Add);
        var pageZeroEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releasePageZero = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var workflow = CreateWorkflow(
            fixture.SourcePath,
            destination,
            candidates: new[] { 0, 1 },
            recognizeAndPlan: async (_, pageIndex, _, _) =>
            {
                if (pageIndex == 0)
                {
                    pageZeroEntered.TrySetResult();
                    await releasePageZero.Task;
                }

                return Placements(pageIndex, $"OCR page {pageIndex + 1}");
            });

        var runTask = workflow.RunAsync(
            OcrRequest.Create(OcrPageScope.Automatic),
            progress,
            CancellationToken.None);

        await pageZeroEntered.Task;
        Assert.DoesNotContain(events, item => item.PageIndex == 1);

        releasePageZero.TrySetResult();
        var result = await runTask;

        Assert.Equal(OcrResultKind.Succeeded, result.Kind);
        var pageOneRenderingIndex = events.FindIndex(item =>
            item.PageIndex == 1 && item.Stage == OcrProgressStage.Rendering);
        var pageZeroRecognizingIndex = events.FindIndex(item =>
            item.PageIndex == 0 && item.Stage == OcrProgressStage.Recognizing);
        Assert.True(pageZeroRecognizingIndex >= 0);
        Assert.True(pageOneRenderingIndex > pageZeroRecognizingIndex);
    }

    private static OcrWorkflow CreateWorkflow(
        string sourcePath,
        string destinationPath,
        IReadOnlyList<int> candidates,
        Func<PdfRenderedPage, int, OcrLanguage, CancellationToken, Task<IReadOnlyList<OcrTextLinePlacement>>>? recognizeAndPlan = null)
    {
        return new OcrWorkflow(
            sourcePath,
            destinationPath,
            preflight: (_, _) => new OcrPreflightResult(Array.Empty<PdfEditFinding>()),
            eligibility: (_, _, _) => new OcrPageEligibilityResult(candidates, Array.Empty<int>()),
            rasterize: (_, pageIndex, _) => Rendered(pageIndex),
            recognizeAndPlan: recognizeAndPlan ?? ((_, pageIndex, _, _) =>
                Task.FromResult<IReadOnlyList<OcrTextLinePlacement>>(Placements(pageIndex, $"OCR page {pageIndex + 1}"))),
            writer: (_, _, _, _, _) => PdfOcrWriteResult.Success);
    }

    private static void AssertProgress(
        OcrProgress actual,
        int currentOrdinal,
        int totalEligiblePages,
        int pageIndex,
        OcrProgressStage stage)
    {
        Assert.Equal(currentOrdinal, actual.CurrentOrdinal);
        Assert.Equal(totalEligiblePages, actual.TotalEligiblePages);
        Assert.Equal(pageIndex, actual.PageIndex);
        Assert.Equal(stage, actual.Stage);
    }

    private static PdfRenderedPage Rendered(int pageIndex) =>
        new(pageIndex, 8, 8, 32, OcrRasterizer.Dpi, new byte[256]);

    private static OcrTextLinePlacement[] Placements(int pageIndex, string text)
    {
        var bounds = new PdfOcrRect(48d, 120d, 160d, 142d);
        var word = new OcrTextWordPlacement(text, 95d, bounds, 0, 1);
        return new[]
        {
            new OcrTextLinePlacement(
                pageIndex,
                text,
                bounds,
                new[] { word },
                0,
                1,
                1,
                1)
        };
    }

    private sealed class InlineProgress<T>(Action<T> callback) : IProgress<T>
    {
        private readonly Action<T> _callback = callback ?? throw new ArgumentNullException(nameof(callback));

        public void Report(T value) => _callback(value);
    }
}
