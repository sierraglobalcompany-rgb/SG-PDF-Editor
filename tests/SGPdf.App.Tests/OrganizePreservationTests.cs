using System.Reflection;
using SGPdf.App.Features.Organize;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OrganizePreservationTests
{
    [Theory]
    [InlineData("NamedDestination", "NamedDestination")]
    [InlineData("TaggedStructure", "TaggedStructure")]
    [InlineData("PageLabel", "PageLabel")]
    [InlineData("Attachment", "Attachment")]
    public void ExtendedDocumentStructures_AreWarningsWithProvenLossEvidence(
        string fixtureKind,
        string expectedKindName)
    {
        using var fixture = CreateFixture(fixtureKind);
        using var session = PdfDocumentSession.Open(fixture.Path);
        var expectedKind = Enum.Parse<OrganizeFindingKind>(expectedKindName);

        var result = new OrganizePreflightInspector().Inspect(session);

        var finding = Assert.Single(result.Findings, item => item.Kind == expectedKind);
        Assert.Equal(OrganizeFindingSeverity.Warning, finding.Severity);
        Assert.Equal(OrganizePreservationStatus.ProvenChangedOrLost, finding.PreservationStatus);
        Assert.True(result.CanProceed);
        Assert.True(result.RequiresWarningConfirmation);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("ProvenChangedOrLost")]
    public void Preflight_UnknownOrChangedNonCryptoStructure_RequiresExplicitWarning(
        string preservationStatusName)
    {
        var preservationStatus = Enum.Parse<OrganizePreservationStatus>(preservationStatusName);
        var result = new OrganizePreflightResult(new[]
        {
            new OrganizeFinding(
                OrganizeFindingKind.Metadata,
                OrganizeFindingSeverity.Warning,
                "representative non-crypto structure",
                preservationStatus)
        });

        Assert.True(result.CanProceed);
        Assert.True(result.RequiresWarningConfirmation);
    }

    [Fact]
    public void ProvenPreserved_InfoFinding_DoesNotRequireWarningByItself()
    {
        var result = new OrganizePreflightResult(new[]
        {
            new OrganizeFinding(
                OrganizeFindingKind.Metadata,
                OrganizeFindingSeverity.Info,
                "preserved by representative evidence",
                OrganizePreservationStatus.ProvenPreserved)
        });

        Assert.True(result.CanProceed);
        Assert.False(result.RequiresWarningConfirmation);
    }

    [Theory]
    [InlineData("Navigation", "Bookmark")]
    [InlineData("Navigation", "InternalLink")]
    [InlineData("Form", "Form")]
    [InlineData("NamedDestination", "NamedDestination")]
    [InlineData("TaggedStructure", "TaggedStructure")]
    [InlineData("PageLabel", "PageLabel")]
    [InlineData("Attachment", "Attachment")]
    public void RealWriter_IdentityImport_DropsDetectedNonMetadataStructure(
        string fixtureKind,
        string findingKindName)
    {
        using var fixture = CreateFixture(fixtureKind);
        var findingKind = Enum.Parse<OrganizeFindingKind>(findingKindName);
        var destination = WriteIdentityCopy(fixture);

        using var sourceSession = PdfDocumentSession.Open(fixture.Path);
        using var outputSession = PdfDocumentSession.Open(destination);
        var inspector = new OrganizePreflightInspector();

        Assert.Contains(inspector.Inspect(sourceSession).Findings, finding => finding.Kind == findingKind);
        Assert.DoesNotContain(inspector.Inspect(outputSession).Findings, finding => finding.Kind == findingKind);
    }

    [Fact]
    public void Metadata_IdentityImport_DoesNotPreserveSourceTitleOrAuthor()
    {
        using var fixture = OrganizePdfFixtureFactory.CreateMetadata();
        var method = typeof(PdfDocumentSession).GetMethod(
            "GetOrganizeMetadataText",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var destination = WriteIdentityCopy(fixture);
        using var sourceSession = PdfDocumentSession.Open(fixture.Path);
        using var outputSession = PdfDocumentSession.Open(destination);

        var sourceTitle = Assert.IsType<string>(method.Invoke(sourceSession, new object[] { "Title", CancellationToken.None }));
        var sourceAuthor = Assert.IsType<string>(method.Invoke(sourceSession, new object[] { "Author", CancellationToken.None }));
        var outputTitle = Assert.IsType<string>(method.Invoke(outputSession, new object[] { "Title", CancellationToken.None }));
        var outputAuthor = Assert.IsType<string>(method.Invoke(outputSession, new object[] { "Author", CancellationToken.None }));

        Assert.Equal("SG PDF metadata fixture", sourceTitle);
        Assert.Equal("SG PDF tests", sourceAuthor);
        Assert.NotEqual(sourceTitle, outputTitle);
        Assert.NotEqual(sourceAuthor, outputAuthor);
    }

    private static string WriteIdentityCopy(OrganizePdfFixture fixture)
    {
        using var sourceSession = PdfDocumentSession.Open(fixture.Path);
        var source = OrganizeSource.Capture(fixture.Path, sourceSession.PageCount);
        var plan = OrganizePlan.FromPrimarySource(source);
        var destination = Path.Combine(fixture.DirectoryPath, $"identity-{Guid.NewGuid():N}.pdf");
        new PdfOrganizeWriter().SaveAsCopy(
            plan,
            fixture.Path,
            destination,
            warningsConfirmed: true);
        return destination;
    }

    private static OrganizePdfFixture CreateFixture(string fixtureKind)
        => fixtureKind switch
        {
            "Metadata" => OrganizePdfFixtureFactory.CreateMetadata(),
            "Navigation" => OrganizePdfFixtureFactory.CreateNavigation(),
            "Form" => OrganizePdfFixtureFactory.CreateAcroForm(),
            "NamedDestination" => OrganizePdfFixtureFactory.CreateNamedDestination(),
            "TaggedStructure" => OrganizePdfFixtureFactory.CreateTagged(),
            "PageLabel" => OrganizePdfFixtureFactory.CreatePageLabels(),
            "Attachment" => OrganizePdfFixtureFactory.CreateAttachment(),
            _ => throw new ArgumentOutOfRangeException(nameof(fixtureKind), fixtureKind, null)
        };
}
