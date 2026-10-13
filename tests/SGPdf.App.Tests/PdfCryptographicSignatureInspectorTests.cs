using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PdfSharp.Pdf;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCryptographicSignatureInspectorTests
{
    [Fact]
    public void GetCryptographicSignatures_ReturnsManagedSnapshotForPdfSharpSignature()
    {
        string sourcePath = Path.Combine(Path.GetTempPath(), $"sgpdf-f12-2-inspector-source-{Guid.NewGuid():N}.pdf");
        string signedPath = Path.Combine(Path.GetTempPath(), $"sgpdf-f12-2-inspector-signed-{Guid.NewGuid():N}.pdf");

        try
        {
            CreateSourcePdf(sourcePath);
            using RSA rsa = RSA.Create(2048);
            using X509Certificate2 certificate = CreateCertificate(rsa);
            InvokeWriter(sourcePath, signedPath, certificate);

            using PdfDocumentSession session = PdfDocumentSession.Open(signedPath);
            IReadOnlyList<object> signatures = InvokeGetCryptographicSignatures(session);

            object signature = Assert.Single(signatures);
            Type signatureType = signature.GetType();
            byte[] contents = (byte[])signatureType.GetProperty("Contents")!.GetValue(signature)!;
            int[] byteRange = (int[])signatureType.GetProperty("ByteRange")!.GetValue(signature)!;
            string subFilter = (string)signatureType.GetProperty("SubFilter")!.GetValue(signature)!;

            Assert.NotEmpty(contents);
            Assert.Equal(4, byteRange.Length);
            Assert.Equal("adbe.pkcs7.detached", subFilter);
            Assert.DoesNotContain(
                signatureType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                property => property.PropertyType == typeof(IntPtr));
            Assert.DoesNotContain(
                signatureType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                field => field.FieldType == typeof(IntPtr));
        }
        finally
        {
            TryDelete(sourcePath);
            TryDelete(signedPath);
        }
    }

    private static IReadOnlyList<object> InvokeGetCryptographicSignatures(PdfDocumentSession session)
    {
        MethodInfo? method = typeof(PdfDocumentSession).GetMethod(
            "GetCryptographicSignatures",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        object? result = method.Invoke(session, new object[] { CancellationToken.None });
        Assert.NotNull(result);
        return ((IEnumerable)result).Cast<object>().ToArray();
    }

    private static void InvokeWriter(
        string sourcePath,
        string destinationPath,
        X509Certificate2 certificate)
    {
        Type type = typeof(MainWindow).Assembly.GetType(
            "SGPdf.App.Features.Signing.PdfCryptographicSignatureWriter",
            throwOnError: true)!;
        MethodInfo method = type.GetMethod(
            "Write",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        try
        {
            method.Invoke(
                null,
                new object[] { sourcePath, destinationPath, certificate, CancellationToken.None });
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }
    }

    private static void CreateSourcePdf(string path)
    {
        using var document = new PdfDocument();
        document.AddPage();
        document.Save(path);
    }

    private static X509Certificate2 CreateCertificate(RSA rsa)
    {
        var request = new CertificateRequest(
            "CN=SG PDF Task 4 Inspector",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
