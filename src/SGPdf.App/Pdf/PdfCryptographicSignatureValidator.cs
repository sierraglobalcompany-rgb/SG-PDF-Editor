using System.Formats.Asn1;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;

namespace SGPdf.App.Pdf;

internal sealed class PdfCryptographicSignatureValidator
{
    private const string SupportedSubFilter = "adbe.pkcs7.detached";
    private const double ValidationDpi = 36d;

    internal PdfCryptographicSignatureValidator()
    {
    }

    internal void Validate(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("No existe el PDF firmado indicado.", fullPath);

        PdfCryptographicSignatureInfo signature;
        using (var session = PdfDocumentSession.Open(fullPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<PdfCryptographicSignatureInfo> signatures =
                session.GetCryptographicSignatures(cancellationToken);

            if (signatures.Count != 1)
            {
                throw new InvalidDataException(
                    $"El PDF debe contener exactamente una firma criptográfica; contiene {signatures.Count}.");
            }

            signature = signatures[0];
            if (!string.Equals(signature.SubFilter, SupportedSubFilter, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"SubFilter de firma no soportado: '{signature.SubFilter}'.");
            }

            if (session.PageCount <= 0)
                throw new InvalidDataException("El PDF firmado no contiene páginas renderizables.");

            _ = session.RenderPage(0, ValidationDpi, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        long fileLength = new FileInfo(fullPath).Length;
        ValidateByteRange(signature.ByteRange, fileLength);

        byte[] signedBytes = ReadSignedBytes(
            fullPath,
            signature.ByteRange,
            cancellationToken);
        byte[] cmsBytes = ExtractCms(signature.Contents);

        var signedCms = new SignedCms(new ContentInfo(signedBytes), detached: true);
        signedCms.Decode(cmsBytes);
        if (signedCms.SignerInfos.Count != 1)
            throw new CryptographicException("El CMS debe contener exactamente un firmante.");

        signedCms.CheckSignature(verifySignatureOnly: true);
    }

    private static void ValidateByteRange(int[] byteRange, long fileLength)
    {
        if (byteRange.Length != 4)
            throw new InvalidDataException("ByteRange debe contener exactamente cuatro enteros.");

        long firstOffset = byteRange[0];
        long firstLength = byteRange[1];
        long secondOffset = byteRange[2];
        long secondLength = byteRange[3];

        if (firstOffset != 0 || firstLength <= 0 || secondOffset < 0 || secondLength <= 0)
            throw new InvalidDataException("ByteRange contiene offsets o longitudes inválidos.");

        long firstEnd = firstOffset + firstLength;
        long secondEnd = secondOffset + secondLength;
        if (firstEnd <= firstOffset || secondEnd <= secondOffset)
            throw new InvalidDataException("ByteRange contiene un desbordamiento o rango vacío.");
        if (firstEnd >= secondOffset)
            throw new InvalidDataException("ByteRange contiene rangos solapados o sin hueco para Contents.");
        if (secondOffset > fileLength || secondEnd != fileLength)
            throw new InvalidDataException("ByteRange queda fuera del archivo o no cubre el documento completo.");

        long signedLength = firstLength + secondLength;
        if (signedLength > int.MaxValue)
            throw new InvalidDataException("ByteRange firmado supera el tamaño soportado por este MVP.");
    }

    private static byte[] ReadSignedBytes(
        string filePath,
        int[] byteRange,
        CancellationToken cancellationToken)
    {
        int firstLength = byteRange[1];
        int secondLength = byteRange[3];
        var signedBytes = new byte[checked(firstLength + secondLength)];

        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.SequentialScan);

        ReadRange(stream, byteRange[0], signedBytes, 0, firstLength, cancellationToken);
        ReadRange(stream, byteRange[2], signedBytes, firstLength, secondLength, cancellationToken);
        return signedBytes;
    }

    private static void ReadRange(
        FileStream stream,
        long fileOffset,
        byte[] destination,
        int destinationOffset,
        int count,
        CancellationToken cancellationToken)
    {
        stream.Position = fileOffset;
        int readTotal = 0;
        while (readTotal < count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int read = stream.Read(
                destination,
                destinationOffset + readTotal,
                count - readTotal);
            if (read == 0)
                throw new EndOfStreamException("El PDF terminó antes de completar el ByteRange firmado.");
            readTotal += read;
        }
    }

    private static byte[] ExtractCms(byte[] contents)
    {
        if (contents.Length == 0)
            throw new CryptographicException("Contents de firma está vacío.");

        try
        {
            var reader = new AsnReader(contents, AsnEncodingRules.DER);
            ReadOnlyMemory<byte> encoded = reader.ReadEncodedValue();
            int encodedLength = encoded.Length;
            if (encodedLength == 0)
                throw new CryptographicException("Contents no contiene un CMS DER válido.");

            ReadOnlySpan<byte> padding = contents.AsSpan(encodedLength);
            if (padding.ContainsAnyExcept((byte)0))
                throw new CryptographicException("Contents contiene datos no nulos después del CMS DER.");

            return encoded.ToArray();
        }
        catch (AsnContentException exception)
        {
            throw new CryptographicException("Contents no contiene un CMS DER válido.", exception);
        }
    }
}
