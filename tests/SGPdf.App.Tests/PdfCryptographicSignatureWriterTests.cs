using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCryptographicSignatureWriterTests
{
    private const string PreservationText = "SGPDF PRESERVATION TEXT";
    private const string PreservationBookmark = "SGPDF preservation bookmark";
    private const string PreservationUri = "https://example.com/sgpdf-preservation";
    private const string PreservationFieldName = "PreservedField";
    private const string PreservationFieldValue = "PreservedValue";
    private const string PreservationTitle = "SG PDF preservation title";
    private const string PreservationAuthor = "SG PDF preservation author";

    [Fact]
    public void Write_CreatesSignedCopyWithoutChangingSource_AndPreservesBasicRendering()
    {
        string directory = CreateTempDirectory();
        try
        {
            string sourcePath = Path.Combine(directory, "source.pdf");
            string destinationPath = Path.Combine(directory, "signed.pdf");
            CreatePdf(sourcePath);
            byte[] sourceBytes = File.ReadAllBytes(sourcePath);

            using var sourceSession = PdfDocumentSession.Open(sourcePath);
            int sourcePageCount = sourceSession.PageCount;
            PdfRenderedPage sourceRender = sourceSession.RenderPage(0, 72d);

            using X509Certificate2 certificate = CreateCertificate();
            InvokeWrite(sourcePath, destinationPath, certificate, CancellationToken.None);

            Assert.True(File.Exists(destinationPath));
            Assert.Equal(sourceBytes, File.ReadAllBytes(sourcePath));

            using var destinationSession = PdfDocumentSession.Open(destinationPath);
            Assert.Equal(sourcePageCount, destinationSession.PageCount);

            PdfRenderedPage destinationRender = destinationSession.RenderPage(0, 72d);
            Assert.Equal(sourceRender.PixelWidth, destinationRender.PixelWidth);
            Assert.Equal(sourceRender.PixelHeight, destinationRender.PixelHeight);
            Assert.Equal(1, destinationSession.GetCryptographicSignatureCount());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Write_PreservesRepresentativeContentNavigationFormAndMetadata()
    {
        string directory = CreateTempDirectory();
        try
        {
            string sourcePath = Path.Combine(directory, "preservation-source.pdf");
            string destinationPath = Path.Combine(directory, "preservation-signed.pdf");
            CreatePreservationPdf(sourcePath);
            byte[] sourceBytes = File.ReadAllBytes(sourcePath);
            PreservationSnapshot before = ReadPreservationSnapshot(sourcePath);

            using X509Certificate2 certificate = CreateCertificate();
            InvokeWrite(sourcePath, destinationPath, certificate, CancellationToken.None);

            Assert.Equal(sourceBytes, File.ReadAllBytes(sourcePath));
            PreservationSnapshot after = ReadPreservationSnapshot(destinationPath);

            Assert.Equal(before.PageCount, after.PageCount);
            Assert.Equal(2, after.PageCount);
            Assert.True(before.TextMatchCount > 0);
            Assert.Equal(before.TextMatchCount, after.TextMatchCount);
            Assert.Equal(before.ImagePng, after.ImagePng);
            Assert.Equal(PreservationBookmark, before.BookmarkTitle);
            Assert.Equal(before.BookmarkTitle, after.BookmarkTitle);
            Assert.Equal(1, before.BookmarkDestinationPageIndex);
            Assert.Equal(before.BookmarkDestinationPageIndex, after.BookmarkDestinationPageIndex);
            Assert.Equal(PreservationUri, before.LinkUri);
            Assert.Equal(before.LinkUri, after.LinkUri);
            Assert.Equal(PreservationFieldValue, before.PreservedFormValue);
            Assert.Equal(before.PreservedFormValue, after.PreservedFormValue);
            Assert.True(after.FormFieldCount >= before.FormFieldCount);
            Assert.Equal(PreservationTitle, before.Title);
            Assert.Equal(before.Title, after.Title);
            Assert.Equal(PreservationAuthor, before.Author);
            Assert.Equal(before.Author, after.Author);
            Assert.Equal(before.RenderWidth, after.RenderWidth);
            Assert.Equal(before.RenderHeight, after.RenderHeight);
            Assert.Equal(1, after.SignatureCount);

            new PdfCryptographicSignatureValidator().Validate(destinationPath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Write_RejectsSameSourceAndDestinationPath()
    {
        string directory = CreateTempDirectory();
        try
        {
            string sourcePath = Path.Combine(directory, "source.pdf");
            CreatePdf(sourcePath);
            using X509Certificate2 certificate = CreateCertificate();

            Assert.Throws<ArgumentException>(() =>
                InvokeWrite(sourcePath, sourcePath, certificate, CancellationToken.None));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Write_RejectsMissingSource()
    {
        string directory = CreateTempDirectory();
        try
        {
            string sourcePath = Path.Combine(directory, "missing.pdf");
            string destinationPath = Path.Combine(directory, "signed.pdf");
            using X509Certificate2 certificate = CreateCertificate();

            Assert.Throws<FileNotFoundException>(() =>
                InvokeWrite(sourcePath, destinationPath, certificate, CancellationToken.None));
            Assert.False(File.Exists(destinationPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Write_CancelledBeforeSave_DoesNotCreateDestination()
    {
        string directory = CreateTempDirectory();
        try
        {
            string sourcePath = Path.Combine(directory, "source.pdf");
            string destinationPath = Path.Combine(directory, "signed.pdf");
            CreatePdf(sourcePath);
            using X509Certificate2 certificate = CreateCertificate();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                InvokeWrite(sourcePath, destinationPath, certificate, cancellation.Token));
            Assert.False(File.Exists(destinationPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void InvokeWrite(
        string sourcePath,
        string destinationPath,
        X509Certificate2 certificate,
        CancellationToken cancellationToken)
    {
        Type? writerType = typeof(PdfDocumentSession).Assembly.GetType(
            "SGPdf.App.Features.Signing.PdfCryptographicSignatureWriter");
        Assert.NotNull(writerType);

        MethodInfo? write = writerType!.GetMethod(
            "Write",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(string), typeof(string), typeof(X509Certificate2), typeof(CancellationToken) },
            modifiers: null);
        Assert.NotNull(write);

        try
        {
            write!.Invoke(null, new object?[] { sourcePath, destinationPath, certificate, cancellationToken });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static void CreatePdf(string path)
    {
        using var document = new PdfDocument();
        PdfPage page = document.AddPage();
        page.Width = XUnit.FromPoint(400d);
        page.Height = XUnit.FromPoint(600d);
        document.Save(path);
    }

    private static void CreatePreservationPdf(string path)
    {
        using var document = new PdfDocument();
        document.Info.Title = PreservationTitle;
        document.Info.Author = PreservationAuthor;

        PdfPage firstPage = document.AddPage();
        firstPage.Width = XUnit.FromPoint(400d);
        firstPage.Height = XUnit.FromPoint(600d);
        PdfPage secondPage = document.AddPage();
        secondPage.Width = XUnit.FromPoint(420d);
        secondPage.Height = XUnit.FromPoint(620d);

        using (var graphics = XGraphics.FromPdfPage(firstPage))
        using (var imageStream = new MemoryStream(Convert.FromBase64String(
                   "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wAARCAACAAIDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD50ooor8MP9Uz/2Q==")))
        using (var image = XImage.FromStream(imageStream))
        {
            graphics.DrawImage(image, 40d, 100d, 36d, 36d);
        }

        AddRawText(firstPage, document);
        firstPage.AddWebLink(new PdfRectangle(new XRect(40d, 160d, 180d, 24d)), PreservationUri);
        document.Outlines.Add(PreservationBookmark, secondPage, true);
        AddSyntheticAcroForm(document);
        document.Save(path);
    }

    private static void AddRawText(PdfPage page, PdfDocument document)
    {
        PdfDictionary? resources = page.Elements.GetDictionary("/Resources");
        if (resources is null)
        {
            resources = new PdfDictionary(document);
            page.Elements["/Resources"] = resources;
        }

        PdfDictionary? fonts = resources.Elements.GetDictionary("/Font");
        if (fonts is null)
        {
            fonts = new PdfDictionary(document);
            resources.Elements["/Font"] = fonts;
        }

        var font = new PdfDictionary(document);
        font.Elements.SetName("/Type", "/Font");
        font.Elements.SetName("/Subtype", "/Type1");
        font.Elements.SetName("/BaseFont", "/Helvetica");
        document.Internals.AddObject(font);
        fonts.Elements["/FPreserve"] = font.Reference;

        byte[] contentBytes = Encoding.ASCII.GetBytes(
            $"BT /FPreserve 14 Tf 40 500 Td ({PreservationText}) Tj ET\n");
        page.Contents.AppendContent().CreateStream(contentBytes);
    }

    private static void AddSyntheticAcroForm(PdfDocument document)
    {
        var acroForm = new PdfDictionary(document);
        var fields = new PdfArray(document);
        acroForm.Elements["/Fields"] = fields;
        document.Internals.AddObject(acroForm);
        document.Internals.Catalog.Elements["/AcroForm"] = acroForm.Reference;

        var field = new PdfDictionary(document);
        field.Elements.SetName("/FT", "/Tx");
        field.Elements.SetString("/T", PreservationFieldName);
        field.Elements.SetString("/V", PreservationFieldValue);
        document.Internals.AddObject(field);
        fields.Elements.Add(field.Reference!);
    }

    private static PreservationSnapshot ReadPreservationSnapshot(string path)
    {
        int pageCount;
        int textMatchCount;
        byte[] imagePng;
        string? bookmarkTitle;
        int? bookmarkDestinationPageIndex;
        string? linkUri;
        int renderWidth;
        int renderHeight;
        int signatureCount;

        using (var session = PdfDocumentSession.Open(path))
        {
            pageCount = session.PageCount;
            textMatchCount = session.FindTextOnPage(0, PreservationText).Count;
            PdfImageObjectInfo image = Assert.Single(session.GetImageObjects(0));
            imagePng = session.GetImagePng(0, image.PageObjectIndex);

            PdfBookmarkNode bookmark = Assert.Single(session.GetBookmarks());
            bookmarkTitle = bookmark.Title;
            bookmarkDestinationPageIndex = bookmark.DestinationPageIndex;

            PdfPageLink link = Assert.Single(session.GetPageLinks(0));
            linkUri = link.Uri;

            PdfRenderedPage render = session.RenderPage(0, 72d);
            renderWidth = render.PixelWidth;
            renderHeight = render.PixelHeight;
            signatureCount = session.GetCryptographicSignatureCount();
        }

        string title;
        string author;
        int formFieldCount;
        string? preservedFormValue = null;
        using (PdfDocument document = PdfReader.Open(path, PdfDocumentOpenMode.Import))
        {
            title = document.Info.Title;
            author = document.Info.Author;
            PdfDictionary? acroForm = document.Internals.Catalog.Elements.GetDictionary("/AcroForm");
            PdfArray? fields = acroForm?.Elements.GetArray("/Fields");
            formFieldCount = fields?.Elements.Count ?? 0;
            if (fields is not null)
            {
                for (int index = 0; index < fields.Elements.Count; index++)
                {
                    PdfDictionary? field = fields.Elements.GetDictionary(index);
                    if (field?.Elements.GetString("/T") == PreservationFieldName)
                    {
                        preservedFormValue = field.Elements.GetString("/V");
                        break;
                    }
                }
            }
        }

        return new PreservationSnapshot(
            pageCount,
            textMatchCount,
            imagePng,
            bookmarkTitle,
            bookmarkDestinationPageIndex,
            linkUri,
            formFieldCount,
            preservedFormValue,
            title,
            author,
            renderWidth,
            renderHeight,
            signatureCount);
    }

    private static X509Certificate2 CreateCertificate()
    {
        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=SG PDF Task 3 Test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));
    }

    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "sgpdf-signing-writer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed record PreservationSnapshot(
        int PageCount,
        int TextMatchCount,
        byte[] ImagePng,
        string? BookmarkTitle,
        int? BookmarkDestinationPageIndex,
        string? LinkUri,
        int FormFieldCount,
        string? PreservedFormValue,
        string Title,
        string Author,
        int RenderWidth,
        int RenderHeight,
        int SignatureCount);
}
