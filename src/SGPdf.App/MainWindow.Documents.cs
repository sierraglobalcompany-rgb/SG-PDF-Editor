using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using SGPdf.App.Features.Labels;
using SGPdf.App.Shell;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool DocumentLifecycleHookRegistered = RegisterDocumentLifecycleHook();
    private readonly ObservableCollection<PdfDocumentWorkspace> _openDocuments = new();
    private bool _documentLifecycleInitialized;

    internal IReadOnlyList<PdfDocumentWorkspace> OpenDocuments => _openDocuments;

    internal PdfDocumentWorkspace? ActiveDocument { get; private set; }

    private static bool RegisterDocumentLifecycleHook()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(DocumentLifecycle_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void DocumentLifecycle_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window || window._documentLifecycleInitialized)
            return;

        window._documentLifecycleInitialized = true;
        _ = DocumentLifecycleHookRegistered;
        window.Closed += DocumentsWindow_Closed;
    }

    private static void DocumentsWindow_Closed(object? sender, EventArgs e)
    {
        if (sender is MainWindow window)
            window.DisposeTrackedDocuments();
    }

    private void TrackOpenedDocument(PdfDocumentWorkspace document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (_openDocuments.Contains(document))
            return;

        _openDocuments.Add(document);
    }

    private void UntrackOpenedDocument(PdfDocumentWorkspace document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _openDocuments.Remove(document);

        if (ReferenceEquals(ActiveDocument, document))
        {
            ActiveDocument = null;
            _session = null;
            _navigation = null;
            _zoom = new Pdf.PdfZoomState();
        }

        document.Dispose();
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

    private async Task ActivateDocumentAsync(PdfDocumentWorkspace document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (ReferenceEquals(ActiveDocument, document))
            return;

        SaveActiveDocumentReaderState();
        _resizeRenderScheduler.CancelCurrent();
        _readerRenderScheduler.CancelCurrent();

        BindLegacyReaderStateToDocument(document);
        _zplDocument = null;
        _renderedZplLabels = Array.Empty<ZplRenderedLabel>();
        _signatureModeActive = false;
        _signatureEditState = null;
        ResetAcroFormUi();

        _updatingReaderScroll = true;
        try
        {
            InitializeContinuousReader(document.PageSizes);
            var pageIndex = Math.Clamp(document.Navigation.CurrentPageIndex, 0, document.PageSizes.Count - 1);
            if (pageIndex > 0)
                ScrollReaderToPage(pageIndex);

            if (document.VerticalOffset > 0d)
            {
                _readerVerticalOffset = document.VerticalOffset;
                ScrollInternalReader(_readerVerticalOffset);
                UpdateReaderCurrentPageFromViewport();
            }

            Title = $"SG PDF Editor — {Path.GetFileName(document.FilePath)}";
            UpdateViewerControlsUi();
            UpdateCurrentPageStatus();
            await RefreshReaderRenderWindowAsync();
        }
        finally
        {
            _updatingReaderScroll = false;
        }
    }

    private void DisposeTrackedDocuments()
    {
        foreach (var document in _openDocuments.ToArray())
            document.Dispose();

        _openDocuments.Clear();
        ActiveDocument = null;
        _session = null;
        _navigation = null;
        _zoom = new Pdf.PdfZoomState();
    }
}
