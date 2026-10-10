using System.Reflection;
using System.Runtime.ExceptionServices;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfEditWriterTextTests
{
    [Fact]
    public void SaveAs_OriginalFontText_ReopensExactAndPreservesNativeProperties()
    {
        using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
        var before = TextEditNativeCharacterizationHarness.InspectFirstText(fixture.Path);
        var imageWorkspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var textWorkspace = CreateDirtyOriginalFontWorkspace(fixture.Path, "CASA 321");
        var destination = fixture.NewOutputPath("original-font.pdf");

        SaveCombined(CreateWriter(), imageWorkspace, textWorkspace, destination);

        var after = TextEditNativeCharacterizationHarness.InspectFirstText(destination);
        Assert.Equal("CASA 321", after.Text);
        Assert.Equal(before.FontName, after.FontName);
        Assert.Equal(before.FontSize, after.FontSize, precision: 3);
        Assert.Equal(before.Red, after.Red);
        Assert.Equal(before.Green, after.Green);
        Assert.Equal(before.Blue, after.Blue);
        Assert.Equal(before.Alpha, after.Alpha);
        AssertMatrix(before.Matrix, after.Matrix);
        Assert.Equal(before.RenderMode, after.RenderMode);
    }

    [Fact]
    public void SaveAs_MixedDeleteImageAndEditText_ResolvesAllBeforeMutationAndGeneratesOnce()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateImageThenText();
        using var source = PdfDocumentSession.Open(fixture.Path);
        var image = Assert.Single(source.GetImageObjects(0));
        var text = Assert.Single(source.GetTextObjects(0));
        Assert.True(image.PageObjectIndex < text.Key.PageObjectIndex);

        var imageWorkspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var imageState = imageWorkspace.EnsureObject(image);
        imageWorkspace.Commit(ImageEditOperationKind.Delete, imageState with { Deleted = true });

        var textWorkspace = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        textWorkspace.EnsureObject(text);
        CommitOriginalFont(textWorkspace, text, "CASA 321");

        var generateCalls = 0;
        var writer = CreateWriter(generateContentOverride: page =>
        {
            generateCalls++;
            return PdfiumNative.FPDFPage_GenerateContent(page);
        });
        var destination = Path.Combine(fixture.DirectoryPath, "mixed.pdf");

        SaveCombined(writer, imageWorkspace, textWorkspace, destination);

        Assert.Equal(1, generateCalls);
        using var saved = PdfDocumentSession.Open(destination);
        Assert.Empty(saved.GetImageObjects(0));
        Assert.Equal("CASA 321", Assert.Single(saved.GetTextObjects(0)).Text);
    }

    [Fact]
    public void SaveAs_DifferentWorkspaceFingerprints_FailsBeforePreflightOrMutation()
    {
        using var imageFixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        using var textFixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
        var imageWorkspace = ImageEditWorkspace.Create(imageFixture.Path, sourceOpenedWithPassword: false);
        var textWorkspace = CreateDirtyOriginalFontWorkspace(textFixture.Path, "CASA 321");
        var destination = Path.Combine(imageFixture.DirectoryPath, "must-not-exist.pdf");
        var signatureCalls = 0;
        var writer = CreateWriter(signatureCountOverride: _ =>
        {
            signatureCalls++;
            return 0;
        });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            SaveCombined(writer, imageWorkspace, textWorkspace, destination));

        Assert.Contains("mismo", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, signatureCalls);
        Assert.False(File.Exists(destination));
    }

    private static TextEditWorkspace CreateDirtyOriginalFontWorkspace(string sourcePath, string replacementText)
    {
        using var source = PdfDocumentSession.Open(sourcePath);
        var text = Assert.Single(source.GetTextObjects(0));
        var workspace = TextEditWorkspace.Create(sourcePath, sourceOpenedWithPassword: false);
        workspace.EnsureObject(text);
        CommitOriginalFont(workspace, text, replacementText);
        return workspace;
    }

    private static void CommitOriginalFont(
        TextEditWorkspace workspace,
        PdfTextObjectInfo source,
        string replacementText)
    {
        var candidate = workspace.PrepareCandidate(
            source.Key,
            replacementText,
            source.FontSize,
            source.FillColor);
        Assert.True(candidate.IsValid);
        Assert.NotNull(candidate.Candidate);
        Assert.Equal(TextFontStrategy.OriginalFont, candidate.Candidate!.FontStrategy);
        workspace.CommitCandidate(candidate.Candidate);
        Assert.True(workspace.IsDirty);
    }

    private static object CreateWriter(
        Func<IntPtr, int>? generateContentOverride = null,
        Func<string, int>? signatureCountOverride = null)
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType(
            "SGPdf.App.Pdf.PdfEditWriter",
            throwOnError: true)!;

        if (generateContentOverride is not null)
        {
            var extended = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SingleOrDefault(info => info.GetParameters().Length == 5);
            Assert.NotNull(extended);
            return extended!.Invoke(new object?[]
            {
                null,
                null,
                signatureCountOverride,
                null,
                generateContentOverride
            });
        }

        var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(info => info.GetParameters().Length == 4);
        return constructor.Invoke(new object?[]
        {
            null,
            null,
            signatureCountOverride,
            null
        });
    }

    private static void SaveCombined(
        object writer,
        ImageEditWorkspace imageWorkspace,
        TextEditWorkspace textWorkspace,
        string destinationPath)
    {
        var method = writer.GetType().GetMethod(
            "SaveAsCopy",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[]
            {
                typeof(ImageEditWorkspace),
                typeof(TextEditWorkspace),
                typeof(string),
                typeof(bool),
                typeof(CancellationToken)
            },
            modifiers: null);
        Assert.NotNull(method);

        try
        {
            method!.Invoke(writer, new object?[]
            {
                imageWorkspace,
                textWorkspace,
                destinationPath,
                false,
                CancellationToken.None
            });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static void AssertMatrix(PdfObjectMatrix expected, PdfObjectMatrix actual)
    {
        Assert.Equal(expected.A, actual.A, precision: 2);
        Assert.Equal(expected.B, actual.B, precision: 2);
        Assert.Equal(expected.C, actual.C, precision: 2);
        Assert.Equal(expected.D, actual.D, precision: 2);
        Assert.Equal(expected.E, actual.E, precision: 2);
        Assert.Equal(expected.F, actual.F, precision: 2);
    }
}
