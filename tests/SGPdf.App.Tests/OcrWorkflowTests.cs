using System.Reflection;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Ocr;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OcrWorkflowTests
{
    private const string PreflightTypeName = "SGPdf.App.Features.Ocr.OcrPreflight";

    [Fact]
    public void Preflight_OrdinarySourceCanProceed()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);
        var preflight = CreateDefaultPreflight();

        var result = Inspect(preflight, session, CancellationToken.None);

        Assert.True(result.CanProceed);
        Assert.DoesNotContain(result.Findings, finding => finding.Severity == PdfEditFindingSeverity.Block);
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

        Assert.False(result.CanProceed);
        var finding = Assert.Single(result.Findings);
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

        Assert.False(result.CanProceed);
        var finding = Assert.Single(result.Findings);
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

        Assert.True(result.CanProceed);
        Assert.Same(session, observedSession);
        Assert.Equal(cts.Token, observedToken);
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

    private static PdfEditPreflightResult Inspect(
        object preflight,
        PdfDocumentSession session,
        CancellationToken cancellationToken)
    {
        var method = preflight.GetType().GetMethod(
            "Inspect",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(PdfDocumentSession), typeof(CancellationToken) },
            modifiers: null);
        Assert.NotNull(method);
        var result = method!.Invoke(preflight, new object[] { session, cancellationToken });
        return Assert.IsType<PdfEditPreflightResult>(result);
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
