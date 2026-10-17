using System.Collections.ObjectModel;
using SGPdf.App.Shell;

namespace SGPdf.App;

public partial class MainWindow
{
    private readonly ObservableCollection<PdfDocumentWorkspace> _openDocuments = new();

    internal IReadOnlyList<PdfDocumentWorkspace> OpenDocuments => _openDocuments;

    internal PdfDocumentWorkspace? ActiveDocument { get; private set; }

    private void TrackOpenedDocument(PdfDocumentWorkspace document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (_openDocuments.Contains(document))
            return;

        _openDocuments.Add(document);
    }

    private void SaveActiveDocumentReaderState()
    {
        if (ActiveDocument is null)
            return;

        if (_navigation is not null)
            ActiveDocument.SetNavigation(_navigation);
        ActiveDocument.SetZoom(_zoom);
        ActiveDocument.VerticalOffset = _readerVerticalOffset;
    }

    private void BindLegacyReaderStateToDocument(PdfDocumentWorkspace document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ActiveDocument = document;
        _session = ActiveDocument?.Session;
        _navigation = ActiveDocument?.Navigation;
        _zoom = ActiveDocument?.Zoom ?? new Pdf.PdfZoomState();
    }

    private void DisposeTrackedDocuments()
    {
        foreach (var document in _openDocuments.ToArray())
            document.Dispose();

        _openDocuments.Clear();
        ActiveDocument = null;
    }
}
