using System.Reflection;
using System.Text;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Ocr;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OcrHardeningTests
{
    private const string WorkflowTypeName = "SGPdf.App.Features.Ocr.OcrWorkflow";

    [Fact]
    public async Task Workflow_RenderFailurePreservesDestinationAndCleansWorkspace()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = ExistingDestination(fixture, "hardening-render.pdf");
        var destinationBefore = File.ReadAllBytes(destination);
        var workspace = fixture.Output("ocr-work-render");
        var writerCalled = false;
        var workflow = CreateHardenedWorkflow(
            fixture.SourcePath,
            destination,
            workspace,
            rasterize: (_, _, _) =>
            {
                Assert.True(Directory.Exists(workspace));
                File.WriteAllText(Path.Combine(workspace, "page-0.bmp"), "temp raster");
                throw new InvalidOperationException("render failed");
            },
            writer: (_, _, _, _, _) =>
            {
                writerCalled = true;
                return PdfOcrWriteResult.Success;
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RunWorkflowAsync(workflow, CancellationToken.None));

        Assert.False(writerCalled);
        Assert.Equal(destinationBefore, File.ReadAllBytes(destination));
        Assert.False(Directory.Exists(workspace));
    }

    [Theory]
    [InlineData("tesseract-exit")]
    [InlineData("timeout")]
    [InlineData("invalid-utf8")]
    [InlineData("malformed-tsv")]
    public async Task Workflow_RecognitionOrParserFailurePreservesDestinationAndCleansWorkspace(string failureKind)
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = ExistingDestination(fixture, $"hardening-{failureKind}.pdf");
        var destinationBefore = File.ReadAllBytes(destination);
        var workspace = fixture.Output($"ocr-work-{failureKind}");
        var writerCalled = false;
        var workflow = CreateHardenedWorkflow(
            fixture.SourcePath,
            destination,
            workspace,
            recognizeAndPlan: (_, _, _, workPath, _) =>
            {
                Assert.Equal(workspace, workPath);
                File.WriteAllText(Path.Combine(workspace, "page-0.bmp"), "temp raster");
                File.WriteAllText(Path.Combine(workspace, "page-0.tsv"), "temp tsv");
                return failureKind switch
                {
                    "tesseract-exit" => Task.FromException<IReadOnlyList<OcrTextLinePlacement>>(
                        new InvalidDataException("Tesseract exit 1")),
                    "timeout" => Task.FromException<IReadOnlyList<OcrTextLinePlacement>>(
                        new TimeoutException("Tesseract timeout")),
                    "invalid-utf8" => Task.FromException<IReadOnlyList<OcrTextLinePlacement>>(
                        new DecoderFallbackException("invalid utf8")),
                    "malformed-tsv" => Task.FromException<IReadOnlyList<OcrTextLinePlacement>>(
                        new FormatException("malformed tsv")),
                    _ => throw new ArgumentOutOfRangeException(nameof(failureKind))
                };
            },
            writer: (_, _, _, _, _) =>
            {
                writerCalled = true;
                return PdfOcrWriteResult.Success;
            });

        await Assert.ThrowsAnyAsync<Exception>(() =>
            RunWorkflowAsync(workflow, CancellationToken.None));

        Assert.False(writerCalled);
        Assert.Equal(destinationBefore, File.ReadAllBytes(destination));
        Assert.False(Directory.Exists(workspace));
    }

    [Fact]
    public async Task Workflow_LaterPageFailureDoesNotPublishEarlierPlacementsAndCleansWorkspace()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = ExistingDestination(fixture, "hardening-page-n.pdf");
        var destinationBefore = File.ReadAllBytes(destination);
        var workspace = fixture.Output("ocr-work-page-n");
        var writerCalled = false;
        var workflow = CreateHardenedWorkflow(
            fixture.SourcePath,
            destination,
            workspace,
            eligibility: (_, _, _) => new OcrPageEligibilityResult(new[] { 0, 1 }, Array.Empty<int>()),
            recognizeAndPlan: (_, pageIndex, _, _, _) =>
            {
                File.WriteAllText(Path.Combine(workspace, $"page-{pageIndex}.tsv"), "temp tsv");
                if (pageIndex == 1)
                {
                    return Task.FromException<IReadOnlyList<OcrTextLinePlacement>>(
                        new InvalidDataException("page 2 failed"));
                }

                return Task.FromResult<IReadOnlyList<OcrTextLinePlacement>>(
                    Placements(pageIndex, "first page OCR"));
            },
            writer: (_, _, _, _, _) =>
            {
                writerCalled = true;
                return PdfOcrWriteResult.Success;
            });

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            RunWorkflowAsync(workflow, CancellationToken.None));

        Assert.False(writerCalled);
        Assert.Equal(destinationBefore, File.ReadAllBytes(destination));
        Assert.False(Directory.Exists(workspace));
    }

    [Fact]
    public async Task Workflow_WriterValidationFailurePreservesDestinationAndCleansWorkspace()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = ExistingDestination(fixture, "hardening-writer-validation.pdf");
        var destinationBefore = File.ReadAllBytes(destination);
        var workspace = fixture.Output("ocr-work-writer-validation");
        var writerCalls = 0;
        var workflow = CreateHardenedWorkflow(
            fixture.SourcePath,
            destination,
            workspace,
            writer: (_, _, _, _, _) =>
            {
                writerCalls++;
                Assert.Equal(destinationBefore, File.ReadAllBytes(destination));
                File.WriteAllText(Path.Combine(workspace, "writer-validation.marker"), "temporary");
                return PdfOcrWriteResult.Failure("semantic validation failed");
            });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RunWorkflowAsync(workflow, CancellationToken.None));

        Assert.Contains("semantic validation failed", error.Message, StringComparison.Ordinal);
        Assert.Equal(1, writerCalls);
        Assert.Equal(destinationBefore, File.ReadAllBytes(destination));
        Assert.False(Directory.Exists(workspace));
    }

    [Fact]
    public async Task Workflow_StaleSourceFingerprintIsForwardedFailClosedAndCleansWorkspace()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = ExistingDestination(fixture, "hardening-stale-source.pdf");
        var destinationBefore = File.ReadAllBytes(destination);
        var workspace = fixture.Output("ocr-work-stale-source");
        var writerCalls = 0;
        var workflow = CreateHardenedWorkflow(
            fixture.SourcePath,
            destination,
            workspace,
            writer: (source, fingerprint, _, _, _) =>
            {
                writerCalls++;
                File.AppendAllText(source, "\n% stale after OCR\n");
                Assert.False(fingerprint.MatchesCurrentFile(source));
                return PdfOcrWriteResult.Failure("source changed");
            });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RunWorkflowAsync(workflow, CancellationToken.None));

        Assert.Contains("source changed", error.Message, StringComparison.Ordinal);
        Assert.Equal(1, writerCalls);
        Assert.Equal(destinationBefore, File.ReadAllBytes(destination));
        Assert.False(Directory.Exists(workspace));
    }

    [Fact]
    public async Task Workflow_CancellationDuringRenderPublishesNothingAndCleansWorkspace()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = ExistingDestination(fixture, "hardening-cancel-render.pdf");
        var destinationBefore = File.ReadAllBytes(destination);
        var workspace = fixture.Output("ocr-work-cancel-render");
        var writerCalled = false;
        using var cts = new CancellationTokenSource();
        var workflow = CreateHardenedWorkflow(
            fixture.SourcePath,
            destination,
            workspace,
            rasterize: (_, _, token) =>
            {
                File.WriteAllText(Path.Combine(workspace, "page-0.bmp"), "temp raster");
                cts.Cancel();
                token.ThrowIfCancellationRequested();
                return Rendered(0);
            },
            writer: (_, _, _, _, _) =>
            {
                writerCalled = true;
                return PdfOcrWriteResult.Success;
            });

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            RunWorkflowAsync(workflow, cts.Token));

        Assert.False(writerCalled);
        Assert.Equal(destinationBefore, File.ReadAllBytes(destination));
        Assert.False(Directory.Exists(workspace));
    }

    [Fact]
    public async Task Workflow_CancellationDuringRecognitionPublishesNothingAndCleansWorkspace()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = ExistingDestination(fixture, "hardening-cancel-recognition.pdf");
        var destinationBefore = File.ReadAllBytes(destination);
        var workspace = fixture.Output("ocr-work-cancel-recognition");
        var writerCalled = false;
        using var cts = new CancellationTokenSource();
        var workflow = CreateHardenedWorkflow(
            fixture.SourcePath,
            destination,
            workspace,
            recognizeAndPlan: (_, _, _, _, token) =>
            {
                File.WriteAllText(Path.Combine(workspace, "page-0.tsv"), "temp tsv");
                cts.Cancel();
                token.ThrowIfCancellationRequested();
                return Task.FromResult<IReadOnlyList<OcrTextLinePlacement>>(Array.Empty<OcrTextLinePlacement>());
            },
            writer: (_, _, _, _, _) =>
            {
                writerCalled = true;
                return PdfOcrWriteResult.Success;
            });

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            RunWorkflowAsync(workflow, cts.Token));

        Assert.False(writerCalled);
        Assert.Equal(destinationBefore, File.ReadAllBytes(destination));
        Assert.False(Directory.Exists(workspace));
    }

    [Fact]
    public async Task Workflow_CancellationDuringWritePreservesDestinationAndCleansWorkspace()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = ExistingDestination(fixture, "hardening-cancel-write.pdf");
        var destinationBefore = File.ReadAllBytes(destination);
        var workspace = fixture.Output("ocr-work-cancel-write");
        using var cts = new CancellationTokenSource();
        var workflow = CreateHardenedWorkflow(
            fixture.SourcePath,
            destination,
            workspace,
            writer: (_, _, _, _, token) =>
            {
                File.WriteAllText(Path.Combine(workspace, "writer.tmp"), "temporary");
                cts.Cancel();
                token.ThrowIfCancellationRequested();
                return PdfOcrWriteResult.Success;
            });

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            RunWorkflowAsync(workflow, cts.Token));

        Assert.Equal(destinationBefore, File.ReadAllBytes(destination));
        Assert.False(Directory.Exists(workspace));
    }

    [Fact]
    public async Task Workflow_BlockedPreflightDoesNotCreateWorkspaceOrRasterize()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("hardening-blocked.pdf");
        var workspace = fixture.Output("ocr-work-blocked");
        var rasterCalled = false;
        var workspaceFactoryCalled = false;
        var type = RequireType(WorkflowTypeName);
        var workflow = CreateHardenedWorkflowCore(
            type,
            fixture.SourcePath,
            destination,
            () =>
            {
                workspaceFactoryCalled = true;
                return workspace;
            },
            preflight: (_, _) => new OcrPreflightResult(new[]
            {
                new PdfEditFinding(
                    PdfEditFindingKind.CryptographicSignature,
                    PdfEditFindingSeverity.Block,
                    "signed blocked",
                    PdfEditPreservationStatus.Unknown)
            }),
            eligibility: (_, _, _) => new OcrPageEligibilityResult(new[] { 0 }, Array.Empty<int>()),
            rasterize: (_, pageIndex, _) =>
            {
                rasterCalled = true;
                return Rendered(pageIndex);
            },
            recognizeAndPlan: (_, pageIndex, _, _, _) =>
                Task.FromResult<IReadOnlyList<OcrTextLinePlacement>>(Placements(pageIndex, "OCR")),
            writer: (_, _, _, _, _) => PdfOcrWriteResult.Success);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RunWorkflowAsync(workflow, CancellationToken.None));

        Assert.False(workspaceFactoryCalled);
        Assert.False(rasterCalled);
        Assert.False(Directory.Exists(workspace));
        Assert.False(File.Exists(destination));
    }

    private static object CreateHardenedWorkflow(
        string sourcePath,
        string destinationPath,
        string workspace,
        Func<PdfDocumentSession, CancellationToken, OcrPreflightResult>? preflight = null,
        Func<PdfDocumentSession, OcrPageScope, CancellationToken, OcrPageEligibilityResult>? eligibility = null,
        Func<PdfDocumentSession, int, CancellationToken, PdfRenderedPage>? rasterize = null,
        Func<PdfRenderedPage, int, OcrLanguage, string, CancellationToken, Task<IReadOnlyList<OcrTextLinePlacement>>>? recognizeAndPlan = null,
        Func<string, PdfEditSourceFingerprint, IReadOnlyList<OcrTextLinePlacement>, string, CancellationToken, PdfOcrWriteResult>? writer = null)
    {
        var type = RequireType(WorkflowTypeName);
        return CreateHardenedWorkflowCore(
            type,
            sourcePath,
            destinationPath,
            () => workspace,
            preflight ?? ((_, _) => new OcrPreflightResult(Array.Empty<PdfEditFinding>())),
            eligibility ?? ((_, _, _) => new OcrPageEligibilityResult(new[] { 0 }, Array.Empty<int>())),
            rasterize ?? ((_, pageIndex, _) => Rendered(pageIndex)),
            recognizeAndPlan ?? ((_, pageIndex, _, _, _) =>
                Task.FromResult<IReadOnlyList<OcrTextLinePlacement>>(Placements(pageIndex, "OCR"))),
            writer ?? ((_, _, _, _, _) => PdfOcrWriteResult.Success));
    }

    private static object CreateHardenedWorkflowCore(
        Type type,
        string sourcePath,
        string destinationPath,
        Func<string> workspacePathFactory,
        Func<PdfDocumentSession, CancellationToken, OcrPreflightResult> preflight,
        Func<PdfDocumentSession, OcrPageScope, CancellationToken, OcrPageEligibilityResult> eligibility,
        Func<PdfDocumentSession, int, CancellationToken, PdfRenderedPage> rasterize,
        Func<PdfRenderedPage, int, OcrLanguage, string, CancellationToken, Task<IReadOnlyList<OcrTextLinePlacement>>> recognizeAndPlan,
        Func<string, PdfEditSourceFingerprint, IReadOnlyList<OcrTextLinePlacement>, string, CancellationToken, PdfOcrWriteResult> writer)
    {
        var parameterTypes = new[]
        {
            typeof(string),
            typeof(string),
            typeof(Func<string>),
            typeof(Func<PdfDocumentSession, CancellationToken, OcrPreflightResult>),
            typeof(Func<PdfDocumentSession, OcrPageScope, CancellationToken, OcrPageEligibilityResult>),
            typeof(Func<PdfDocumentSession, int, CancellationToken, PdfRenderedPage>),
            typeof(Func<PdfRenderedPage, int, OcrLanguage, string, CancellationToken, Task<IReadOnlyList<OcrTextLinePlacement>>>),
            typeof(Func<string, PdfEditSourceFingerprint, IReadOnlyList<OcrTextLinePlacement>, string, CancellationToken, PdfOcrWriteResult>)
        };
        var constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            parameterTypes,
            modifiers: null);
        Assert.NotNull(constructor);
        var workflow = constructor!.Invoke(new object[]
        {
            sourcePath,
            destinationPath,
            workspacePathFactory,
            preflight,
            eligibility,
            rasterize,
            recognizeAndPlan,
            writer
        });
        Assert.NotNull(workflow);
        return workflow!;
    }

    private static async Task<object> RunWorkflowAsync(object workflow, CancellationToken cancellationToken)
    {
        var method = workflow.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(candidate =>
            {
                if (candidate.Name != "RunAsync")
                    return false;
                var parameters = candidate.GetParameters();
                return parameters.Length == 3 &&
                       parameters[0].ParameterType == typeof(OcrRequest) &&
                       parameters[2].ParameterType == typeof(CancellationToken);
            });
        var invocation = method.Invoke(
            workflow,
            new object?[] { OcrRequest.Create(OcrPageScope.Automatic), null, cancellationToken });
        var task = Assert.IsAssignableFrom<Task>(invocation);
        await task;
        var resultProperty = task.GetType().GetProperty("Result");
        Assert.NotNull(resultProperty);
        return resultProperty!.GetValue(task)!;
    }

    private static string ExistingDestination(UtilityOverlayFixture fixture, string fileName)
    {
        var destination = fixture.Output(fileName);
        File.WriteAllText(destination, "existing destination");
        return destination;
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

    private static Type RequireType(string typeName)
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType(typeName, throwOnError: false);
        Assert.NotNull(type);
        return type!;
    }
}
