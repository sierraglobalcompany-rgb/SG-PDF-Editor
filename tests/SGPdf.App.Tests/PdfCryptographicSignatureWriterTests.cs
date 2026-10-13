using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PdfSharp.Pdf;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCryptographicSignatureWriterTests
{
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
        page.Width = PdfSharp.Drawing.XUnit.FromPoint(400d);
        page.Height = PdfSharp.Drawing.XUnit.FromPoint(600d);
        document.Save(path);
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
}
