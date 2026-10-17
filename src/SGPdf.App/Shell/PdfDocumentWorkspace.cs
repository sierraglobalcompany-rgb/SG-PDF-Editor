using System.IO;
using SGPdf.App.Navigation;
using SGPdf.App.Pdf;

namespace SGPdf.App.Shell;

internal sealed class PdfDocumentWorkspace : IDisposable
{
    private bool _disposed;

    internal PdfDocumentWorkspace(
        PdfDocumentSession session,
        IReadOnlyList<PdfPageSize> pageSizes)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        ArgumentNullException.ThrowIfNull(pageSizes);
        if (pageSizes.Count == 0)
            throw new ArgumentException("El PDF debe contener al menos una página.", nameof(pageSizes));

        PageSizes = pageSizes.ToArray();
        Navigation = new PageNavigationState(PageSizes.Count);
        Zoom = new PdfZoomState();
        PendingState = new PdfDocumentPendingState();
    }

    internal PdfDocumentSession Session { get; }

    internal IReadOnlyList<PdfPageSize> PageSizes { get; }

    public string FilePath => Session.FilePath;

    public string DisplayName => Path.GetFileName(FilePath);

    internal PageNavigationState Navigation { get; private set; }

    internal PdfZoomState Zoom { get; private set; }

    internal double VerticalOffset { get; set; }

    internal PdfDocumentPendingState PendingState { get; }

    internal bool IsDirty { get; private set; }

    internal void SetNavigation(PageNavigationState navigation)
        => Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));

    internal void SetZoom(PdfZoomState zoom)
        => Zoom = zoom ?? throw new ArgumentNullException(nameof(zoom));

    internal void MarkDirty()
        => IsDirty = true;

    internal void MarkClean()
        => IsDirty = false;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Session.Dispose();
    }
}
