using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SGPdf.App.Features.Signing;

internal static class PfxCertificateLoader
{
    internal static X509Certificate2 Load(string path, string password, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("La ruta del certificado PFX es obligatoria.", nameof(path));

        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("No existe el certificado PFX indicado.", fullPath);

        X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(
            fullPath,
            password,
            X509KeyStorageFlags.EphemeralKeySet);

        try
        {
            if (!certificate.HasPrivateKey)
                throw new CryptographicException("El certificado PFX no contiene una clave privada.");

            using RSA? rsa = certificate.GetRSAPrivateKey();
            if (rsa is null)
                throw new CryptographicException("El certificado PFX debe usar una clave privada RSA.");

            var notBefore = new DateTimeOffset(certificate.NotBefore.ToUniversalTime());
            var notAfter = new DateTimeOffset(certificate.NotAfter.ToUniversalTime());
            if (now < notBefore || now > notAfter)
                throw new CryptographicException("El certificado PFX no es válido en la fecha actual.");

            return certificate;
        }
        catch
        {
            certificate.Dispose();
            throw;
        }
    }
}
