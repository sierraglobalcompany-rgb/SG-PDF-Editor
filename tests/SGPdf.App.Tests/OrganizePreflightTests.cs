using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OrganizePreflightTests
{
    private static readonly Assembly AppAssembly = typeof(PdfDocumentSession).Assembly;

    [Fact]
    public void PinnedPdfium_ExportsTask3DetectorsUsedByPreflight()
    {
        var required = new[]
        {
            "FPDF_GetSignatureCount",
            "FPDF_GetFormType",
            "FPDF_CountNamedDests",
            "FPDFCatalog_IsTagged",
            "FPDFDoc_GetAttachmentCount",
            "FPDF_GetPageLabel",
            "FPDF_GetMetaText"
        };

        Assert.True(NativeLibrary.TryLoad("pdfium", out var library), "No se pudo cargar el pdfium.dll pinneado por el proyecto.");
        try
        {
            var missing = required.Where(name => !NativeLibrary.TryGetExport(library, name, out _)).ToArray();
            Assert.True(missing.Length == 0, $"Faltan detectores PDFium requeridos por Task 3: {string.Join(", ", missing)}");
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }

    [Fact]
    public void PlainUnsignedUnprotected_CanProceedWithoutWarning()
    {
        using var fixture = OrganizePdfFixtureFactory.CreatePlain();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var result = Inspect(session);

        Assert.True(ReadBool(result, "CanProceed"));
        Assert.False(ReadBool(result, "RequiresWarningConfirmation"));
        Assert.Empty(ReadFindings(result));
    }

    [Fact]
    public void SignedPolicy_AlwaysBlocks_WithInjectedSignatureCount()
    {
        using var fixture = OrganizePdfFixtureFactory.CreatePlain();
        using var session = PdfDocumentSession.Open(fixture.Path);
        Func<PdfDocumentSession, CancellationToken, int> signatureReader = (_, _) => 1;

        var result = Inspect(session, signatureReader);

        Assert.False(ReadBool(result, "CanProceed"));
        AssertFinding(result, "CryptographicSignature", "Block", expectedPreservation: null);
    }

    [Fact]
    public void ProtectedSource_AlwaysBlocks()
    {
        using var fixture = OrganizePdfFixtureFactory.CreateProtected();
        using var session = PdfDocumentSession.Open(fixture.Path, "secret");

        var result = Inspect(session);

        Assert.False(ReadBool(result, "CanProceed"));
        AssertFinding(result, "PasswordProtectedSource", "Block", expectedPreservation: null);
    }

    [Fact]
    public void BookmarksAndInternalLinks_AreWarningsWithUnknownPreservation()
    {
        using var fixture = OrganizePdfFixtureFactory.CreateNavigation();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var result = Inspect(session);

        Assert.True(ReadBool(result, "CanProceed"));
        Assert.True(ReadBool(result, "RequiresWarningConfirmation"));
        AssertFinding(result, "Bookmark", "Warning", "Unknown");
        AssertFinding(result, "InternalLink", "Warning", "Unknown");
    }

    [Fact]
    public void Form_IsWarningUnknown_WhenDetected()
    {
        using var fixture = OrganizePdfFixtureFactory.CreateAcroForm();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var result = Inspect(session);

        Assert.True(ReadBool(result, "CanProceed"));
        Assert.True(ReadBool(result, "RequiresWarningConfirmation"));
        AssertFinding(result, "Form", "Warning", "Unknown");
    }

    [Fact]
    public void Metadata_IsWarningUnknown_WhenDetected()
    {
        using var fixture = OrganizePdfFixtureFactory.CreateMetadata();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var result = Inspect(session);

        Assert.True(ReadBool(result, "CanProceed"));
        Assert.True(ReadBool(result, "RequiresWarningConfirmation"));
        AssertFinding(result, "Metadata", "Warning", "Unknown");
    }

    [Fact]
    public void PreCanceled_ThrowsWithoutPartialResult()
    {
        using var fixture = OrganizePdfFixtureFactory.CreateNavigation();
        using var session = PdfDocumentSession.Open(fixture.Path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => Inspect(session, cancellationToken: cancellation.Token));
    }

    private static object Inspect(
        PdfDocumentSession session,
        Func<PdfDocumentSession, CancellationToken, int>? signatureReader = null,
        CancellationToken cancellationToken = default)
    {
        var inspectorType = RequiredType("SGPdf.App.Features.Organize.OrganizePreflightInspector");
        object inspector;
        if (signatureReader is null)
        {
            inspector = CreateInstance(inspectorType, Array.Empty<object>());
        }
        else
        {
            inspector = CreateInstance(inspectorType, new object[] { signatureReader });
        }

        var method = inspectorType.GetMethod("Inspect", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(method);
        try
        {
            var value = method.Invoke(inspector, new object[] { session, cancellationToken });
            Assert.NotNull(value);
            return value;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static object CreateInstance(Type type, object[] arguments)
    {
        var value = Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            args: arguments,
            culture: null);
        Assert.NotNull(value);
        return value;
    }

    private static Type RequiredType(string fullName)
    {
        return AppAssembly.GetType(fullName, throwOnError: true)!;
    }

    private static bool ReadBool(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(property);
        return Assert.IsType<bool>(property.GetValue(instance));
    }

    private static IReadOnlyList<object> ReadFindings(object result)
    {
        var property = result.GetType().GetProperty("Findings", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(property);
        var enumerable = Assert.IsAssignableFrom<System.Collections.IEnumerable>(property.GetValue(result));
        return enumerable.Cast<object>().ToArray();
    }

    private static void AssertFinding(
        object result,
        string expectedKind,
        string expectedSeverity,
        string? expectedPreservation)
    {
        var finding = Assert.Single(ReadFindings(result), item => ReadEnumName(item, "Kind") == expectedKind);
        Assert.Equal(expectedSeverity, ReadEnumName(finding, "Severity"));
        if (expectedPreservation is not null)
            Assert.Equal(expectedPreservation, ReadEnumName(finding, "PreservationStatus"));
    }

    private static string ReadEnumName(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(property);
        var value = property.GetValue(instance);
        Assert.NotNull(value);
        return value.ToString()!;
    }
}
