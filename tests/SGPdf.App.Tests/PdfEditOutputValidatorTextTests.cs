using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfEditOutputValidatorTextTests
{
    [Fact]
    public void Validate_CombinedOriginalFontOutput_PassesAndRendersEditedPage()
    {
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var sourcePath = Path.Combine(directory.Path, "source.pdf");
        var outputPath = Path.Combine(directory.Path, "output.pdf");
        WriteSimpleHelveticaPdf(sourcePath, "CASA 123", fontSize: 18, red: 0, green: 0, blue: 0, x: 72, y: 300);
        WriteSimpleHelveticaPdf(outputPath, "CASA 321", fontSize: 18, red: 0, green: 0, blue: 0, x: 72, y: 300);
        var imageWorkspace = ImageEditWorkspace.Create(sourcePath, sourceOpenedWithPassword: false);
        var textWorkspace = CreateDirtyWorkspace(sourcePath, "CASA 321", TextFontStrategy.OriginalFont);
        var rendered = new List<(int PageIndex, double Dpi)>();

        ValidateCombined(CreateValidator((pageIndex, dpi) => rendered.Add((pageIndex, dpi))), outputPath, imageWorkspace, textWorkspace);

        var render = Assert.Single(rendered);
        Assert.Equal(0, render.PageIndex);
        Assert.Equal(36d, render.Dpi);
    }

    [Theory]
    [InlineData("unicode")]
    [InlineData("type")]
    [InlineData("matrix")]
    [InlineData("size")]
    [InlineData("color")]
    public void Validate_CombinedTextMismatch_IsRejected(string mismatch)
    {
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var sourcePath = Path.Combine(directory.Path, "source.pdf");
        var outputPath = Path.Combine(directory.Path, "output.pdf");
        WriteSimpleHelveticaPdf(sourcePath, "CASA 123", fontSize: 18, red: 0, green: 0, blue: 0, x: 72, y: 300);

        if (mismatch == "type")
        {
            WriteVectorOnlyPdf(outputPath);
        }
        else
        {
            WriteSimpleHelveticaPdf(
                outputPath,
                mismatch == "unicode" ? "CASA 999" : "CASA 321",
                fontSize: mismatch == "size" ? 20 : 18,
                red: mismatch == "color" ? 255 : 0,
                green: 0,
                blue: 0,
                x: mismatch == "matrix" ? 90 : 72,
                y: 300);
        }

        var imageWorkspace = ImageEditWorkspace.Create(sourcePath, sourceOpenedWithPassword: false);
        var textWorkspace = CreateDirtyWorkspace(sourcePath, "CASA 321", TextFontStrategy.OriginalFont);

        Assert.Throws<InvalidDataException>(() =>
            ValidateCombined(CreateValidator(), outputPath, imageWorkspace, textWorkspace));
    }

    [Fact]
    public void Validate_CombinedFallbackWorkspace_HelveticaOutputIsRejected()
    {
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var sourcePath = Path.Combine(directory.Path, "source.pdf");
        var outputPath = Path.Combine(directory.Path, "output.pdf");
        WriteSimpleHelveticaPdf(sourcePath, "CASA 123", fontSize: 18, red: 0, green: 0, blue: 0, x: 72, y: 300);
        WriteSimpleHelveticaPdf(outputPath, "CASA 123X", fontSize: 18, red: 0, green: 0, blue: 0, x: 72, y: 300);
        var imageWorkspace = ImageEditWorkspace.Create(sourcePath, sourceOpenedWithPassword: false);
        var textWorkspace = CreateDirtyWorkspace(sourcePath, "CASA 123X", TextFontStrategy.FallbackTtf);

        Assert.Throws<InvalidDataException>(() =>
            ValidateCombined(CreateValidator(), outputPath, imageWorkspace, textWorkspace));
    }

    [Fact]
    public void Validate_CombinedFallbackWriterOutput_PassesExactUnicodeAndRendersEditedPage()
    {
        using var fixture = TextEditNativeCharacterizationHarness.CreateSimpleTextPdf();
        var imageWorkspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var textWorkspace = CreateDirtyWorkspace(fixture.Path, "NIÑO áé", TextFontStrategy.FallbackTtf);
        var outputPath = fixture.NewOutputPath("fallback-output.pdf");
        SaveCombined(CreateWriter(), imageWorkspace, textWorkspace, outputPath);
        var rendered = new List<(int PageIndex, double Dpi)>();

        ValidateCombined(CreateValidator((pageIndex, dpi) => rendered.Add((pageIndex, dpi))), outputPath, imageWorkspace, textWorkspace);

        var render = Assert.Single(rendered);
        Assert.Equal(0, render.PageIndex);
        Assert.Equal(36d, render.Dpi);
        using var saved = PdfDocumentSession.Open(outputPath);
        Assert.Equal("NIÑO áé", Assert.Single(saved.GetTextObjects(0)).Text);
    }

    private static TextEditWorkspace CreateDirtyWorkspace(
        string sourcePath,
        string replacementText,
        TextFontStrategy expectedStrategy)
    {
        using var source = PdfDocumentSession.Open(sourcePath);
        var text = Assert.Single(source.GetTextObjects(0));
        var workspace = TextEditWorkspace.Create(sourcePath, sourceOpenedWithPassword: false);
        workspace.EnsureObject(text);
        var candidate = workspace.PrepareCandidate(text.Key, replacementText, text.FontSize, text.FillColor);
        Assert.True(candidate.IsValid);
        Assert.NotNull(candidate.Candidate);
        Assert.Equal(expectedStrategy, candidate.Candidate!.FontStrategy);
        workspace.CommitCandidate(candidate.Candidate);
        Assert.True(workspace.IsDirty);
        return workspace;
    }

    private static object CreateValidator(Action<int, double>? renderObserver = null)
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType(
            "SGPdf.App.Pdf.PdfEditOutputValidator",
            throwOnError: true)!;
        var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(info => info.GetParameters().Length == 1);
        return constructor.Invoke(new object?[] { renderObserver });
    }

    private static void ValidateCombined(
        object validator,
        string outputPath,
        ImageEditWorkspace imageWorkspace,
        TextEditWorkspace textWorkspace,
        CancellationToken cancellationToken = default)
    {
        var method = validator.GetType().GetMethod(
            "Validate",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[]
            {
                typeof(string),
                typeof(ImageEditWorkspace),
                typeof(TextEditWorkspace),
                typeof(CancellationToken)
            },
            modifiers: null);
        Assert.NotNull(method);

        try
        {
            method!.Invoke(validator, new object?[] { outputPath, imageWorkspace, textWorkspace, cancellationToken });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static object CreateWriter()
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType(
            "SGPdf.App.Pdf.PdfEditWriter",
            throwOnError: true)!;
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

    private static void WriteSimpleHelveticaPdf(
        string path,
        string text,
        int fontSize,
        int red,
        int green,
        int blue,
        int x,
        int y)
    {
        if (text.Any(ch => ch is '(' or ')' or '\\' || ch > 0x7f))
            throw new ArgumentException("The raw validator fixture accepts ASCII text only.", nameof(text));

        var color = $"{red / 255d:0.###} {green / 255d:0.###} {blue / 255d:0.###} rg";
        var content = $"BT\n/F1 {fontSize} Tf\n{color}\n1 0 0 1 {x} {y} Tm\n({text}) Tj\nET\n";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            Stream(content)
        };
        WritePdf(path, objects);
    }

    private static void WriteVectorOnlyPdf(string path)
    {
        const string content = "0 0 0 rg\n72 280 80 20 re\nf\n";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Resources << >> /Contents 4 0 R >>",
            Stream(content)
        };
        WritePdf(path, objects);
    }

    private static string Stream(string content)
        => $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream";

    private static void WritePdf(string path, IReadOnlyList<string> objects)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true)
        {
            NewLine = "\n"
        };

        writer.Write("%PDF-1.4\n");
        writer.Flush();

        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            writer.Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
            writer.Flush();
        }

        var xrefOffset = stream.Position;
        writer.Write($"xref\n0 {objects.Count + 1}\n");
        writer.Write("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            writer.Write($"{offset:0000000000} 00000 n \n");
        writer.Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        writer.Flush();
    }
}