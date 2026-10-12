using System.Reflection;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Ocr;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OcrWorkflowTests
{
    private const string PreflightTypeName = "SGPdf.App.Features.Ocr.OcrPreflight";
    private const string PreflightResultTypeName = "SGPdf.App.Features.Ocr.OcrPreflightResult";
    private const string WorkflowTypeName = "SGPdf.App.Features.Ocr.OcrWorkflow";

    [Fact]
    public void Preflight_ContractReturnsOcrSpecificResult()
    {
        var type = RequireType(PreflightTypeName);
        var method = RequireInspectMethod(type);

        Assert.Equal(PreflightResultTypeName, method.ReturnType.FullName);
    }

    [Fact]
    public void Preflight_OrdinarySourceCanProceed()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);
        var preflight = CreateDefaultPreflight();

        var result = Inspect(preflight, session, CancellationToken.None);

        Assert.True(CanProceed(result));
        Assert.DoesNotContain(Findings(result), finding => finding.Severity == PdfEditFindingSeverity.Block);
    }

    [Fact]
    public void Preflight_PropagatesPasswordBlock()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);
        var blocked = Blocked(
            PdfEditFindingKind.PasswordProtectedSource,
            "password blocked");
        var preflight = CreateInjectedPreflight((_, _) => blocked);

        var result = Inspect(preflight, session, CancellationToken.None);

        Assert.False(CanProceed(result));
        var finding = Assert.Single(Findings(result));
        Assert.Equal(PdfEditFindingKind.PasswordProtectedSource, finding.Kind);
        Assert.Equal(PdfEditFindingSeverity.Block, finding.Severity);
    }

    [Fact]
    public void Preflight_PropagatesCryptographicSignatureBlock()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);
        var blocked = Blocked(
            PdfEditFindingKind.CryptographicSignature,
            "signature blocked");
        var preflight = CreateInjectedPreflight((_, _) => blocked);

        var result = Inspect(preflight, session, CancellationToken.None);

        Assert.False(CanProceed(result));
        var finding = Assert.Single(Findings(result));
        Assert.Equal(PdfEditFindingKind.CryptographicSignature, finding.Kind);
        Assert.Equal(PdfEditFindingSeverity.Block, finding.Severity);
    }

    [Fact]
    public void Preflight_AlreadyCancelledDoesNotInvokeEditPreflight()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);
        var invoked = false;
        var preflight = CreateInjectedPreflight((_, _) =>
        {
            invoked = true;
            return Supported();
        });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        AssertInvocationThrows<OperationCanceledException>(() =>
            Inspect(preflight, session, cts.Token));

        Assert.False(invoked);
    }

    [Fact]
    public void Preflight_PassesSessionAndCancellationTokenToEditPreflight()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);
        PdfDocumentSession? observedSession = null;
        CancellationToken observedToken = default;
        using var cts = new CancellationTokenSource();
        var preflight = CreateInjectedPreflight((candidate, token) =>
        {
            observedSession = candidate;
            observedToken = token;
            return Supported();
        });

        var result = Inspect(preflight, session, cts.Token);

        Assert.True(CanProceed(result));
        Assert.Same(session, observedSession);
        Assert.Equal(cts.Token, observedToken);
    }

    [Fact]
    public async Task Workflow_DefaultSpanishRunsOneEligiblePageAndPublishesOnce()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("ocr-workflow-spanish.pdf");
        OcrLanguage? observedLanguage = null;
        var writeCalls = 0;
        var workflow = CreateInjectedWorkflow(
            fixture.SourcePath,
            destination,
            eligibility: (_, _, _) => Eligible(new[] { 0 }),
            recognizeAndPlan: (_, pageIndex, language, _) =>
            {
                observedLanguage = language;
                return Task.FromResult<IReadOnlyList<OcrTextLinePlacement>>(Placements(pageIndex, "Factura Bogotá"));
            },
            writer: (_, _, placements, output, _) =>
            {
                writeCalls++;
                Assert.Equal(destination, output);
                Assert.Single(placements);
                return PdfOcrWriteResult.Success;
            });

        var result = await RunWorkflowAsync(
            workflow,
            OcrRequest.Create(OcrPageScope.Automatic),
            CancellationToken.None);

        Assert.Equal(OcrLanguage.Spanish, observedLanguage);
        Assert.Equal(1, writeCalls);
        Assert.Equal("Succeeded", ResultKind(result));
        Assert.Equal(new[] { 0 }, ProcessedPages(result));
        Assert.Empty(SkippedPages(result));
    }

    [Theory]
    [InlineData(OcrLanguage.English)]
    [InlineData(OcrLanguage.SpanishEnglish)]
    public async Task Workflow_ForwardsRequestedLanguage(OcrLanguage language)
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output($"ocr-workflow-{language}.pdf");
        OcrLanguage? observedLanguage = null;
        var workflow = CreateInjectedWorkflow(
            fixture.SourcePath,
            destination,
            eligibility: (_, _, _) => Eligible(new[] { 0 }),
            recognizeAndPlan: (_, pageIndex, observed, _) =>
            {
                observedLanguage = observed;
                return Task.FromResult<IReadOnlyList<OcrTextLinePlacement>>(Placements(pageIndex, "Recognized text"));
            });

        var result = await RunWorkflowAsync(
            workflow,
            OcrRequest.Create(OcrPageScope.Automatic, language),
            CancellationToken.None);

        Assert.Equal(language, observedLanguage);
        Assert.Equal("Succeeded", ResultKind(result));
    }

    [Fact]
    public async Task Workflow_HybridProcessesOnlyCandidateAndReportsSkippedSearchablePage()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("ocr-workflow-hybrid.pdf");
        var rasterizedPages = new List<int>();
        IReadOnlyList<OcrTextLinePlacement>? writtenPlacements = null;
        var workflow = CreateInjectedWorkflow(
            fixture.SourcePath,
            destination,
            eligibility: (_, _, _) => Eligible(new[] { 1 }, new[] { 0 }),
            rasterize: (_, pageIndex, _) =>
            {
                rasterizedPages.Add(pageIndex);
                return Rendered(pageIndex);
            },
            recognizeAndPlan: (_, pageIndex, _, _) =>
                Task.FromResult<IReadOnlyList<OcrTextLinePlacement>>(Placements(pageIndex, "Scan page")),
            writer: (_, _, placements, _, _) =>
            {
                writtenPlacements = placements.ToArray();
                return PdfOcrWriteResult.Success;
            });

        var result = await RunWorkflowAsync(
            workflow,
            OcrRequest.Create(OcrPageScope.Automatic),
            CancellationToken.None);

        Assert.Equal(new[] { 1 }, rasterizedPages);
        var placement = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<OcrTextLinePlacement>>(writtenPlacements));
        Assert.Equal(1, placement.PageIndex);
        Assert.Equal(new[] { 1 }, ProcessedPages(result));
        Assert.Equal(new[] { 0 }, SkippedPages(result));
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(1, 3)]
    public async Task Workflow_ForwardsExplicitPageScope(int startPageIndex, int endPageIndex)
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output($"ocr-workflow-scope-{startPageIndex}-{endPageIndex}.pdf");
        OcrPageScope? observedScope = null;
        var requestedScope = startPageIndex == endPageIndex
            ? OcrPageScope.Current(startPageIndex)
            : OcrPageScope.Range(startPageIndex, endPageIndex);
        var workflow = CreateInjectedWorkflow(
            fixture.SourcePath,
            destination,
            eligibility: (_, scope, _) =>
            {
                observedScope = scope;
                return Eligible(new[] { startPageIndex });
            },
            recognizeAndPlan: (_, pageIndex, _, _) =>
                Task.FromResult<IReadOnlyList<OcrTextLinePlacement>>(Placements(pageIndex, "Scoped OCR")));

        var result = await RunWorkflowAsync(
            workflow,
            OcrRequest.Create(requestedScope),
            CancellationToken.None);

        Assert.Same(requestedScope, observedScope);
        Assert.Equal("Succeeded", ResultKind(result));
    }

    [Fact]
    public async Task Workflow_RotatedRasterIsForwardedUnchangedToRecognitionPlanning()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("ocr-workflow-rotated.pdf");
        var rotated = Rendered(0) with
        {
            DeviceTransform = new PdfPageDeviceTransform(
                612d,
                0d,
                0d,
                1d,
                -1d,
                0d,
                2550,
                3300)
        };
        PdfRenderedPage? observedRaster = null;
        var workflow = CreateInjectedWorkflow(
            fixture.SourcePath,
            destination,
            eligibility: (_, _, _) => Eligible(new[] { 0 }),
            rasterize: (_, _, _) => rotated,
            recognizeAndPlan: (raster, pageIndex, _, _) =>
            {
                observedRaster = raster;
                return Task.FromResult<IReadOnlyList<OcrTextLinePlacement>>(Placements(pageIndex, "Rotated OCR"));
            });

        var result = await RunWorkflowAsync(
            workflow,
            OcrRequest.Create(OcrPageScope.Automatic),
            CancellationToken.None);

        Assert.Same(rotated, observedRaster);
        Assert.Equal("Succeeded", ResultKind(result));
    }

    [Fact]
    public async Task Workflow_FullySearchableReturnsNoOpWithoutPublishingDestination()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("ocr-workflow-noop.pdf");
        File.WriteAllText(destination, "existing destination");
        var destinationBefore = File.ReadAllBytes(destination);
        var writerCalled = false;
        var rasterCalled = false;
        var workflow = CreateInjectedWorkflow(
            fixture.SourcePath,
            destination,
            eligibility: (_, _, _) => Eligible(Array.Empty<int>(), new[] { 0 }),
            rasterize: (_, pageIndex, _) =>
            {
                rasterCalled = true;
                return Rendered(pageIndex);
            },
            writer: (_, _, _, _, _) =>
            {
                writerCalled = true;
                return PdfOcrWriteResult.Success;
            });

        var result = await RunWorkflowAsync(
            workflow,
            OcrRequest.Create(OcrPageScope.Automatic),
            CancellationToken.None);

        Assert.Equal("NoOp", ResultKind(result));
        Assert.Empty(ProcessedPages(result));
        Assert.Equal(new[] { 0 }, SkippedPages(result));
        Assert.False(rasterCalled);
        Assert.False(writerCalled);
        Assert.Equal(destinationBefore, File.ReadAllBytes(destination));
    }

    private static object CreateDefaultPreflight()
    {
        var type = RequireType(PreflightTypeName);
        var constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);
        Assert.NotNull(constructor);
        var preflight = constructor!.Invoke(Array.Empty<object>());
        Assert.NotNull(preflight);
        return preflight!;
    }

    private static object CreateInjectedPreflight(
        Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult> editPreflight)
    {
        var type = RequireType(PreflightTypeName);
        var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(candidate =>
            {
                var parameters = candidate.GetParameters();
                return parameters.Length == 1 &&
                       parameters[0].ParameterType == typeof(Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult>);
            });
        Assert.NotNull(constructor);
        var preflight = constructor!.Invoke(new object[] { editPreflight });
        Assert.NotNull(preflight);
        return preflight!;
    }

    private static object CreateInjectedWorkflow(
        string sourcePath,
        string destinationPath,
        Func<PdfDocumentSession, CancellationToken, OcrPreflightResult>? preflight = null,
        Func<PdfDocumentSession, OcrPageScope, CancellationToken, OcrPageEligibilityResult>? eligibility = null,
        Func<PdfDocumentSession, int, CancellationToken, PdfRenderedPage>? rasterize = null,
        Func<PdfRenderedPage, int, OcrLanguage, CancellationToken, Task<IReadOnlyList<OcrTextLinePlacement>>>? recognizeAndPlan = null,
        Func<string, PdfEditSourceFingerprint, IReadOnlyList<OcrTextLinePlacement>, string, CancellationToken, PdfOcrWriteResult>? writer = null)
    {
        var type = RequireType(WorkflowTypeName);
        var parameterTypes = new[]
        {
            typeof(string),
            typeof(string),
            typeof(Func<PdfDocumentSession, CancellationToken, OcrPreflightResult>),
            typeof(Func<PdfDocumentSession, OcrPageScope, CancellationToken, OcrPageEligibilityResult>),
            typeof(Func<PdfDocumentSession, int, CancellationToken, PdfRenderedPage>),
            typeof(Func<PdfRenderedPage, int, OcrLanguage, CancellationToken, Task<IReadOnlyList<OcrTextLinePlacement>>>),
            typeof(Func<string, PdfEditSourceFingerprint, IReadOnlyList<OcrTextLinePlacement>, string, CancellationToken, PdfOcrWriteResult>)
        };
        var constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            parameterTypes,
            modifiers: null);
        Assert.NotNull(constructor);

        return constructor!.Invoke(new object[]
        {
            sourcePath,
            destinationPath,
            preflight ?? ((_, _) => new OcrPreflightResult(Array.Empty<PdfEditFinding>())),
            eligibility ?? ((_, _, _) => Eligible(new[] { 0 })),
            rasterize ?? ((_, pageIndex, _) => Rendered(pageIndex)),
            recognizeAndPlan ?? ((_, pageIndex, _, _) =>
                Task.FromResult<IReadOnlyList<OcrTextLinePlacement>>(Placements(pageIndex, "OCR"))),
            writer ?? ((_, _, _, _, _) => PdfOcrWriteResult.Success)
        });
    }

    private static async Task<object> RunWorkflowAsync(
        object workflow,
        OcrRequest request,
        CancellationToken cancellationToken)
    {
        var method = workflow.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(candidate =>
            {
                if (candidate.Name != "RunAsync")
                    return false;
                var parameters = candidate.GetParameters();
                return parameters.Length == 3 &&
                       parameters[0].ParameterType == typeof(OcrRequest) &&
                       parameters[2].ParameterType == typeof(CancellationToken);
            });
        Assert.NotNull(method);
        var invocation = method!.Invoke(workflow, new object?[] { request, null, cancellationToken });
        var task = Assert.IsAssignableFrom<Task>(invocation);
        await task;
        var resultProperty = task.GetType().GetProperty("Result");
        Assert.NotNull(resultProperty);
        var result = resultProperty!.GetValue(task);
        Assert.NotNull(result);
        return result!;
    }

    private static object Inspect(
        object preflight,
        PdfDocumentSession session,
        CancellationToken cancellationToken)
    {
        var method = RequireInspectMethod(preflight.GetType());
        var result = method.Invoke(preflight, new object[] { session, cancellationToken });
        Assert.NotNull(result);
        return result!;
    }

    private static MethodInfo RequireInspectMethod(Type type)
    {
        var method = type.GetMethod(
            "Inspect",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(PdfDocumentSession), typeof(CancellationToken) },
            modifiers: null);
        Assert.NotNull(method);
        return method!;
    }

    private static string ResultKind(object result)
    {
        var property = result.GetType().GetProperty(
            "Kind",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return property!.GetValue(result)?.ToString() ?? string.Empty;
    }

    private static IReadOnlyList<int> ProcessedPages(object result) =>
        PageIndices(result, "ProcessedPageIndices");

    private static IReadOnlyList<int> SkippedPages(object result) =>
        PageIndices(result, "SkippedSearchablePageIndices");

    private static IReadOnlyList<int> PageIndices(object result, string propertyName)
    {
        var property = result.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsAssignableFrom<IReadOnlyList<int>>(property!.GetValue(result));
    }

    private static OcrPageEligibilityResult Eligible(
        IReadOnlyList<int> candidates,
        IReadOnlyList<int>? skipped = null)
        => new(candidates, skipped ?? Array.Empty<int>());

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

    private static bool CanProceed(object result)
    {
        var property = result.GetType().GetProperty(
            "CanProceed",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsType<bool>(property!.GetValue(result));
    }

    private static IReadOnlyList<PdfEditFinding> Findings(object result)
    {
        var property = result.GetType().GetProperty(
            "Findings",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsAssignableFrom<IReadOnlyList<PdfEditFinding>>(property!.GetValue(result));
    }

    private static PdfEditPreflightResult Supported() =>
        new(Array.Empty<PdfEditFinding>());

    private static PdfEditPreflightResult Blocked(PdfEditFindingKind kind, string message) =>
        new(new[]
        {
            new PdfEditFinding(
                kind,
                PdfEditFindingSeverity.Block,
                message,
                PdfEditPreservationStatus.Unknown)
        });

    private static Type RequireType(string typeName)
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType(typeName, throwOnError: false);
        Assert.NotNull(type);
        return type!;
    }

    private static TException AssertInvocationThrows<TException>(Action action)
        where TException : Exception
    {
        var wrapper = Assert.Throws<TargetInvocationException>(action);
        return Assert.IsType<TException>(wrapper.InnerException);
    }
}
