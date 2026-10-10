using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ImageEditPreflightPolicyTests
{
    private static readonly Assembly AppAssembly = typeof(PdfDocumentSession).Assembly;

    [Theory]
    [InlineData("Form")]
    [InlineData("Bookmark")]
    [InlineData("NamedDestination")]
    [InlineData("InternalLink")]
    [InlineData("TaggedStructure")]
    [InlineData("PageLabel")]
    [InlineData("Attachment")]
    [InlineData("Metadata")]
    public void RichRepresentativeEvidence_IsInfoAndProvenPreserved(string findingKind)
    {
        using var fixture = ImageEditPreservationFixture.CreateRich();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var result = Inspect(session);
        var finding = Assert.Single(ReadFindings(result), item => ReadEnumName(item, "Kind") == findingKind);

        Assert.Equal("Info", ReadEnumName(finding, "Severity"));
        Assert.Equal("ProvenPreserved", ReadEnumName(finding, "PreservationStatus"));
        Assert.True(ReadBool(result, "CanProceed"));
        Assert.False(ReadBool(result, "RequiresWarningConfirmation"));
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
        Assert.False(ReadBool(result, "RequiresWarningConfirmation"));
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
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("ProvenChangedOrLost")]
    public void NonCryptoWarning_RequiresExplicitConfirmation(string preservationStatus)
    {
        var finding = CreateFinding("Metadata", "Warning", preservationStatus);
        var result = CreateResult(finding);

        Assert.True(ReadBool(result, "CanProceed"));
        Assert.True(ReadBool(result, "RequiresWarningConfirmation"));
    }

    private static object Inspect(
        PdfDocumentSession session,
        Func<PdfDocumentSession, CancellationToken, int>? signatureReader = null)
    {
        var type = RequiredType("SGPdf.App.Features.Edit.Images.ImageEditPreflightInspector");
        object inspector;
        if (signatureReader is null)
        {
            inspector = Create(type, Array.Empty<object>());
        }
        else
        {
            inspector = Create(type, new object[] { signatureReader });
        }

        var method = type.GetMethod("Inspect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var result = method.Invoke(inspector, new object[] { session, CancellationToken.None });
        Assert.NotNull(result);
        return result;
    }

    private static object CreateFinding(string kind, string severity, string preservationStatus)
    {
        var findingType = RequiredType("SGPdf.App.Features.Edit.Images.ImageEditFinding");
        var kindType = RequiredType("SGPdf.App.Features.Edit.Images.ImageEditFindingKind");
        var severityType = RequiredType("SGPdf.App.Features.Edit.Images.ImageEditFindingSeverity");
        var preservationType = RequiredType("SGPdf.App.Features.Edit.Images.ImageEditPreservationStatus");

        return Create(findingType, new[]
        {
            Enum.Parse(kindType, kind),
            Enum.Parse(severityType, severity),
            "synthetic policy finding",
            Enum.Parse(preservationType, preservationStatus)
        });
    }

    private static object CreateResult(object finding)
    {
        var resultType = RequiredType("SGPdf.App.Features.Edit.Images.ImageEditPreflightResult");
        var findingType = finding.GetType();
        var array = Array.CreateInstance(findingType, 1);
        array.SetValue(finding, 0);
        return Create(resultType, new object[] { array });
    }

    private static object Create(Type type, object[] args)
    {
        var instance = Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: args,
            culture: null);
        Assert.NotNull(instance);
        return instance;
    }

    private static Type RequiredType(string fullName)
    {
        var type = AppAssembly.GetType(fullName, throwOnError: false);
        Assert.NotNull(type);
        return type;
    }

    private static bool ReadBool(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsType<bool>(property.GetValue(instance));
    }

    private static IReadOnlyList<object> ReadFindings(object result)
    {
        var property = result.GetType().GetProperty("Findings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        var enumerable = Assert.IsAssignableFrom<System.Collections.IEnumerable>(property.GetValue(result));
        return enumerable.Cast<object>().ToArray();
    }

    private static string ReadEnumName(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        var value = property.GetValue(instance);
        Assert.NotNull(value);
        return value.ToString()!;
    }
}
