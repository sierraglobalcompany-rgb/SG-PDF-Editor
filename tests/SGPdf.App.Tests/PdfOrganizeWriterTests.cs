using System.IO;
using System.Text;
using SGPdf.App.Features.Organize;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfOrganizeWriterTests
{
    [Fact]
    public void SaveAsCopy_MaterializesReorderDuplicateDeleteAndAbsoluteRotations()
    {
        using var fixture = Task4PdfFixture.CreateMarkerSource();
        var source = OrganizeSource.Capture(fixture.Path, 3);
        var initial = OrganizePlan.FromPrimarySource(source);
        var pages = new[]
        {
            new OrganizePage(initial.Pages[2].ItemId, source.SourceId, 2, 1),
            new OrganizePage(initial.Pages[0].ItemId, source.SourceId, 0, 0),
            new OrganizePage(Guid.NewGuid(), source.SourceId, 0, 1)
        };
        var plan = new OrganizePlan(new[] { source }, pages);
        var destination = fixture.PathFor("materialized.pdf");

        new PdfOrganizeWriter().SaveAsCopy(
            plan,
            fixture.Path,
            destination,
            warningsConfirmed: false);

        using var output = PdfDocumentSession.Open(destination);
        Assert.Equal(3, output.PageCount);
        Assert.NotEmpty(output.FindTextOnPage(0, "THREE"));
        Assert.NotEmpty(output.FindTextOnPage(1, "ONE"));
        Assert.NotEmpty(output.FindTextOnPage(2, "ONE"));
        Assert.Empty(output.FindTextOnPage(0, "TWO"));
        Assert.Equal(new[] { 3, 0, 1 }, Enumerable.Range(0, 3).Select(index => output.GetPageRotation(index)).ToArray());
    }

    [Fact]
    public void SaveAsCopy_MultipleSources_PreservesExactLogicalOrder()
    {
        using var firstFixture = Task4PdfFixture.CreateMarkerSource("A-");
        using var secondFixture = Task4PdfFixture.CreateMarkerSource("B-");
        var first = OrganizeSource.Capture(firstFixture.Path, 3);
        var second = OrganizeSource.Capture(secondFixture.Path, 3);
        var plan = new OrganizePlan(
            new[] { first, second },
            new[]
            {
                new OrganizePage(Guid.NewGuid(), second.SourceId, 1, 0),
                new OrganizePage(Guid.NewGuid(), first.SourceId, 0, 0),
                new OrganizePage(Guid.NewGuid(), second.SourceId, 2, 0),
                new OrganizePage(Guid.NewGuid(), first.SourceId, 2, 0)
            });
        var destination = firstFixture.PathFor("multi-source.pdf");

        new PdfOrganizeWriter().SaveAsCopy(
            plan,
            firstFixture.Path,
            destination,
            warningsConfirmed: false);

        using var output = PdfDocumentSession.Open(destination);
        Assert.Equal(4, output.PageCount);
        Assert.NotEmpty(output.FindTextOnPage(0, "B-TWO"));
        Assert.NotEmpty(output.FindTextOnPage(1, "A-ONE"));
        Assert.NotEmpty(output.FindTextOnPage(2, "B-THREE"));
        Assert.NotEmpty(output.FindTextOnPage(3, "A-THREE"));
    }

    [Fact]
    public void SaveAsCopy_StaleSecondarySource_PreservesExistingDestination()
    {
        using var firstFixture = Task4PdfFixture.CreateMarkerSource("A-");
        using var secondFixture = Task4PdfFixture.CreateMarkerSource("B-");
        var first = OrganizeSource.Capture(firstFixture.Path, 3);
        var second = OrganizeSource.Capture(secondFixture.Path, 3);
        var plan = new OrganizePlan(
            new[] { first, second },
            new[]
            {
                new OrganizePage(Guid.NewGuid(), first.SourceId, 0, 0),
                new OrganizePage(Guid.NewGuid(), second.SourceId, 0, 0)
            });
        var destination = firstFixture.PathFor("existing-secondary-stale.pdf");
        File.WriteAllText(destination, "KEEP");
        File.Delete(secondFixture.Path);

        Assert.ThrowsAny<Exception>((Action)(() => new PdfOrganizeWriter().SaveAsCopy(
            plan,
            firstFixture.Path,
            destination,
            warningsConfirmed: false)));

        Assert.Equal("KEEP", File.ReadAllText(destination));
        AssertNoTempResidue(destination);
    }

    [Fact]
    public void SaveAsCopy_DestinationEqualsActiveSource_IsRejectedBeforeWrite()
    {
        using var fixture = Task4PdfFixture.CreateMarkerSource();
        var source = OrganizeSource.Capture(fixture.Path, 3);
        var plan = OrganizePlan.FromPrimarySource(source);
        var original = File.ReadAllBytes(fixture.Path);

        Assert.Throws<ArgumentException>((Action)(() => new PdfOrganizeWriter().SaveAsCopy(
            plan,
            fixture.Path,
            fixture.Path,
            warningsConfirmed: false)));

        Assert.Equal(original, File.ReadAllBytes(fixture.Path));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SaveAsCopy_SourceChangedOrMissingBeforeMaterialization_PreservesDestination(bool deleteSource)
    {
        using var fixture = Task4PdfFixture.CreateMarkerSource();
        var source = OrganizeSource.Capture(fixture.Path, 3);
        var plan = OrganizePlan.FromPrimarySource(source);
        var destination = fixture.PathFor("existing-stale.pdf");
        File.WriteAllText(destination, "KEEP");

        if (deleteSource)
            File.Delete(fixture.Path);
        else
            File.AppendAllText(fixture.Path, "\n% changed after capture\n");

        Assert.ThrowsAny<Exception>((Action)(() => new PdfOrganizeWriter().SaveAsCopy(
            plan,
            fixture.Path,
            destination,
            warningsConfirmed: false)));

        Assert.Equal("KEEP", File.ReadAllText(destination));
        AssertNoTempResidue(destination);
    }

    [Fact]
    public void SaveAsCopy_ForcedNativeSaveFailure_PreservesDestinationAndCleansTemp()
    {
        using var fixture = Task4PdfFixture.CreateMarkerSource();
        var source = OrganizeSource.Capture(fixture.Path, 3);
        var plan = OrganizePlan.FromPrimarySource(source);
        var destination = fixture.PathFor("existing-native.pdf");
        File.WriteAllText(destination, "KEEP");
        var writer = new PdfOrganizeWriter(
            saveAsCopyOverride: static (_, _, _) => 0,
            validateOutputOverride: null,
            preflightOverride: null);

        Assert.Throws<InvalidOperationException>((Action)(() => writer.SaveAsCopy(
            plan,
            fixture.Path,
            destination,
            warningsConfirmed: false)));

        Assert.Equal("KEEP", File.ReadAllText(destination));
        AssertNoTempResidue(destination);
    }

    [Fact]
    public void SaveAsCopy_ForcedValidationFailure_PreservesDestinationAndCleansTemp()
    {
        using var fixture = Task4PdfFixture.CreateMarkerSource();
        var source = OrganizeSource.Capture(fixture.Path, 3);
        var plan = OrganizePlan.FromPrimarySource(source);
        var destination = fixture.PathFor("existing-validation.pdf");
        File.WriteAllText(destination, "KEEP");
        var writer = new PdfOrganizeWriter(
            saveAsCopyOverride: null,
            validateOutputOverride: static (_, _, _) => throw new InvalidDataException("forced validation failure"),
            preflightOverride: null);

        Assert.Throws<InvalidDataException>((Action)(() => writer.SaveAsCopy(
            plan,
            fixture.Path,
            destination,
            warningsConfirmed: false)));

        Assert.Equal("KEEP", File.ReadAllText(destination));
        AssertNoTempResidue(destination);
    }

    [Fact]
    public void SaveAsCopy_PreflightBlock_DoesNotCreateTempOrReplaceDestination()
    {
        using var fixture = Task4PdfFixture.CreateMarkerSource();
        var source = OrganizeSource.Capture(fixture.Path, 3);
        var plan = OrganizePlan.FromPrimarySource(source);
        var destination = fixture.PathFor("existing-block.pdf");
        File.WriteAllText(destination, "KEEP");
        var block = new OrganizePreflightResult(new[]
        {
            new OrganizeFinding(
                OrganizeFindingKind.CryptographicSignature,
                OrganizeFindingSeverity.Block,
                "signed",
                OrganizePreservationStatus.Unknown)
        });
        var writer = new PdfOrganizeWriter(
            saveAsCopyOverride: null,
            validateOutputOverride: null,
            preflightOverride: (_, _) => block);

        Assert.Throws<InvalidOperationException>((Action)(() => writer.SaveAsCopy(
            plan,
            fixture.Path,
            destination,
            warningsConfirmed: true)));

        Assert.Equal("KEEP", File.ReadAllText(destination));
        AssertNoTempResidue(destination);
    }

    [Fact]
    public void SaveAsCopy_PasswordProtectedSource_BlocksWithoutTemp()
    {
        using var fixture = OrganizePdfFixtureFactory.CreateProtected();
        var source = OrganizeSource.Capture(fixture.Path, 1);
        var plan = OrganizePlan.FromPrimarySource(source);
        var destination = Path.Combine(fixture.DirectoryPath, "existing-protected.pdf");
        File.WriteAllText(destination, "KEEP");

        Assert.Throws<InvalidOperationException>((Action)(() => new PdfOrganizeWriter().SaveAsCopy(
            plan,
            fixture.Path,
            destination,
            warningsConfirmed: true)));

        Assert.Equal("KEEP", File.ReadAllText(destination));
        AssertNoTempResidue(destination);
    }

    [Fact]
    public void SaveAsCopy_WarningRequiresExplicitConfirmation()
    {
        using var fixture = OrganizePdfFixtureFactory.CreateMetadata();
        var source = OrganizeSource.Capture(fixture.Path, 1);
        var plan = OrganizePlan.FromPrimarySource(source);
        var denied = Path.Combine(fixture.DirectoryPath, "warning-denied.pdf");
        var allowed = Path.Combine(fixture.DirectoryPath, "warning-allowed.pdf");
        var writer = new PdfOrganizeWriter();

        Assert.Throws<InvalidOperationException>((Action)(() => writer.SaveAsCopy(
            plan,
            fixture.Path,
            denied,
            warningsConfirmed: false)));
        Assert.False(File.Exists(denied));
        AssertNoTempResidue(denied);

        writer.SaveAsCopy(
            plan,
            fixture.Path,
            allowed,
            warningsConfirmed: true);
        Assert.True(File.Exists(allowed));
    }

    private static void AssertNoTempResidue(string destination)
    {
        var directory = Path.GetDirectoryName(destination)!;
        var fileName = Path.GetFileName(destination);
        Assert.Empty(Directory.GetFiles(directory, $".{fileName}.*.sgpdf.tmp"));
    }
}

internal sealed class Task4PdfFixture : IDisposable
{
    private Task4PdfFixture(string directoryPath, string path)
    {
        DirectoryPath = directoryPath;
        Path = path;
    }

    internal string DirectoryPath { get; }
    internal string Path { get; }
    internal string PathFor(string fileName) => System.IO.Path.Combine(DirectoryPath, fileName);

    internal static Task4PdfFixture CreateMarkerSource(string markerPrefix = "")
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-organize-writer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = System.IO.Path.Combine(directory, "source.pdf");
        File.WriteAllBytes(path, BuildMarkerPdf(markerPrefix));
        return new Task4PdfFixture(directory, path);
    }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }

    private static byte[] BuildMarkerPdf(string markerPrefix)
    {
        static string StreamObject(string marker)
        {
            var content = $"BT /F1 24 Tf 30 100 Td ({marker}) Tj ET\n";
            var length = Encoding.Latin1.GetByteCount(content);
            return $"<< /Length {length} >>\nstream\n{content}endstream";
        }

        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 5 0 R 7 0 R] /Count 3 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 300] /Rotate 0 /Resources << /Font << /F1 9 0 R >> >> /Contents 4 0 R >>",
            StreamObject($"{markerPrefix}ONE"),
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] /Rotate 90 /Resources << /Font << /F1 9 0 R >> >> /Contents 6 0 R >>",
            StreamObject($"{markerPrefix}TWO"),
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 400 250] /Rotate 180 /Resources << /Font << /F1 9 0 R >> >> /Contents 8 0 R >>",
            StreamObject($"{markerPrefix}THREE"),
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        using var stream = new MemoryStream();
        WriteLatin1(stream, "%PDF-1.7\n");
        var offsets = new long[objects.Length + 1];
        for (var index = 0; index < objects.Length; index++)
        {
            offsets[index + 1] = stream.Position;
            WriteLatin1(stream, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xref = stream.Position;
        WriteLatin1(stream, $"xref\n0 {objects.Length + 1}\n");
        WriteLatin1(stream, "0000000000 65535 f \n");
        for (var index = 1; index < offsets.Length; index++)
            WriteLatin1(stream, $"{offsets[index]:D10} 00000 n \n");
        WriteLatin1(stream, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return stream.ToArray();
    }

    private static void WriteLatin1(Stream stream, string text)
    {
        var bytes = Encoding.Latin1.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }
}
