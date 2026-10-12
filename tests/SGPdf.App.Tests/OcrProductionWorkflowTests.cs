using System.Reflection;
using SGPdf.App.Features.Ocr;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OcrProductionWorkflowTests
{
    private const string WorkflowTypeName = "SGPdf.App.Features.Ocr.OcrWorkflow";

    [Fact]
    public async Task ProductionWorkflow_ImageOnlySpanishScanBecomesSearchableWithoutMutatingSource()
    {
        using var fixture = OcrInvisibleTextNativeHarness.Characterize(rotate90: false);
        var destination = Path.Combine(fixture.DirectoryPath, "production-workflow.pdf");
        var sourceBefore = File.ReadAllBytes(fixture.SourcePath);
        var workflow = CreateProductionWorkflow(fixture.SourcePath, destination);

        var result = await RunWorkflowAsync(
            workflow,
            OcrRequest.Create(OcrPageScope.Automatic),
            CancellationToken.None);

        Assert.Equal(OcrResultKind.Succeeded, result.Kind);
        Assert.Equal(new[] { 0 }, result.ProcessedPageIndices);
        Assert.Empty(result.SkippedPageIndices);
        Assert.True(File.Exists(destination));
        Assert.Equal(sourceBefore, File.ReadAllBytes(fixture.SourcePath));

        using var output = PdfDocumentSession.Open(destination);
        Assert.NotEmpty(output.FindTextOnPage(0, "12345"));
        Assert.NotEmpty(output.FindTextOnPage(0, "Bogotá"));
    }

    private static object CreateProductionWorkflow(string sourcePath, string destinationPath)
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType(WorkflowTypeName, throwOnError: false);
        Assert.NotNull(type);
        var constructor = type!.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(string), typeof(string) },
            modifiers: null);
        Assert.NotNull(constructor);

        var workflow = constructor!.Invoke(new object[] { sourcePath, destinationPath });
        Assert.NotNull(workflow);
        return workflow!;
    }

    private static async Task<OcrResult> RunWorkflowAsync(
        object workflow,
        OcrRequest request,
        CancellationToken cancellationToken)
    {
        var method = workflow.GetType().GetMethod(
            "RunAsync",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(OcrRequest), typeof(IProgress<OcrProgress>), typeof(CancellationToken) },
            modifiers: null);
        Assert.NotNull(method);

        var invocation = method!.Invoke(workflow, new object?[] { request, null, cancellationToken });
        var task = Assert.IsAssignableFrom<Task<OcrResult>>(invocation);
        return await task;
    }
}
