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
    public void ExtendedDocumentStructures_AreDetectedConservatively(
        string fixtureKind,
        string expectedKindName)
    {
        using var fixture = CreateFixture(fixtureKind);
        using var session = PdfDocumentSession.Open(fixture.Path);
        var expectedKind = Enum.Parse<OrganizeFindingKind>(expectedKindName);

        var result = new OrganizePreflightInspector().Inspect(session);

        var finding = Assert.Single(result.Findings, item => item.Kind == expectedKind);
        Assert.Equal(OrganizeFindingSeverity.Warning, finding.Severity);
        Assert.Equal(OrganizePreservationStatus.Unknown, finding.PreservationStatus);
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

    [Fact]
    public void Characterize_RealWriterOutput_ReportsObservedStructures()
    {
        var observations = new List<string>();
        foreach (var fixtureKind in new[]
                 {
                     "Metadata", "Navigation", "Form", "NamedDestination",
                     "TaggedStructure", "PageLabel", "Attachment"
                 })
        {
            using var fixture = CreateFixture(fixtureKind);
            using var sourceSession = PdfDocumentSession.Open(fixture.Path);
            var pageCount = sourceSession.PageCount;
            var sourceResult = new OrganizePreflightInspector().Inspect(sourceSession);
            var source = OrganizeSource.Capture(fixture.Path, pageCount);
            var plan = OrganizePlan.FromPrimarySource(source);
            var destination = Path.Combine(fixture.DirectoryPath, $"{fixtureKind}-output.pdf");

            new PdfOrganizeWriter().SaveAsCopy(
                plan,
                fixture.Path,
                destination,
                warningsConfirmed: true);

            using var outputSession = PdfDocumentSession.Open(destination);
            var outputResult = new OrganizePreflightInspector().Inspect(outputSession);
            observations.Add(
                $"{fixtureKind}: source=[{Kinds(sourceResult)}] output=[{Kinds(outputResult)}]");
        }

        Assert.True(false, string.Join(Environment.NewLine, observations));
    }

    private static string Kinds(OrganizePreflightResult result)
        => string.Join(",", result.Findings.Select(finding => finding.Kind.ToString()));

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
