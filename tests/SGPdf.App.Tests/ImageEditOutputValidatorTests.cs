using System.Reflection;
using System.Runtime.ExceptionServices;
using PdfSharp.Pdf;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ImageEditOutputValidatorTests
{
    [Fact]
    public void Validate_CopyOfSourceWithNoEdits_DoesNotRenderUneditedPages()
    {
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var sourcePath = Path.Combine(directory.Path, "source.pdf");
        var outputPath = Path.Combine(directory.Path, "output.pdf");
        WritePdf(sourcePath, pageCount: 3);
        File.Copy(sourcePath, outputPath);
        var workspace = ImageEditWorkspace.Create(sourcePath, sourceOpenedWithPassword: false);
        var rendered = new List<(int PageIndex, double Dpi)>();
        var validator = CreateValidator((pageIndex, dpi) => rendered.Add((pageIndex, dpi)));

        Validate(validator, outputPath, workspace);

        Assert.Empty(rendered);
    }

    [Fact]
    public void Validate_PageCountMismatch_IsRejected()
    {
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var sourcePath = Path.Combine(directory.Path, "source.pdf");
        var outputPath = Path.Combine(directory.Path, "output.pdf");
        WritePdf(sourcePath, pageCount: 2);
        WritePdf(outputPath, pageCount: 1);
        var workspace = ImageEditWorkspace.Create(sourcePath, sourceOpenedWithPassword: false);

        Assert.Throws<InvalidDataException>(() => Validate(CreateValidator(), outputPath, workspace));
    }

    [Fact]
    public void Validate_InvalidPdf_IsRejectedAsInvalidData()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        var outputPath = Path.Combine(fixture.DirectoryPath, "invalid.pdf");
        File.WriteAllBytes(outputPath, new byte[] { 1, 2, 3, 4, 5, 6 });
        var workspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);

        Assert.Throws<InvalidDataException>(() => Validate(CreateValidator(), outputPath, workspace));
    }

    [Fact]
    public void Validate_StableEditedObjectMatrixMismatch_IsRejected()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        var outputPath = Path.Combine(fixture.DirectoryPath, "unchanged-copy.pdf");
        File.Copy(fixture.Path, outputPath);
        using var source = PdfDocumentSession.Open(fixture.Path);
        var image = Assert.Single(source.GetImageObjects(0));
        var workspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var state = workspace.EnsureObject(image);
        workspace.Commit(ImageEditOperationKind.Move, state with
        {
            CurrentMatrix = state.CurrentMatrix with
            {
                E = state.CurrentMatrix.E + 40d,
                F = state.CurrentMatrix.F + 15d
            }
        });

        Assert.Throws<InvalidDataException>(() => Validate(CreateValidator(), outputPath, workspace));
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

    private static void Validate(
        object validator,
        string outputPath,
        ImageEditWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        var method = validator.GetType().GetMethod(
            "Validate",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(string), typeof(ImageEditWorkspace), typeof(CancellationToken) },
            modifiers: null);
        Assert.NotNull(method);
        try
        {
            method.Invoke(validator, new object?[] { outputPath, workspace, cancellationToken });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static void WritePdf(string path, int pageCount)
    {
        using var document = new PdfDocument();
        for (var index = 0; index < pageCount; index++)
        {
            var page = document.AddPage();
            page.Width = PdfSharp.Drawing.XUnit.FromPoint(300d + index);
            page.Height = PdfSharp.Drawing.XUnit.FromPoint(400d + index);
        }
        document.Save(path);
    }
}