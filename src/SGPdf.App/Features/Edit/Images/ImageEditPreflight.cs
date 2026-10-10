using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Edit.Images;

internal enum ImageEditFindingSeverity
{
    Info,
    Warning,
    Block
}

internal enum ImageEditPreservationStatus
{
    ProvenPreserved,
    ProvenChangedOrLost,
    Unknown
}

internal enum ImageEditFindingKind
{
    CryptographicSignature,
    PasswordProtectedSource,
    Form,
    Bookmark,
    NamedDestination,
    InternalLink,
    TaggedStructure,
    PageLabel,
    Attachment,
    Metadata
}

internal sealed record ImageEditFinding(
    ImageEditFindingKind Kind,
    ImageEditFindingSeverity Severity,
    string Message,
    ImageEditPreservationStatus PreservationStatus);

internal sealed record ImageEditPreflightResult(IReadOnlyList<ImageEditFinding> Findings)
{
    internal bool CanProceed => Findings.All(finding => finding.Severity != ImageEditFindingSeverity.Block);

    internal bool RequiresWarningConfirmation =>
        CanProceed && Findings.Any(finding => finding.Severity == ImageEditFindingSeverity.Warning);
}

internal sealed class ImageEditPreflightInspector
{
    private readonly Func<PdfDocumentSession, CancellationToken, int> _signatureCountReader;

    internal ImageEditPreflightInspector()
        : this(ReadSignatureCount)
    {
    }

    internal ImageEditPreflightInspector(Func<PdfDocumentSession, CancellationToken, int> signatureCountReader)
    {
        _signatureCountReader = signatureCountReader ?? throw new ArgumentNullException(nameof(signatureCountReader));
    }

    internal ImageEditPreflightResult Inspect(
        PdfDocumentSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        var findings = new List<ImageEditFinding>();

        if (session.OpenedWithPassword)
        {
            findings.Add(new ImageEditFinding(
                ImageEditFindingKind.PasswordProtectedSource,
                ImageEditFindingSeverity.Block,
                "Los PDF abiertos con contraseña no se pueden materializar en EDITAR.",
                ImageEditPreservationStatus.Unknown));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var signatureCount = _signatureCountReader(session, cancellationToken);
        if (signatureCount != 0)
        {
            findings.Add(new ImageEditFinding(
                ImageEditFindingKind.CryptographicSignature,
                ImageEditFindingSeverity.Block,
                signatureCount < 0
                    ? "No se pudo comprobar con seguridad si el PDF contiene firmas criptográficas."
                    : "El PDF contiene una firma criptográfica y está bloqueado para edición de imágenes.",
                ImageEditPreservationStatus.Unknown));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (session.GetOrganizeFormType(cancellationToken) != 0)
            findings.Add(CreatePreservedInfo(ImageEditFindingKind.Form, "formularios"));

        cancellationToken.ThrowIfCancellationRequested();
        if (session.GetBookmarks(cancellationToken).Count > 0)
            findings.Add(CreatePreservedInfo(ImageEditFindingKind.Bookmark, "marcadores"));

        cancellationToken.ThrowIfCancellationRequested();
        if (session.HasOrganizeNamedDestinations(cancellationToken))
            findings.Add(CreatePreservedInfo(ImageEditFindingKind.NamedDestination, "destinos nombrados"));

        cancellationToken.ThrowIfCancellationRequested();
        if (ContainsInternalLink(session, cancellationToken))
            findings.Add(CreatePreservedInfo(ImageEditFindingKind.InternalLink, "enlaces internos"));

        cancellationToken.ThrowIfCancellationRequested();
        if (session.IsOrganizeTagged(cancellationToken))
            findings.Add(CreatePreservedInfo(ImageEditFindingKind.TaggedStructure, "estructura etiquetada"));

        cancellationToken.ThrowIfCancellationRequested();
        if (session.HasOrganizePageLabels(cancellationToken))
            findings.Add(CreatePreservedInfo(ImageEditFindingKind.PageLabel, "etiquetas de página"));

        cancellationToken.ThrowIfCancellationRequested();
        if (session.HasOrganizeAttachments(cancellationToken))
            findings.Add(CreatePreservedInfo(ImageEditFindingKind.Attachment, "archivos adjuntos"));

        cancellationToken.ThrowIfCancellationRequested();
        if (session.HasOrganizeMetadata(cancellationToken))
            findings.Add(CreatePreservedInfo(ImageEditFindingKind.Metadata, "metadatos"));

        cancellationToken.ThrowIfCancellationRequested();
        return new ImageEditPreflightResult(findings.AsReadOnly());
    }

    private static int ReadSignatureCount(
        PdfDocumentSession session,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var count = session.GetCryptographicSignatureCount();
        cancellationToken.ThrowIfCancellationRequested();
        return count;
    }

    private static bool ContainsInternalLink(
        PdfDocumentSession session,
        CancellationToken cancellationToken)
    {
        for (var pageIndex = 0; pageIndex < session.PageCount; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (session.GetPageLinks(pageIndex, cancellationToken)
                .Any(link => link.ActionKind == PdfLinkActionKind.InternalGoto))
            {
                return true;
            }
        }

        return false;
    }

    private static ImageEditFinding CreatePreservedInfo(
        ImageEditFindingKind kind,
        string structureName)
        => new(
            kind,
            ImageEditFindingSeverity.Info,
            $"El PDF contiene {structureName}; el writer F6 los preservó en el corpus representativo automatizado.",
            ImageEditPreservationStatus.ProvenPreserved);
}
