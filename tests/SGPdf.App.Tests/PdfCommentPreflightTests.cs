using System.Reflection;
using System.Runtime.ExceptionServices;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCommentPreflightTests
{
    private static readonly Assembly AppAssembly = typeof(PdfDocumentSession).Assembly;

    [Fact]
    public void CleanSource_CanProceedWithoutWarnings()
    {
        using var fixture = ImageEditPreservationFixture.CreatePlain();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var result = Inspect(session);

        Assert.True(ReadBool(result, "CanProceed"));
        Assert.False(ReadBool(result, "RequiresWarningConfirmation"));
        Assert.Empty(ReadFindings(result));
    }

    [Theory]
    [InlineData("Form")]
    [InlineData("Bookmark")]
    [InlineData("NamedDestination")]
    [InlineData("InternalLink")]
    [InlineData("TaggedStructure")]
    [InlineData("PageLabel")]
    [InlineData("Attachment")]
    public void RichUnverifiedStructures_AreWarningsUntilF8PreservationMatrixCloses(string findingKind)
    {
        using var fixture = ImageEditPreservationFixture.CreateRich();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var result = Inspect(session);
        var finding = Assert.Single(ReadFindings(result), item => ReadEnumName(item, "Kind") == findingKind);

        Assert.Equal("Warning", ReadEnumName(finding, "Severity"));
        Assert.Equal("Unknown", ReadEnumName(finding, "PreservationStatus"));
        Assert.True(ReadBool(result, "CanProceed"));
        Assert.True(ReadBool(result, "RequiresWarningConfirmation"));
        Assert.Contains("COMENTAR", ReadString(finding, "Message"), StringComparison.Ordinal);
    }

    [Fact]
    public void Metadata_IsInformationalButDoesNotBorrowF7PreservationProof()
    {
        using var fixture = ImageEditPreservationFixture.CreateRich();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var result = Inspect(session);
        var finding = Assert.Single(ReadFindings(result), item => ReadEnumName(item, "Kind") == "Metadata");

        Assert.Equal("Info", ReadEnumName(finding, "Severity"));
        Assert.Equal("Unknown", ReadEnumName(finding, "PreservationStatus"));
    }

    [Fact]
    public void Signature_IsBlockWithUnknownPreservation()
    {
        using var fixture = ImageEditPreservationFixture.CreatePlain();
        using var session = PdfDocumentSession.Open(fixture.Path);
        Func<PdfDocumentSession, CancellationToken, int> signatureReader = (_, _) => 1;

        var result = Inspect(session, signatureReader);
        var finding = Assert.Single(ReadFindings(result));

        Assert.Equal("CryptographicSignature", ReadEnumName(finding, "Kind"));
        Assert.Equal("Block", ReadEnumName(finding, "Severity"));
        Assert.Equal("Unknown", ReadEnumName(finding, "PreservationStatus"));
        Assert.False(ReadBool(result, "CanProceed"));
        Assert.Contains("COMENTAR", ReadString(finding, "Message"), StringComparison.Ordinal);
    }

    [Fact]
    public void PasswordOpenedSource_IsBlockWithUnknownPreservation()
    {
        using var fixture = OrganizePdfFixtureFactory.CreateProtected();
        using var session = PdfDocumentSession.Open(fixture.Path, "secret");

        var result = Inspect(session);
        var finding = Assert.Single(ReadFindings(result), item => ReadEnumName(item, "Kind") == "PasswordProtectedSource");

        Assert.Equal("Block", ReadEnumName(finding, "Severity"));
        Assert.Equal("Unknown", ReadEnumName(finding, "PreservationStatus"));
        Assert.False(ReadBool(result, "CanProceed"));
        Assert.Contains("COMENTAR", ReadString(finding, "Message"), StringComparison.Ordinal);
    }

    private static object Inspect(
        PdfDocumentSession session,
        Func<PdfDocumentSession, CancellationToken, int>? signatureReader = null)
    {
        var type = AppAssembly.GetType(
            "SGPdf.App.Features.Comments.PdfCommentPreflightInspector",
            throwOnError: false);
        Assert.NotNull(type);

        object? inspector = signatureReader is null
            ? Activator.CreateInstance(type, nonPublic: true)
            : Activator.CreateInstance(
                type,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { signatureReader },
                culture: null);
        Assert.NotNull(inspector);

        var method = type.GetMethod(
            "Inspect",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(PdfDocumentSession), typeof(CancellationToken) },
            modifiers: null);
        Assert.NotNull(method);

        try
        {
            var result = method.Invoke(inspector, new object[] { session, CancellationToken.None });
            Assert.NotNull(result);
            return result;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static bool ReadBool(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsType<bool>(property.GetValue(instance));
    }

    private static IReadOnlyList<object> ReadFindings(object result)
    {
        var property = result.GetType().GetProperty(
            "Findings",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        var enumerable = Assert.IsAssignableFrom<System.Collections.IEnumerable>(property.GetValue(result));
        return enumerable.Cast<object>().ToArray();
    }

    private static string ReadEnumName(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        var value = property.GetValue(instance);
        Assert.NotNull(value);
        return value.ToString()!;
    }

    private static string ReadString(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsType<string>(property.GetValue(instance));
    }
}
