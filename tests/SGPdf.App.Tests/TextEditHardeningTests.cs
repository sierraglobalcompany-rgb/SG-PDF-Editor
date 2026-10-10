using System.Reflection;
using PdfSharp.Pdf;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class TextEditHardeningTests
{
    [Fact]
    public void EnterEditMode_MultiPageDocument_QueriesImageAndTextDiscoveryOnlyForActivePageOnce()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f7-active-page-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "two-pages.pdf");
            using (var document = new PdfDocument())
            {
                document.AddPage();
                document.AddPage();
                document.Save(path);
            }

            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, path));
                OrganizeWindowTestHost.SetField(
                    window,
                    "_getCryptographicSignatureCount",
                    (Func<PdfDocumentSession, int>)(_ => 0));

                var imagePages = new List<int>();
                var textPages = new List<int>();
                OrganizeWindowTestHost.SetField(
                    window,
                    "_getImageObjects",
                    (Func<PdfDocumentSession, int, CancellationToken, IReadOnlyList<PdfImageObjectInfo>>)
                    ((_, pageIndex, _) =>
                    {
                        imagePages.Add(pageIndex);
                        return Array.Empty<PdfImageObjectInfo>();
                    }));
                OrganizeWindowTestHost.SetField(
                    window,
                    "_getTextObjects",
                    (Func<PdfDocumentSession, int, CancellationToken, IReadOnlyList<PdfTextObjectInfo>>)
                    ((_, pageIndex, _) =>
                    {
                        textPages.Add(pageIndex);
                        return Array.Empty<PdfTextObjectInfo>();
                    }));

                Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterEditModeAsync"));

                Assert.Equal(new[] { 0 }, imagePages);
                Assert.Equal(new[] { 0 }, textPages);
            }
            finally
            {
                ImageEditCommandTestHost.CloseClean(window);
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }
        });
    }

    [Fact]
    public void PersistentTextArtifacts_ContainNoNativeHandles()
    {
        var persistentTypes = new[]
        {
            typeof(PdfTextObjectInfo),
            typeof(TextObjectKey),
            typeof(PdfTextObjectQuad),
            typeof(PdfTextFillColor),
            typeof(TextEditState),
            typeof(TextEditCandidate),
            typeof(TextEditWorkspace),
            typeof(CidType2FontMapPlan),
            typeof(FallbackFontAsset)
        };

        foreach (var type in persistentTypes)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.DoesNotContain(fields, field => IsNativeHandle(field.FieldType));

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.DoesNotContain(properties, property => IsNativeHandle(property.PropertyType));
        }
    }

    [Fact]
    public void FallbackFontAsset_OversizedLockedFile_IsRejectedBeforeTypefaceOpen()
    {
        var method = typeof(FallbackFontAsset).GetMethod(
            "CreateValidatedTypeface",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f7-font-hardening-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "oversized.ttf");
        try
        {
            using var locked = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
            locked.SetLength(FallbackFontAsset.MaxFontBytes + 1L);
            locked.Flush(flushToDisk: true);

            var invocation = Assert.Throws<TargetInvocationException>(() =>
                method!.Invoke(null, new object[] { path }));
            Assert.IsType<NotSupportedException>(invocation.InnerException);
        }
        finally
        {
            try { File.Delete(path); } catch { }
            try { Directory.Delete(directory); } catch { }
        }
    }

    [Fact]
    public void CombinedSave_CancelledAfterValidation_PreservesDestinationDirtyBaselineAndCleansTemp()
    {
        using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
        var imageWorkspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var textWorkspace = CreateDirtyTextWorkspace(fixture.Path);
        var destination = fixture.NewOutputPath("cancel-after-validation.pdf");
        var sentinel = new byte[] { 7, 1, 7, 1 };
        File.WriteAllBytes(destination, sentinel);
        using var cancellation = new CancellationTokenSource();

        var writer = new PdfEditWriter(
            validateOutputOverride: (_, _, _) => cancellation.Cancel(),
            signatureCountOverride: _ => 0);

        Assert.Throws<OperationCanceledException>(() =>
            writer.SaveAsCopy(
                imageWorkspace,
                textWorkspace,
                destination,
                warningsConfirmed: false,
                cancellation.Token));

        Assert.Equal(sentinel, File.ReadAllBytes(destination));
        Assert.True(textWorkspace.IsDirty);
        AssertNoTemps(fixture.DirectoryPath);
    }

    [Fact]
    public void CombinedSave_StaleSource_PreservesBothDirtyBaselinesAndDoesNotPublish()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateImageThenText();
        var (imageWorkspace, textWorkspace) = CreateDirtyCombinedWorkspaces(fixture.Path);
        var destination = Path.Combine(fixture.DirectoryPath, "stale-must-not-publish.pdf");

        File.AppendAllText(fixture.Path, "stale-source");
        var writer = new PdfEditWriter(signatureCountOverride: _ => 0);

        Assert.Throws<IOException>(() =>
            writer.SaveAsCopy(imageWorkspace, textWorkspace, destination, warningsConfirmed: false));

        Assert.True(imageWorkspace.IsDirty);
        Assert.True(textWorkspace.IsDirty);
        Assert.False(File.Exists(destination));
        AssertNoTemps(fixture.DirectoryPath);
    }

    [Fact]
    public void CombinedSave_PublicationFailure_PreservesBothDirtyBaselinesAndCleansTemp()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateImageThenText();
        var (imageWorkspace, textWorkspace) = CreateDirtyCombinedWorkspaces(fixture.Path);
        var destination = Path.Combine(fixture.DirectoryPath, "directory-collision.pdf");
        Directory.CreateDirectory(destination);
        var writer = new PdfEditWriter(signatureCountOverride: _ => 0);

        Assert.ThrowsAny<IOException>(() =>
            writer.SaveAsCopy(imageWorkspace, textWorkspace, destination, warningsConfirmed: false));

        Assert.True(imageWorkspace.IsDirty);
        Assert.True(textWorkspace.IsDirty);
        Assert.True(Directory.Exists(destination));
        AssertNoTemps(fixture.DirectoryPath);
    }

    private static TextEditWorkspace CreateDirtyTextWorkspace(string path)
    {
        using var source = PdfDocumentSession.Open(path);
        var text = Assert.Single(source.GetTextObjects(0));
        var workspace = TextEditWorkspace.Create(path, sourceOpenedWithPassword: false);
        workspace.EnsureObject(text);
        var candidate = workspace.PrepareCandidate(
            text.Key,
            "CASA 321",
            text.FontSize,
            text.FillColor);
        Assert.True(candidate.IsValid);
        workspace.CommitCandidate(candidate.Candidate!);
        Assert.True(workspace.IsDirty);
        return workspace;
    }

    private static (ImageEditWorkspace Images, TextEditWorkspace Text) CreateDirtyCombinedWorkspaces(string path)
    {
        using var source = PdfDocumentSession.Open(path);
        var image = Assert.Single(source.GetImageObjects(0));
        var text = Assert.Single(source.GetTextObjects(0));

        var images = ImageEditWorkspace.Create(path, sourceOpenedWithPassword: false);
        var imageState = images.EnsureObject(image);
        var matrix = imageState.CurrentMatrix;
        images.Commit(
            ImageEditOperationKind.Move,
            imageState with
            {
                CurrentMatrix = new PdfObjectMatrix(
                    matrix.A,
                    matrix.B,
                    matrix.C,
                    matrix.D,
                    matrix.E + 1d,
                    matrix.F)
            });
        Assert.True(images.IsDirty);

        var texts = TextEditWorkspace.Create(path, sourceOpenedWithPassword: false);
        texts.EnsureObject(text);
        var candidate = texts.PrepareCandidate(
            text.Key,
            "CASA 321",
            text.FontSize,
            text.FillColor);
        Assert.True(candidate.IsValid);
        texts.CommitCandidate(candidate.Candidate!);
        Assert.True(texts.IsDirty);

        return (images, texts);
    }

    private static void AssertNoTemps(string directory)
        => Assert.Empty(Directory.GetFiles(directory, ".*.sgpdf.tmp", SearchOption.TopDirectoryOnly));

    private static bool IsNativeHandle(Type type)
        => type == typeof(IntPtr) || type == typeof(UIntPtr);
}
