using System.IO;
using System.Security.Cryptography.X509Certificates;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Signatures;

namespace SGPdf.App.Features.Signing;

internal static class PdfCryptographicSignatureWriter
{
    internal static void Write(
        string sourcePath,
        string destinationPath,
        X509Certificate2 certificate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("La ruta del PDF fuente es obligatoria.", nameof(sourcePath));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("La ruta del PDF de destino es obligatoria.", nameof(destinationPath));
        ArgumentNullException.ThrowIfNull(certificate);

        cancellationToken.ThrowIfCancellationRequested();

        string sourceFullPath = Path.GetFullPath(sourcePath);
        string destinationFullPath = Path.GetFullPath(destinationPath);

        if (string.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El PDF firmado debe guardarse en una ruta distinta del archivo fuente.", nameof(destinationPath));
        if (!File.Exists(sourceFullPath))
            throw new FileNotFoundException("No existe el PDF fuente indicado.", sourceFullPath);

        bool destinationExisted = File.Exists(destinationFullPath);
        try
        {
            using var document = PdfReader.Open(sourceFullPath, PdfDocumentOpenMode.Modify);
            cancellationToken.ThrowIfCancellationRequested();

            if (document.PageCount == 0)
                throw new InvalidOperationException("El PDF fuente no contiene páginas para firmar.");

            var options = new DigitalSignatureOptions
            {
                PageIndex = 0
            };

            DigitalSignatureHandler.ForDocument(
                document,
                new DotNetCmsSigner(certificate),
                options);

            cancellationToken.ThrowIfCancellationRequested();
            document.Save(destinationFullPath);
        }
        catch
        {
            if (!destinationExisted && File.Exists(destinationFullPath))
            {
                try
                {
                    File.Delete(destinationFullPath);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            throw;
        }
    }
}
