using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Organize;

internal enum OrganizeFindingSeverity
{
    Info,
    Warning,
    Block
}

internal enum OrganizePreservationStatus
{
    ProvenPreserved,
    ProvenChangedOrLost,
    Unknown
}

internal enum OrganizeFindingKind
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

internal sealed record OrganizeFinding(
    OrganizeFindingKind Kind,
    OrganizeFindingSeverity Severity,
    string Message,
    OrganizePreservationStatus PreservationStatus);

internal sealed record OrganizePreflightResult(IReadOnlyList<OrganizeFinding> Findings)
{
    internal bool CanProceed => Findings.All(finding => finding.Severity != OrganizeFindingSeverity.Block);

    internal bool RequiresWarningConfirmation =>
        CanProceed && Findings.Any(finding => finding.Severity == OrganizeFindingSeverity.Warning);
}

internal sealed class OrganizePreflightInspector
{
    private readonly Func<PdfDocumentSession, CancellationToken, int> _signatureCountReader;

    internal OrganizePreflightInspector()
        : this(ReadSignatureCount)
    {
    }

    internal OrganizePreflightInspector(Func<PdfDocumentSession, CancellationToken, int> signatureCountReader)
    {
        _signatureCountReader = signatureCountReader ?? throw new ArgumentNullException(nameof(signatureCountReader));
    }

    internal OrganizePreflightResult Inspect(
        PdfDocumentSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        var findings = new List<OrganizeFinding>();

        if (session.OpenedWithPassword)
        {
            findings.Add(new OrganizeFinding(
                OrganizeFindingKind.PasswordProtectedSource,
                OrganizeFindingSeverity.Block,
                "Los PDF abiertos con contraseña no se pueden reorganizar en esta versión.",
                OrganizePreservationStatus.Unknown));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (_signatureCountReader(session, cancellationToken) > 0)
        {
            findings.Add(new OrganizeFinding(
                OrganizeFindingKind.CryptographicSignature,
                OrganizeFindingSeverity.Block,
                "El PDF contiene una firma criptográfica y no se modificará estructuralmente.",
                OrganizePreservationStatus.Unknown));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (session.GetOrganizeFormType(cancellationToken) != 0)
        {
            findings.Add(CreateUnknownWarning(
                OrganizeFindingKind.Form,
                "El PDF contiene un formulario; su preservación todavía no está demostrada."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (session.GetBookmarks(cancellationToken).Count > 0)
        {
            findings.Add(CreateUnknownWarning(
                OrganizeFindingKind.Bookmark,
                "El PDF contiene marcadores; su preservación todavía no está demostrada."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (session.HasOrganizeNamedDestinations(cancellationToken))
        {
            findings.Add(CreateUnknownWarning(
                OrganizeFindingKind.NamedDestination,
                "El PDF contiene destinos nombrados; su preservación todavía no está demostrada."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (ContainsInternalLink(session, cancellationToken))
        {
            findings.Add(CreateUnknownWarning(
                OrganizeFindingKind.InternalLink,
                "El PDF contiene enlaces internos; su preservación todavía no está demostrada."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (session.IsOrganizeTagged(cancellationToken))
        {
            findings.Add(CreateUnknownWarning(
                OrganizeFindingKind.TaggedStructure,
                "El PDF contiene estructura etiquetada; su preservación todavía no está demostrada."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (session.HasOrganizePageLabels(cancellationToken))
        {
            findings.Add(CreateUnknownWarning(
                OrganizeFindingKind.PageLabel,
                "El PDF contiene etiquetas de página; su preservación todavía no está demostrada."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (session.HasOrganizeAttachments(cancellationToken))
        {
            findings.Add(CreateUnknownWarning(
                OrganizeFindingKind.Attachment,
                "El PDF contiene archivos adjuntos; su preservación todavía no está demostrada."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (session.HasOrganizeMetadata(cancellationToken))
        {
            findings.Add(CreateUnknownWarning(
                OrganizeFindingKind.Metadata,
                "El PDF contiene metadatos; su preservación todavía no está demostrada."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new OrganizePreflightResult(findings.AsReadOnly());
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
        cancellationToken.ThrowIfCancellationRequested();
        var pageCount = session.PageCount;

        for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
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

    private static OrganizeFinding CreateUnknownWarning(
        OrganizeFindingKind kind,
        string message)
    {
        return new OrganizeFinding(
            kind,
            OrganizeFindingSeverity.Warning,
            message,
            OrganizePreservationStatus.Unknown);
    }
}
