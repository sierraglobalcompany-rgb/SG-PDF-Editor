using SGPdf.App.Features.Edit;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Comments;

internal sealed class PdfCommentPreflightInspector
{
    private readonly PdfEditPreflightInspector _sharedDetector;

    internal PdfCommentPreflightInspector()
        : this(ReadSignatureCount)
    {
    }

    internal PdfCommentPreflightInspector(
        Func<PdfDocumentSession, CancellationToken, int> signatureCountReader)
    {
        ArgumentNullException.ThrowIfNull(signatureCountReader);
        _sharedDetector = new PdfEditPreflightInspector(signatureCountReader);
    }

    internal PdfEditPreflightResult Inspect(
        PdfDocumentSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        var detected = _sharedDetector.Inspect(session, cancellationToken);
        var findings = detected.Findings
            .Select(MapFinding)
            .ToArray();
        return new PdfEditPreflightResult(findings);
    }

    private static PdfEditFinding MapFinding(PdfEditFinding finding)
        => finding.Kind switch
        {
            PdfEditFindingKind.CryptographicSignature => new PdfEditFinding(
                finding.Kind,
                PdfEditFindingSeverity.Block,
                "COMENTAR no modifica PDF con firmas criptográficas o cuya firma no pueda verificarse con seguridad.",
                PdfEditPreservationStatus.Unknown),

            PdfEditFindingKind.PasswordProtectedSource => new PdfEditFinding(
                finding.Kind,
                PdfEditFindingSeverity.Block,
                "COMENTAR no modifica PDF abiertos con contraseña en esta versión.",
                PdfEditPreservationStatus.Unknown),

            PdfEditFindingKind.Metadata or PdfEditFindingKind.PageRotation => new PdfEditFinding(
                finding.Kind,
                PdfEditFindingSeverity.Info,
                $"COMENTAR detectó {Describe(finding.Kind)}; la matriz de preservación completa de F8 se valida por separado.",
                PdfEditPreservationStatus.Unknown),

            _ => new PdfEditFinding(
                finding.Kind,
                PdfEditFindingSeverity.Warning,
                $"COMENTAR detectó {Describe(finding.Kind)} cuya preservación completa aún requiere confirmación explícita en F8.",
                PdfEditPreservationStatus.Unknown)
        };

    private static string Describe(PdfEditFindingKind kind)
        => kind switch
        {
            PdfEditFindingKind.Form => "formularios",
            PdfEditFindingKind.Bookmark => "marcadores",
            PdfEditFindingKind.NamedDestination => "destinos nombrados",
            PdfEditFindingKind.InternalLink => "enlaces internos",
            PdfEditFindingKind.TaggedStructure => "estructura etiquetada",
            PdfEditFindingKind.PageLabel => "etiquetas de página",
            PdfEditFindingKind.PageRotation => "rotación de página",
            PdfEditFindingKind.Attachment => "archivos adjuntos",
            PdfEditFindingKind.Metadata => "metadatos",
            _ => "una estructura protegida"
        };

    private static int ReadSignatureCount(
        PdfDocumentSession session,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var count = session.GetCryptographicSignatureCount();
        cancellationToken.ThrowIfCancellationRequested();
        return count;
    }
}
