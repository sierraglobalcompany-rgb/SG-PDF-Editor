using System.Text;
using System.Text.RegularExpressions;
using SGPdf.App.Features.Comments;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCommentPreservationF8Tests
{
    [Fact]
    public void CommentWriter_FreshMeasurement_PreservesRepresentativeDocumentStructuresAndAnnotations()
    {
        using var fixture = F8PreservationFixture.Create();
        var destination = fixture.NewOutputPath("f8-preservation-commented.pdf");
        var workspace = CommentWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        AddNewNote(
            workspace,
            new CommentRect(210d, 285d, 234d, 309d),
            new CommentColor(0, 120, 215, 255),
            "Nueva nota F8 niño café");

        new PdfCommentWriter().SaveAsCopy(
            workspace,
            destination,
            warningsConfirmed: true,
            CancellationToken.None);

        using var output = PdfDocumentSession.Open(destination);
        var raw = Encoding.Latin1.GetString(File.ReadAllBytes(destination));

        Assert.NotEqual(0, output.GetOrganizeFormType());
        Assert.Matches(new Regex(@"/V\s*\(F8 Filled Value\)", RegexOptions.CultureInvariant), raw);

        var bookmark = Assert.Single(output.GetBookmarks());
        Assert.Equal("F8 Root", bookmark.Title);
        Assert.Equal(0, bookmark.DestinationPageIndex);

        Assert.True(output.HasOrganizeNamedDestinations());
        Assert.Contains("(F8Chapter)", raw, StringComparison.Ordinal);

        var internalLink = Assert.Single(
            output.GetPageLinks(0),
            link => link.ActionKind == PdfLinkActionKind.InternalGoto);
        Assert.Equal(1, internalLink.DestinationPageIndex);

        Assert.True(output.IsOrganizeTagged());
        Assert.Contains("/StructTreeRoot", raw, StringComparison.Ordinal);

        Assert.True(output.HasOrganizePageLabels());
        Assert.Contains("(F8-)", raw, StringComparison.Ordinal);

        Assert.True(output.HasOrganizeAttachments());
        var attachment = NativeAttachmentReader.ReadSingle(destination);
        Assert.Equal("f8-note.bin", attachment.Name);
        Assert.Equal("f8-exact-payload-2026", Encoding.UTF8.GetString(attachment.Payload));

        Assert.Equal("SG PDF F8 preservation fixture", output.GetOrganizeMetadataText("Title"));
        Assert.Equal("SG PDF Task 11 F8", output.GetOrganizeMetadataText("Author"));
        Assert.Matches(
            new Regex(@"/CustomMarker\s*\(F8 Task11 fresh metadata\)", RegexOptions.CultureInvariant),
            raw);

        Assert.Equal(1, output.GetPageRotation(1));

        var comments = output.GetCommentsOnPage(0);
        Assert.Contains(comments, item =>
            item.Subtype == CommentSubtype.Text &&
            string.Equals(item.Contents, "Existing supported F8", StringComparison.Ordinal));
        Assert.Contains(comments, item =>
            item.Subtype == CommentSubtype.Text &&
            string.Equals(item.Contents, "Nueva nota F8 niño café", StringComparison.Ordinal));
        Assert.Contains(comments, item => item.Subtype is null);
        Assert.Contains("/Subtype /Stamp", raw, StringComparison.Ordinal);
        Assert.Contains("(Unsupported F8 stamp)", raw, StringComparison.Ordinal);
    }

    [Fact]
    public void CommentWriter_Lifecycle_CreateReopenEditReopenDeleteReopen_IsStable()
    {
        using var fixture = F8PreservationFixture.Create();
        const string createdContents = "Lifecycle creada F8 niño café";
        const string editedContents = "Lifecycle editada F8 Bogotá áé";

        var createdPath = fixture.NewOutputPath("f8-lifecycle-created.pdf");
        var createWorkspace = CommentWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        AddNewNote(
            createWorkspace,
            new CommentRect(185d, 235d, 211d, 261d),
            new CommentColor(255, 210, 0, 255),
            createdContents);
        new PdfCommentWriter().SaveAsCopy(
            createWorkspace,
            createdPath,
            warningsConfirmed: true,
            CancellationToken.None);

        using (var reopenedCreated = PdfDocumentSession.Open(createdPath))
        {
            Assert.Single(
                reopenedCreated.GetCommentsOnPage(0),
                item => item.Subtype == CommentSubtype.Text && item.Contents == createdContents);
        }

        var editedPath = fixture.NewOutputPath("f8-lifecycle-edited.pdf");
        var editWorkspace = BuildWorkspaceForExistingNote(createdPath, createdContents, out var editKey);
        editWorkspace.CommitCandidate(editWorkspace.PrepareNoteText(editKey, editedContents));
        new PdfCommentWriter().SaveAsCopy(
            editWorkspace,
            editedPath,
            warningsConfirmed: true,
            CancellationToken.None);

        using (var reopenedEdited = PdfDocumentSession.Open(editedPath))
        {
            Assert.DoesNotContain(
                reopenedEdited.GetCommentsOnPage(0),
                item => item.Subtype == CommentSubtype.Text && item.Contents == createdContents);
            Assert.Single(
                reopenedEdited.GetCommentsOnPage(0),
                item => item.Subtype == CommentSubtype.Text && item.Contents == editedContents);
        }

        var deletedPath = fixture.NewOutputPath("f8-lifecycle-deleted.pdf");
        var deleteWorkspace = BuildWorkspaceForExistingNote(editedPath, editedContents, out var deleteKey);
        deleteWorkspace.CommitCandidate(deleteWorkspace.PrepareDelete(deleteKey));
        new PdfCommentWriter().SaveAsCopy(
            deleteWorkspace,
            deletedPath,
            warningsConfirmed: true,
            CancellationToken.None);

        using var reopenedDeleted = PdfDocumentSession.Open(deletedPath);
        var finalComments = reopenedDeleted.GetCommentsOnPage(0);
        Assert.DoesNotContain(
            finalComments,
            item => item.Subtype == CommentSubtype.Text && item.Contents == editedContents);
        Assert.Contains(
            finalComments,
            item => item.Subtype == CommentSubtype.Text && item.Contents == "Existing supported F8");
        Assert.Contains(finalComments, item => item.Subtype is null);
    }

    private static CommentWorkspace BuildWorkspaceForExistingNote(
        string sourcePath,
        string contents,
        out CommentKey key)
    {
        var workspace = CommentWorkspace.Create(sourcePath, sourceOpenedWithPassword: false);
        using var session = PdfDocumentSession.Open(sourcePath);
        var comments = session.GetCommentsOnPage(0);
        var identities = CommentIdentity.CreateForPage(comments);
        var commentIndex = Array.FindIndex(
            comments.ToArray(),
            item => item.Subtype == CommentSubtype.Text && item.Contents == contents);
        Assert.True(commentIndex >= 0, $"No se encontró la nota lifecycle '{contents}'.");

        var info = comments[commentIndex];
        var state = CreateExistingState(info);
        workspace.EnsureBaseline(state, identities[commentIndex]);
        key = state.Key;
        return workspace;
    }

    private static CommentKey AddNewNote(
        CommentWorkspace workspace,
        CommentRect rect,
        CommentColor color,
        string contents)
    {
        var state = new CommentState(
            CommentKey.New(),
            pageIndex: 0,
            CommentSubtype.Text,
            rect,
            color,
            quadPoints: null,
            contents,
            inkStrokes: null,
            borderWidth: null,
            isNew: true,
            deleted: false);
        workspace.CommitCandidate(workspace.PrepareCreate(state));
        return state.Key;
    }

    private static CommentState CreateExistingState(PdfCommentInfo info)
        => new(
            CommentKey.New(),
            info.PageIndex,
            info.Subtype ?? throw new InvalidOperationException("Se esperaba un subtipo F8 soportado."),
            info.Rect ?? throw new InvalidOperationException("Se esperaba geometría de comentario."),
            info.Color ?? new CommentColor(0, 0, 0, 255),
            info.Quads,
            info.Contents,
            info.Strokes,
            info.BorderWidth,
            isNew: false,
            deleted: false);
}

internal sealed class F8PreservationFixture : IDisposable
{
    private F8PreservationFixture(string directoryPath, string path)
    {
        DirectoryPath = directoryPath;
        Path = path;
    }

    internal string DirectoryPath { get; }
    internal string Path { get; }

    internal string NewOutputPath(string fileName) => System.IO.Path.Combine(DirectoryPath, fileName);

    internal static F8PreservationFixture Create()
    {
        var objects = new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Outlines 8 0 R /PageMode /UseOutlines /AcroForm 11 0 R /Names << /Dests 13 0 R /EmbeddedFiles 15 0 R >> /MarkInfo << /Marked true >> /StructTreeRoot 14 0 R /PageLabels 18 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [4 0 R 6 0 R] /Count 2 >>\nendobj\n",
            "3 0 obj\n<< >>\nendobj\n",
            "4 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Contents 5 0 R /Annots [10 0 R 12 0 R 20 0 R 21 0 R] >>\nendobj\n",
            StreamObject(5, string.Empty, string.Empty),
            "6 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Rotate 90 /Contents 7 0 R >>\nendobj\n",
            StreamObject(7, string.Empty, string.Empty),
            "8 0 obj\n<< /Type /Outlines /First 9 0 R /Last 9 0 R /Count 1 >>\nendobj\n",
            "9 0 obj\n<< /Title (F8 Root) /Parent 8 0 R /Dest [4 0 R /Fit] >>\nendobj\n",
            "10 0 obj\n<< /Type /Annot /Subtype /Link /Rect [20 30 120 60] /Border [0 0 0] /Dest [6 0 R /Fit] >>\nendobj\n",
            "11 0 obj\n<< /Fields [12 0 R] >>\nendobj\n",
            "12 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (Name) /V (F8 Filled Value) /DV (F8 Filled Value) /Rect [20 20 120 45] /P 4 0 R >>\nendobj\n",
            "13 0 obj\n<< /Names [(F8Chapter) [4 0 R /Fit]] >>\nendobj\n",
            "14 0 obj\n<< /Type /StructTreeRoot /K [] >>\nendobj\n",
            "15 0 obj\n<< /Names [(f8-note.bin) 16 0 R] >>\nendobj\n",
            "16 0 obj\n<< /Type /Filespec /F (f8-note.bin) /UF (f8-note.bin) /EF << /F 17 0 R >> >>\nendobj\n",
            StreamObject(17, "/Type /EmbeddedFile", "f8-exact-payload-2026"),
            "18 0 obj\n<< /Nums [0 << /S /D /P (F8-) >>] >>\nendobj\n",
            "19 0 obj\n<< /Title (SG PDF F8 preservation fixture) /Author (SG PDF Task 11 F8) /CustomMarker (F8 Task11 fresh metadata) >>\nendobj\n",
            "20 0 obj\n<< /Type /Annot /Subtype /Text /Rect [145 205 169 229] /Contents (Existing supported F8) /C [1 0.82 0] /P 4 0 R >>\nendobj\n",
            "21 0 obj\n<< /Type /Annot /Subtype /Stamp /Rect [55 250 155 285] /Contents (Unsupported F8 stamp) /Name /Approved /P 4 0 R >>\nendobj\n"
        };

        var directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"sgpdf-f8-preservation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = System.IO.Path.Combine(directory, "f8-rich-comments.pdf");
        File.WriteAllBytes(path, BuildPdf(objects, "<< /Size 22 /Root 1 0 R /Info 19 0 R >>"));
        return new F8PreservationFixture(directory, path);
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
