using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfImageEditWriterHardeningTests
{
    [Fact]
    public void SaveAs_CancelledAfterValidation_PreservesDestinationAndCleansTemp()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        var destination = Path.Combine(fixture.DirectoryPath, "cancel-after-validation.pdf");
        var sentinel = new byte[] { 4, 2, 4, 2 };
        File.WriteAllBytes(destination, sentinel);
        using var cancellation = new CancellationTokenSource();

        var workspace = CreateWorkspace(fixture.Path);
        var writer = new PdfImageEditWriter(
            validateOutputOverride: (_, _, _) => cancellation.Cancel(),
            signatureCountOverride: _ => 0);

        Assert.Throws<OperationCanceledException>(() =>
            writer.SaveAsCopy(workspace, destination, warningsConfirmed: false, cancellation.Token));

        Assert.Equal(sentinel, File.ReadAllBytes(destination));
        AssertNoTemps(fixture.DirectoryPath);
    }

    [Fact]
    public void SaveAs_PublicationFailure_CleansTemporaryOutput()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        var destination = Path.Combine(fixture.DirectoryPath, "directory-collision.pdf");
        Directory.CreateDirectory(destination);

        var workspace = CreateWorkspace(fixture.Path);
        var writer = new PdfImageEditWriter(signatureCountOverride: _ => 0);

        Assert.ThrowsAny<IOException>(() =>
            writer.SaveAsCopy(workspace, destination, warningsConfirmed: false));

        Assert.True(Directory.Exists(destination));
        AssertNoTemps(fixture.DirectoryPath);
    }

    private static ImageEditWorkspace CreateWorkspace(string path)
    {
        using var session = PdfDocumentSession.Open(path);
        var workspace = ImageEditWorkspace.Create(path, sourceOpenedWithPassword: false);
        workspace.EnsureObject(Assert.Single(session.GetImageObjects(0)));
        return workspace;
    }

    private static void AssertNoTemps(string directory)
        => Assert.Empty(Directory.GetFiles(directory, ".*.sgpdf.tmp", SearchOption.TopDirectoryOnly));
}
