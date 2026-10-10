using System.Reflection;
using System.Runtime.ExceptionServices;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfEditWriterTextColorTests
{
    [Fact]
    public void SaveAs_OriginalFontColorEdit_AppliesFillAndPreservesSizeMatrixFont()
    {
        using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
        var before = TextEditNativeCharacterizationHarness.InspectFirstText(fixture.Path);
        using var source = PdfDocumentSession.Open(fixture.Path);
        var text = Assert.Single(source.GetTextObjects(0));
        var imageWorkspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var textWorkspace = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        textWorkspace.EnsureObject(text);
        var expectedColor = new PdfTextFillColor(12u, 34u, 56u, 200u);
        var candidate = textWorkspace.PrepareCandidate(
            text.Key,
            "CASA 321",
            text.FontSize,
            expectedColor);
        Assert.True(candidate.IsValid);
        Assert.Equal(TextFontStrategy.OriginalFont, candidate.Candidate!.FontStrategy);
        textWorkspace.CommitCandidate(candidate.Candidate);

        var destination = fixture.NewOutputPath("original-font-color.pdf");
        SaveCombined(CreateWriter(), imageWorkspace, textWorkspace, destination);

        var after = TextEditNativeCharacterizationHarness.InspectFirstText(destination);
        Assert.Equal("CASA 321", after.Text);
        Assert.Equal(expectedColor.Red, after.Red);
        Assert.Equal(expectedColor.Green, after.Green);
        Assert.Equal(expectedColor.Blue, after.Blue);
        Assert.Equal(expectedColor.Alpha, after.Alpha);
        Assert.Equal(before.FontSize, after.FontSize, precision: 3);
        Assert.Equal(before.FontName, after.FontName);
        Assert.Equal(before.Matrix.A, after.Matrix.A, precision: 2);
        Assert.Equal(before.Matrix.B, after.Matrix.B, precision: 2);
        Assert.Equal(before.Matrix.C, after.Matrix.C, precision: 2);
        Assert.Equal(before.Matrix.D, after.Matrix.D, precision: 2);
        Assert.Equal(before.Matrix.E, after.Matrix.E, precision: 2);
        Assert.Equal(before.Matrix.F, after.Matrix.F, precision: 2);
    }

    private static object CreateWriter()
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType("SGPdf.App.Pdf.PdfEditWriter", throwOnError: true)!;
        var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(info => info.GetParameters().Length == 4);
        return constructor.Invoke(new object?[] { null, null, null, null });
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
}
