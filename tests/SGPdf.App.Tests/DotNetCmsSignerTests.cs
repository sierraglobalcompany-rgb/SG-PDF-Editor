using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class DotNetCmsSignerTests
{
    private const string Sha256Oid = "2.16.840.1.101.3.4.2.1";

    [Fact]
    public async Task GetSignatureAsync_ReturnsDetachedSha256CmsForExactContent()
    {
        using RSA rsa = RSA.Create(2048);
        using X509Certificate2 certificate = CreateCertificate(rsa);
        object signer = CreateSigner(certificate);
        byte[] content = "SG PDF F12.2 exact content"u8.ToArray();

        byte[] signature = await GetSignatureAsync(signer, new MemoryStream(content));

        var cms = new SignedCms(new ContentInfo(content), detached: true);
        cms.Decode(signature);
        cms.CheckSignature(verifySignatureOnly: true);

        Assert.Single(cms.SignerInfos);
        Assert.Equal(Sha256Oid, cms.SignerInfos[0].DigestAlgorithm.Value);
        Assert.Contains(cms.Certificates.Cast<X509Certificate2>(), item => item.Thumbprint == certificate.Thumbprint);
    }

    [Fact]
    public async Task GetSignatureAsync_ModifiedContent_FailsVerification()
    {
        using RSA rsa = RSA.Create(2048);
        using X509Certificate2 certificate = CreateCertificate(rsa);
        object signer = CreateSigner(certificate);
        byte[] content = "SG PDF original content"u8.ToArray();
        byte[] signature = await GetSignatureAsync(signer, new MemoryStream(content));
        byte[] modified = content.ToArray();
        modified[0] ^= 0x01;

        var cms = new SignedCms(new ContentInfo(modified), detached: true);
        cms.Decode(signature);

        Assert.Throws<CryptographicException>(() => cms.CheckSignature(verifySignatureOnly: true));
    }

    [Fact]
    public void CertificateName_UsesCertificateSimpleName_AndImplementsPdfSharpSignerInterface()
    {
        using RSA rsa = RSA.Create(2048);
        using X509Certificate2 certificate = CreateCertificate(rsa);
        object signer = CreateSigner(certificate);
        Type interfaceType = Type.GetType(
            "PdfSharp.Pdf.Signatures.IDigitalSigner, PdfSharp.System",
            throwOnError: true)!;

        string certificateName = (string)signer.GetType().GetProperty("CertificateName")!.GetValue(signer)!;

        Assert.Equal(certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false), certificateName);
        Assert.Contains(interfaceType, signer.GetType().GetInterfaces());
    }

    [Fact]
    public async Task GetSignatureSizeAsync_ReservesActualOneByteCmsLengthPlusFour()
    {
        using RSA rsa = RSA.Create(2048);
        using X509Certificate2 certificate = CreateCertificate(rsa);
        object signer = CreateSigner(certificate);

        byte[] actualSignature = await GetSignatureAsync(signer, new MemoryStream(new byte[] { 0 }));
        int reservedSize = await GetSignatureSizeAsync(signer);

        Assert.Equal(actualSignature.Length + 4, reservedSize);
    }

    private static object CreateSigner(X509Certificate2 certificate)
    {
        Type type = typeof(MainWindow).Assembly.GetType(
            "SGPdf.App.Features.Signing.DotNetCmsSigner",
            throwOnError: true)!;
        ConstructorInfo constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(X509Certificate2) },
            modifiers: null)!;
        return constructor.Invoke(new object[] { certificate });
    }

    private static Task<byte[]> GetSignatureAsync(object signer, Stream stream)
    {
        MethodInfo method = signer.GetType().GetMethod("GetSignatureAsync")!;
        return (Task<byte[]>)method.Invoke(signer, new object[] { stream })!;
    }

    private static Task<int> GetSignatureSizeAsync(object signer)
    {
        MethodInfo method = signer.GetType().GetMethod("GetSignatureSizeAsync")!;
        return (Task<int>)method.Invoke(signer, null)!;
    }

    private static X509Certificate2 CreateCertificate(RSA rsa)
    {
        var request = new CertificateRequest(
            "CN=SG PDF Task 2 Signer",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));
    }
}
