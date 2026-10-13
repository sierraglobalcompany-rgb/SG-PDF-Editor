using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using PdfSharp.Pdf.Signatures;

namespace SGPdf.App.Features.Signing;

internal sealed class DotNetCmsSigner : IDigitalSigner
{
    private const string Sha256Oid = "2.16.840.1.101.3.4.2.1";
    private readonly X509Certificate2 _certificate;
    private int _signatureSize;

    internal DotNetCmsSigner(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        _certificate = certificate;
    }

    public string CertificateName =>
        _certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

    public async Task<int> GetSignatureSizeAsync()
    {
        if (_signatureSize == 0)
        {
            byte[] signature = await GetSignatureAsync(
                    new MemoryStream(new byte[] { 0 }))
                .ConfigureAwait(false);
            _signatureSize = signature.Length + 4;
        }

        return _signatureSize;
    }

    public Task<byte[]> GetSignatureAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
            throw new ArgumentException("El flujo que se va a firmar debe ser legible.", nameof(stream));

        if (stream.CanSeek)
            stream.Position = 0;

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        byte[] content = buffer.ToArray();

        var signedCms = new SignedCms(new ContentInfo(content), detached: true);
        var signer = new CmsSigner(_certificate)
        {
            DigestAlgorithm = new Oid(Sha256Oid),
            IncludeOption = X509IncludeOption.EndCertOnly
        };
        signedCms.ComputeSignature(signer, silent: true);

        return Task.FromResult(signedCms.Encode());
    }
}
