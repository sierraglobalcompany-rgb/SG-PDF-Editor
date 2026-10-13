using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PfxCertificateLoaderTests
{
    private const string Password = "task2-test-password";
    private static readonly DateTimeOffset Now = new(2030, 6, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Load_ValidRsaPfx_ReturnsCertificateWithRsaPrivateKey()
    {
        string path = WriteRsaPfx(Now.AddDays(-1), Now.AddDays(1), includePrivateKey: true);
        try
        {
            using X509Certificate2 certificate = Load(path, Password, Now);
            using RSA? privateKey = certificate.GetRSAPrivateKey();

            Assert.True(certificate.HasPrivateKey);
            Assert.NotNull(privateKey);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_WrongPassword_ThrowsCryptographicException()
    {
        string path = WriteRsaPfx(Now.AddDays(-1), Now.AddDays(1), includePrivateKey: true);
        try
        {
            Assert.Throws<CryptographicException>(() => Load(path, "wrong-password", Now));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_PfxWithoutPrivateKey_ThrowsCryptographicException()
    {
        string path = WriteRsaPfx(Now.AddDays(-1), Now.AddDays(1), includePrivateKey: false);
        try
        {
            Assert.Throws<CryptographicException>(() => Load(path, Password, Now));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_ExpiredCertificate_ThrowsCryptographicException()
    {
        string path = WriteRsaPfx(Now.AddDays(-10), Now.AddDays(-1), includePrivateKey: true);
        try
        {
            Assert.Throws<CryptographicException>(() => Load(path, Password, Now));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_NotYetValidCertificate_ThrowsCryptographicException()
    {
        string path = WriteRsaPfx(Now.AddDays(1), Now.AddDays(10), includePrivateKey: true);
        try
        {
            Assert.Throws<CryptographicException>(() => Load(path, Password, Now));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_EcdsaCertificate_ThrowsCryptographicException()
    {
        string path = WriteEcdsaPfx(Now.AddDays(-1), Now.AddDays(1));
        try
        {
            Assert.Throws<CryptographicException>(() => Load(path, Password, Now));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static X509Certificate2 Load(string path, string password, DateTimeOffset now)
    {
        Type type = typeof(MainWindow).Assembly.GetType(
            "SGPdf.App.Features.Signing.PfxCertificateLoader",
            throwOnError: true)!;
        MethodInfo method = type.GetMethod("Load", BindingFlags.Static | BindingFlags.NonPublic)!;

        try
        {
            return (X509Certificate2)method.Invoke(null, new object[] { path, password, now })!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static string WriteRsaPfx(DateTimeOffset notBefore, DateTimeOffset notAfter, bool includePrivateKey)
    {
        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=SG PDF Task 2 RSA",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.CreateSelfSigned(notBefore, notAfter);

        byte[] pfx;
        if (includePrivateKey)
        {
            pfx = certificate.Export(X509ContentType.Pkcs12, Password);
        }
        else
        {
            using X509Certificate2 publicOnly = X509CertificateLoader.LoadCertificate(
                certificate.Export(X509ContentType.Cert));
            pfx = publicOnly.Export(X509ContentType.Pkcs12, Password);
        }

        return WriteTempPfx(pfx);
    }

    private static string WriteEcdsaPfx(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=SG PDF Task 2 ECDSA", ecdsa, HashAlgorithmName.SHA256);
        using X509Certificate2 certificate = request.CreateSelfSigned(notBefore, notAfter);
        return WriteTempPfx(certificate.Export(X509ContentType.Pkcs12, Password));
    }

    private static string WriteTempPfx(byte[] pfx)
    {
        string path = Path.Combine(Path.GetTempPath(), $"sgpdf-task2-{Guid.NewGuid():N}.pfx");
        File.WriteAllBytes(path, pfx);
        return path;
    }
}
