using System.Reflection;
using SGPdf.App.Features.Edit;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfUtilityPreflightTests
{
    private const string PreflightTypeName = "SGPdf.App.Features.Utilities.PdfUtilityPreflight";

    [Fact]
    public void Inspect_PlainPdfReusesSupportedEditPolicy()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);
        var preflight = CreatePreflight();

        var result = Inspect(preflight, session, CancellationToken.None);

        Assert.True(result.CanEdit);
        Assert.Empty(result.Risks);
    }

    [Fact]
    public void Inspect_CancellationPropagatesBeforeInspection()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);
        var preflight = CreatePreflight();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var wrapper = Assert.Throws<TargetInvocationException>(() => Inspect(preflight, session, cts.Token));
        Assert.IsType<OperationCanceledException>(wrapper.InnerException);
    }

    private static object CreatePreflight()
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType(PreflightTypeName, throwOnError: false);
        Assert.NotNull(type);
        var instance = Activator.CreateInstance(type!, nonPublic: true);
        Assert.NotNull(instance);
        return instance!;
    }

    private static PdfEditPreflightResult Inspect(
        object preflight,
        PdfDocumentSession session,
        CancellationToken cancellationToken)
    {
        var method = preflight.GetType().GetMethod(
            "Inspect",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var result = method!.Invoke(preflight, new object[] { session, cancellationToken });
        return Assert.IsType<PdfEditPreflightResult>(result);
    }
}
