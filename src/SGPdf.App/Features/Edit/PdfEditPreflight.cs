using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Edit;

internal enum PdfEditFindingSeverity
{
    Info,
    Warning,
    Block
}

internal enum PdfEditPreservationStatus
{
    ProvenPreserved,
    ProvenChangedOrLost,
    Unknown
}

internal enum PdfEditFindingKind
{
    CryptographicSignature,
    PasswordProtectedSource,
    Form,
    Bookmark,
    NamedDestination,
    InternalLink,
    TaggedStructure,
    PageLabel,
    PageRotation,
    Attachment,
    Metadata
}

internal sealed record PdfEditFinding(
    PdfEditFindingKind Kind,
    PdfEditFindingSeverity Severity,
    string Message,
    PdfEditPreservationStatus PreservationStatus);

internal sealed record PdfEditPreflightResult(IReadOnlyList<PdfEditFinding> Findings)
{
    internal bool CanProceed => Findings.All(finding => finding.Severity != PdfEditFindingSeverity.Block);

    internal bool RequiresWarningConfirmation =>
        CanProceed && Findings.Any(finding => finding.Severity == PdfEditFindingSeverity.Warning);
}

internal sealed class PdfEditPreflightInspector
{
    private readonly Func<PdfDocumentSession, CancellationToken, int> _signatureCountReader;

    internal PdfEditPreflightInspector()
        : this(ReadSignatureCount)
    {
    }

    internal PdfEditPreflightInspector(Func<PdfDocumentSession, CancellationToken, int> signatureCountReader)
    {
        _signatureCountReader = signatureCountReader ?? throw new ArgumentNullException(nameof(signatureCountReader));
    }

    internal PdfEditPreflightResult Inspect(
        PdfDocumentSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        var findings = new List<PdfEditFinding>();

        if (session.OpenedWithPassword)
        {
            findings.Add(new PdfEditFinding(
                PdfEditFindingKind.PasswordProtectedSource,
                PdfEditFindingSeverity.Block,
                "Los PDF abiertos con contraseña no se pueden materializar en EDITAR.",
                PdfEditPreservationStatus.Unknown));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var signatureCount = _signatureCountReader(session, cancellationToken);
        if (signatureCount != 0)
        {
            findings.Add(new PdfEditFinding(
                PdfEditFindingKind.CryptographicSignature,
                PdfEditFindingSeverity.Block,
                signatureCount < 0
                    ? "No se pudo comprobar con seguridad si el PDF contiene firmas criptográficas."
                    : "El PDF contiene una firma criptográfica y está bloqueado para edición en EDITAR.",
                PdfEditPreservationStatus.Unknown));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (session.GetOrganizeFormType(cancellationToken) != 0)
            findings.Add(CreatePreservedInfo(PdfEditFindingKind.Form, "formularios"));

        cancellationToken.ThrowIfCancellationRequested();
        if (session.GetBookmarks(cancellationToken).Count > 0)
            findings.Add(CreatePreservedInfo(PdfEditFindingKind.Bookmark, "marcadores"));

        cancellationToken.ThrowIfCancellationRequested();
        if (session.HasOrganizeNamedDestinations(cancellationToken))
            findings.Add(CreatePreservedInfo(PdfEditFindingKind.NamedDestination, "destinos nombrados"));

        cancellationToken.ThrowIfCancellationRequested();
        if (ContainsInternalLink(session, cancellationToken))
            findings.Add(CreatePreservedInfo(PdfEditFindingKind.InternalLink, "enlaces internos"));

        cancellationToken.ThrowIfCancellationRequested();
        if (session.IsOrganizeTagged(cancellationToken))
            findings.Add(CreatePreservedInfo(PdfEditFindingKind.TaggedStructure, "estructura etiquetada"));

        cancellationToken.ThrowIfCancellationRequested();
        if (session.HasOrganizePageLabels(cancellationToken))
            findings.Add(CreatePreservedInfo(PdfEditFindingKind.PageLabel, "etiquetas de página"));

        cancellationToken.ThrowIfCancellationRequested();
        if (ContainsPageRotation(session, cancellationToken))
            findings.Add(CreatePreservedInfo(PdfEditFindingKind.PageRotation, "rotación de página"));

        cancellationToken.ThrowIfCancellationRequested();
        if (session.HasOrganizeAttachments(cancellationToken))
            findings.Add(CreatePreservedInfo(PdfEditFindingKind.Attachment, "archivos adjuntos"));

        cancellationToken.ThrowIfCancellationRequested();
        if (session.HasOrganizeMetadata(cancellationToken))
            findings.Add(CreatePreservedInfo(PdfEditFindingKind.Metadata, "metadatos"));

        cancellationToken.ThrowIfCancellationRequested();
        return new PdfEditPreflightResult(findings.AsReadOnly());
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

    private static bool ContainsPageRotation(
        PdfDocumentSession session,
        CancellationToken cancellationToken)
    {
        for (var pageIndex = 0; pageIndex < session.PageCount; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (session.GetPageRotation(pageIndex, cancellationToken) != 0)
                return true;
        }

        return false;
    }

    private static PdfEditFinding CreatePreservedInfo(
        PdfEditFindingKind kind,
        string structureName)
        => new(
            kind,
            PdfEditFindingSeverity.Info,
            $"El PDF contiene {structureName}; el writer combinado de EDITAR los preservó en el corpus representativo automatizado de F7.",
            PdfEditPreservationStatus.ProvenPreserved);
}
