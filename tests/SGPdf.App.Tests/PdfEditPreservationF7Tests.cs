using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfEditPreservationF7Tests
{
    [Fact]
    public void CombinedWriter_RealImageAndTextEdit_PreservesRepresentativeDocumentStructures()
    {
        using var fixture = F7PreservationFixture.Create();
        var destination = WriteCombinedEdit(fixture);

        using var output = PdfDocumentSession.Open(destination);
        var raw = Encoding.Latin1.GetString(File.ReadAllBytes(destination));

        Assert.NotEqual(0, output.GetOrganizeFormType());
        Assert.Matches(new Regex(@"/V\s*\(Filled Value\)", RegexOptions.CultureInvariant), raw);

        var bookmark = Assert.Single(output.GetBookmarks());
        Assert.Equal("Root", bookmark.Title);
        Assert.Equal(0, bookmark.DestinationPageIndex);

        Assert.True(output.HasOrganizeNamedDestinations());
        Assert.Contains("(ChapterOne)", raw, StringComparison.Ordinal);

        var internalLink = Assert.Single(
            output.GetPageLinks(0),
            link => link.ActionKind == PdfLinkActionKind.InternalGoto);
        Assert.Equal(1, internalLink.DestinationPageIndex);

        Assert.True(output.IsOrganizeTagged());
        Assert.Contains("/StructTreeRoot", raw, StringComparison.Ordinal);

        Assert.True(output.HasOrganizePageLabels());
        Assert.Contains("(A-)", raw, StringComparison.Ordinal);

        Assert.True(output.HasOrganizeAttachments());
        var attachment = NativeAttachmentReader.ReadSingle(destination);
        Assert.Equal("note.txt", attachment.Name);
        Assert.Equal("hello-f7", Encoding.UTF8.GetString(attachment.Payload));

        Assert.Equal("SG PDF F7 preservation fixture", output.GetOrganizeMetadataText("Title"));
        Assert.Equal("SG PDF Task 11", output.GetOrganizeMetadataText("Author"));
        Assert.Matches(
            new Regex(@"/CustomMarker\s*\(F7 Task11 custom metadata\)", RegexOptions.CultureInvariant),
            raw);

        Assert.Equal(1, output.GetPageRotation(1));
    }

    [Theory]
    [InlineData("Form")]
    [InlineData("Bookmark")]
    [InlineData("NamedDestination")]
    [InlineData("InternalLink")]
    [InlineData("TaggedStructure")]
    [InlineData("PageLabel")]
    [InlineData("Attachment")]
    [InlineData("Metadata")]
    [InlineData("PageRotation")]
    public void F7Preflight_MeasuredPreservedStructures_AreInfoAndProvenPreserved(string findingKind)
    {
        using var fixture = F7PreservationFixture.Create();
        using var source = PdfDocumentSession.Open(fixture.Path);

        var result = new PdfEditPreflightInspector((_, _) => 0).Inspect(source);
        var finding = Assert.Single(result.Findings, item => item.Kind.ToString() == findingKind);

        Assert.Equal(PdfEditFindingSeverity.Info, finding.Severity);
        Assert.Equal(PdfEditPreservationStatus.ProvenPreserved, finding.PreservationStatus);
        Assert.DoesNotContain("writer F6", finding.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.CanProceed);
        Assert.False(result.RequiresWarningConfirmation);
    }

    private static string WriteCombinedEdit(F7PreservationFixture fixture)
    {
        using var source = PdfDocumentSession.Open(fixture.Path);

        var image = Assert.Single(source.GetImageObjects(0));
        var imageWorkspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var imageState = imageWorkspace.EnsureObject(image);
        imageWorkspace.Commit(ImageEditOperationKind.Move, imageState with
        {
            CurrentMatrix = imageState.CurrentMatrix with { E = imageState.CurrentMatrix.E + 1d }
        });

        var text = Assert.Single(source.GetTextObjects(0));
        Assert.Equal("BASE", text.Text);
        var textWorkspace = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var textState = textWorkspace.EnsureObject(text);
        var candidate = textWorkspace.PrepareCandidate(
            textState.Key,
            "SEAB",
            textState.FontSize,
            textState.FillColor);
        Assert.NotNull(candidate.Candidate);
        Assert.Equal(TextFontStrategy.OriginalFont, candidate.Candidate!.FontStrategy);
        textWorkspace.CommitCandidate(candidate.Candidate);

        var destination = fixture.NewOutputPath("f7-preservation-edited.pdf");
        new PdfEditWriter().SaveAsCopy(
            imageWorkspace,
            textWorkspace,
            destination,
            warningsConfirmed: true,
            CancellationToken.None);

        using var saved = PdfDocumentSession.Open(destination);
        Assert.Equal("SEAB", Assert.Single(saved.GetTextObjects(0)).Text);
        Assert.Equal(imageState.CurrentMatrix.E + 1d, Assert.Single(saved.GetImageObjects(0)).Matrix.E, precision: 2);
        return destination;
    }
}

internal static class NativeAttachmentReader
{
    private const string Library = "pdfium";

    internal static (string Name, byte[] Payload) ReadSingle(string path)
    {
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait();
        IntPtr document = IntPtr.Zero;
        try
        {
            document = PdfiumNative.FPDF_LoadDocument(path, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not open the F7 preservation output.");
            Assert.Equal(1, PdfiumNative.FPDFDoc_GetAttachmentCount(document));

            var attachment = FPDFDoc_GetAttachment(document, 0);
            Assert.NotEqual(IntPtr.Zero, attachment);
            return (ReadName(attachment), ReadPayload(attachment));
        }
        finally
        {
            if (document != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(document);
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static string ReadName(IntPtr attachment)
    {
        var required = FPDFAttachment_GetName(attachment, IntPtr.Zero, 0);
        Assert.True(required >= 2u);
        var buffer = Marshal.AllocHGlobal(checked((int)required));
        try
        {
            var copied = FPDFAttachment_GetName(attachment, buffer, required);
            Assert.Equal(required, copied);
            return Marshal.PtrToStringUni(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static byte[] ReadPayload(IntPtr attachment)
    {
        Assert.NotEqual(0, FPDFAttachment_GetFile(attachment, IntPtr.Zero, 0, out var required));
        Assert.True(required > 0u);
        var buffer = Marshal.AllocHGlobal(checked((int)required));
        try
        {
            Assert.NotEqual(0, FPDFAttachment_GetFile(attachment, buffer, required, out var copied));
            Assert.Equal(required, copied);
            var bytes = new byte[checked((int)copied)];
            Marshal.Copy(buffer, bytes, 0, bytes.Length);
            return bytes;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr FPDFDoc_GetAttachment(IntPtr document, int index);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    private static extern uint FPDFAttachment_GetName(IntPtr attachment, IntPtr buffer, uint buflen);

    [DllImport(Library, CallingConvention = CallingConvention.StdCall)]
    private static extern int FPDFAttachment_GetFile(
        IntPtr attachment,
        IntPtr buffer,
        uint buflen,
        out uint outBuflen);
}

internal sealed class F7PreservationFixture : IDisposable
{
    private F7PreservationFixture(string directoryPath, string path)
    {
        DirectoryPath = directoryPath;
        Path = path;
    }

    internal string DirectoryPath { get; }
    internal string Path { get; }

    internal string NewOutputPath(string fileName) => System.IO.Path.Combine(DirectoryPath, fileName);

    internal static F7PreservationFixture Create()
    {
        const string pageOneContent =
            "q 80 0 0 60 40 50 cm /Im1 Do Q\n" +
            "BT /F1 18 Tf 0 0 0 rg 1 0 0 1 150 300 Tm (BASE) Tj ET";

        var objects = new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Outlines 8 0 R /PageMode /UseOutlines /AcroForm 11 0 R /Names << /Dests 13 0 R /EmbeddedFiles 15 0 R >> /MarkInfo << /Marked true >> /StructTreeRoot 14 0 R /PageLabels 18 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [4 0 R 6 0 R] /Count 2 >>\nendobj\n",
            "3 0 obj\n<< >>\nendobj\n",
            "4 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Resources << /XObject << /Im1 19 0 R >> /Font << /F1 21 0 R >> >> /Contents 5 0 R /Annots [10 0 R 12 0 R] >>\nendobj\n",
            StreamObject(5, string.Empty, pageOneContent),
            "6 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Rotate 90 /Contents 7 0 R >>\nendobj\n",
            StreamObject(7, string.Empty, string.Empty),
            "8 0 obj\n<< /Type /Outlines /First 9 0 R /Last 9 0 R /Count 1 >>\nendobj\n",
            "9 0 obj\n<< /Title (Root) /Parent 8 0 R /Dest [4 0 R /Fit] >>\nendobj\n",
            "10 0 obj\n<< /Type /Annot /Subtype /Link /Rect [20 30 120 60] /Border [0 0 0] /Dest [6 0 R /Fit] >>\nendobj\n",
            "11 0 obj\n<< /Fields [12 0 R] >>\nendobj\n",
            "12 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (Name) /V (Filled Value) /DV (Filled Value) /Rect [20 20 120 45] /P 4 0 R >>\nendobj\n",
            "13 0 obj\n<< /Names [(ChapterOne) [4 0 R /Fit]] >>\nendobj\n",
            "14 0 obj\n<< /Type /StructTreeRoot /K [] >>\nendobj\n",
            "15 0 obj\n<< /Names [(note.txt) 16 0 R] >>\nendobj\n",
            "16 0 obj\n<< /Type /Filespec /F (note.txt) /UF (note.txt) /EF << /F 17 0 R >> >>\nendobj\n",
            StreamObject(17, "/Type /EmbeddedFile", "hello-f7"),
            "18 0 obj\n<< /Nums [0 << /S /D /P (A-) >>] >>\nendobj\n",
            StreamObject(19, "/Type /XObject /Subtype /Image /Width 1 /Height 1 /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /ASCIIHexDecode", "FF0000>"),
            "20 0 obj\n<< /Title (SG PDF F7 preservation fixture) /Author (SG PDF Task 11) /CustomMarker (F7 Task11 custom metadata) >>\nendobj\n",
            "21 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n"
        };

        var directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"sgpdf-f7-preservation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = System.IO.Path.Combine(directory, "rich-combined.pdf");
        File.WriteAllBytes(path, BuildPdf(objects, "<< /Size 22 /Root 1 0 R /Info 20 0 R >>"));
        return new F7PreservationFixture(directory, path);
    }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
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
