using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Organize;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ImageEditPreservationTests
{
    private static readonly Assembly AppAssembly = typeof(PdfDocumentSession).Assembly;

    [Theory]
    [InlineData(nameof(OrganizeFindingKind.Form))]
    [InlineData(nameof(OrganizeFindingKind.Bookmark))]
    [InlineData(nameof(OrganizeFindingKind.NamedDestination))]
    [InlineData(nameof(OrganizeFindingKind.InternalLink))]
    [InlineData(nameof(OrganizeFindingKind.TaggedStructure))]
    [InlineData(nameof(OrganizeFindingKind.PageLabel))]
    [InlineData(nameof(OrganizeFindingKind.Attachment))]
    public void RealF6Writer_MinimalImageEdit_PreservesRepresentativeStructure(string findingKindName)
    {
        using var fixture = ImageEditPreservationFixture.CreateRich();
        var destination = WriteMinimalImageEdit(fixture);
        var findingKind = Enum.Parse<OrganizeFindingKind>(findingKindName);

        using var source = PdfDocumentSession.Open(fixture.Path);
        using var output = PdfDocumentSession.Open(destination);
        var detector = new OrganizePreflightInspector();

        Assert.Contains(detector.Inspect(source).Findings, finding => finding.Kind == findingKind);
        Assert.Contains(detector.Inspect(output).Findings, finding => finding.Kind == findingKind);
    }

    [Fact]
    public void RealF6Writer_MinimalImageEdit_PreservesRepresentativeMetadataValues()
    {
        using var fixture = ImageEditPreservationFixture.CreateRich();
        var destination = WriteMinimalImageEdit(fixture);

        using var source = PdfDocumentSession.Open(fixture.Path);
        using var output = PdfDocumentSession.Open(destination);

        Assert.Equal("SG PDF F6 preservation fixture", ReadMetadata(source, "Title"));
        Assert.Equal("SG PDF tests", ReadMetadata(source, "Author"));
        Assert.Equal(ReadMetadata(source, "Title"), ReadMetadata(output, "Title"));
        Assert.Equal(ReadMetadata(source, "Author"), ReadMetadata(output, "Author"));
    }

    [Fact]
    public void ImageEditPreflight_CleanImageDocument_CanProceedWithoutWarning()
    {
        using var fixture = ImageEditPreservationFixture.CreatePlain();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var result = InspectImageEditPreflight(session);

        Assert.True(ReadBool(result, "CanProceed"));
        Assert.False(ReadBool(result, "RequiresWarningConfirmation"));
        Assert.Empty(ReadFindings(result));
    }

    private static string WriteMinimalImageEdit(ImageEditPreservationFixture fixture)
    {
        using var source = PdfDocumentSession.Open(fixture.Path);
        var image = Assert.Single(source.GetImageObjects(0));
        var workspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var state = workspace.EnsureObject(image);
        workspace.Commit(ImageEditOperationKind.Move, state with
        {
            CurrentMatrix = state.CurrentMatrix with { E = state.CurrentMatrix.E + 1d }
        });

        var destination = Path.Combine(fixture.DirectoryPath, $"edited-{Guid.NewGuid():N}.pdf");
        new PdfImageEditWriter().SaveAsCopy(
            workspace,
            destination,
            warningsConfirmed: true,
            CancellationToken.None);

        using var saved = PdfDocumentSession.Open(destination);
        var savedImage = Assert.Single(saved.GetImageObjects(0));
        Assert.Equal(state.CurrentMatrix.E + 1d, savedImage.Matrix.E, precision: 2);
        return destination;
    }

    private static string ReadMetadata(PdfDocumentSession session, string tag)
    {
        var method = typeof(PdfDocumentSession).GetMethod(
            "GetOrganizeMetadataText",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return Assert.IsType<string>(method.Invoke(session, new object[] { tag, CancellationToken.None }));
    }

    private static object InspectImageEditPreflight(PdfDocumentSession session)
    {
        var inspectorType = AppAssembly.GetType(
            "SGPdf.App.Features.Edit.Images.ImageEditPreflightInspector",
            throwOnError: false);
        Assert.NotNull(inspectorType);

        var inspector = Activator.CreateInstance(
            inspectorType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: Array.Empty<object>(),
            culture: null);
        Assert.NotNull(inspector);

        var method = inspectorType.GetMethod(
            "Inspect",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);
        try
        {
            var result = method.Invoke(inspector, new object[] { session, CancellationToken.None });
            Assert.NotNull(result);
            return result;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static bool ReadBool(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsType<bool>(property.GetValue(instance));
    }

    private static IReadOnlyList<object> ReadFindings(object result)
    {
        var property = result.GetType().GetProperty(
            "Findings",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        var enumerable = Assert.IsAssignableFrom<System.Collections.IEnumerable>(property.GetValue(result));
        return enumerable.Cast<object>().ToArray();
    }
}

internal sealed class ImageEditPreservationFixture : IDisposable
{
    private ImageEditPreservationFixture(string directoryPath, string path)
    {
        DirectoryPath = directoryPath;
        Path = path;
    }

    internal string DirectoryPath { get; }
    internal string Path { get; }

    internal static ImageEditPreservationFixture CreatePlain()
    {
        var content = "q 80 0 0 60 40 50 cm /Im1 Do Q";
        return Create("plain-image.pdf", new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [4 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< >>\nendobj\n",
            "4 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Resources << /XObject << /Im1 6 0 R >> >> /Contents 5 0 R >>\nendobj\n",
            StreamObject(5, string.Empty, content),
            StreamObject(6, "/Type /XObject /Subtype /Image /Width 1 /Height 1 /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /ASCIIHexDecode", "FF0000>")
        }, "<< /Size 7 /Root 1 0 R >>");
    }

    internal static ImageEditPreservationFixture CreateRich()
    {
        var imageContent = "q 80 0 0 60 40 50 cm /Im1 Do Q";
        return Create("rich-image.pdf", new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Outlines 8 0 R /PageMode /UseOutlines /AcroForm 11 0 R /Names << /Dests 13 0 R /EmbeddedFiles 15 0 R >> /MarkInfo << /Marked true >> /StructTreeRoot 14 0 R /PageLabels 18 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [4 0 R 6 0 R] /Count 2 >>\nendobj\n",
            "3 0 obj\n<< >>\nendobj\n",
            "4 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Resources << /XObject << /Im1 19 0 R >> >> /Contents 5 0 R /Annots [10 0 R 12 0 R] >>\nendobj\n",
            StreamObject(5, string.Empty, imageContent),
            "6 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Contents 7 0 R >>\nendobj\n",
            StreamObject(7, string.Empty, string.Empty),
            "8 0 obj\n<< /Type /Outlines /First 9 0 R /Last 9 0 R /Count 1 >>\nendobj\n",
            "9 0 obj\n<< /Title (Root) /Parent 8 0 R /Dest [4 0 R /Fit] >>\nendobj\n",
            "10 0 obj\n<< /Type /Annot /Subtype /Link /Rect [20 30 120 60] /Border [0 0 0] /Dest [6 0 R /Fit] >>\nendobj\n",
            "11 0 obj\n<< /Fields [12 0 R] >>\nendobj\n",
            "12 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (Name) /Rect [20 20 120 45] /P 4 0 R >>\nendobj\n",
            "13 0 obj\n<< /Names [(ChapterOne) [4 0 R /Fit]] >>\nendobj\n",
            "14 0 obj\n<< /Type /StructTreeRoot /K [] >>\nendobj\n",
            "15 0 obj\n<< /Names [(note.txt) 16 0 R] >>\nendobj\n",
            "16 0 obj\n<< /Type /Filespec /F (note.txt) /UF (note.txt) /EF << /F 17 0 R >> >>\nendobj\n",
            StreamObject(17, "/Type /EmbeddedFile", "hello"),
            "18 0 obj\n<< /Nums [0 << /S /D /P (A-) >>] >>\nendobj\n",
            StreamObject(19, "/Type /XObject /Subtype /Image /Width 1 /Height 1 /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /ASCIIHexDecode", "FF0000>"),
            "20 0 obj\n<< /Title (SG PDF F6 preservation fixture) /Author (SG PDF tests) >>\nendobj\n"
        }, "<< /Size 21 /Root 1 0 R /Info 20 0 R >>");
    }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }

    private static ImageEditPreservationFixture Create(
        string fileName,
        IReadOnlyList<string> objects,
        string trailer)
    {
        var directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"sgpdf-f6-preservation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = System.IO.Path.Combine(directory, fileName);
        File.WriteAllBytes(path, BuildPdf(objects, trailer));
        return new ImageEditPreservationFixture(directory, path);
    }

    private static string StreamObject(int objectNumber, string dictionaryEntries, string stream)
    {
        var length = Encoding.Latin1.GetByteCount(stream);
        var entries = string.IsNullOrWhiteSpace(dictionaryEntries)
            ? $"/Length {length}"
            : $"{dictionaryEntries} /Length {length}";
        return $"{objectNumber} 0 obj\n<< {entries} >>\nstream\n{stream}\nendstream\nendobj\n";
    }

    private static byte[] BuildPdf(IReadOnlyList<string> objects, string trailer)
    {
        using var stream = new MemoryStream();
        WriteLatin1(stream, "%PDF-1.7\n");
        var offsets = new long[objects.Count + 1];
        for (var index = 0; index < objects.Count; index++)
        {
            offsets[index + 1] = stream.Position;
            WriteLatin1(stream, objects[index]);
        }

        var xrefOffset = stream.Position;
        WriteLatin1(stream, $"xref\n0 {objects.Count + 1}\n");
        WriteLatin1(stream, "0000000000 65535 f \n");
        for (var index = 1; index < offsets.Length; index++)
            WriteLatin1(stream, $"{offsets[index]:D10} 00000 n \n");
        WriteLatin1(stream, $"trailer\n{trailer}\nstartxref\n{xrefOffset}\n%%EOF\n");
        return stream.ToArray();
    }

    private static void WriteLatin1(Stream stream, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
