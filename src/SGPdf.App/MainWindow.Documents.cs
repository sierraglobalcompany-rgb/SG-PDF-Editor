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

    private Func<PdfDocumentWorkspace, bool> _confirmCloseDirtyDocument = static document =>
        MessageBox.Show(
            $"{document.DisplayName} tiene cambios sin guardar.\n\n¿Deseas cerrar este documento y descartar esos cambios?",
            "SG PDF Editor",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;

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
        _session = ActiveDocument.Session;
        _navigation = ActiveDocument.Navigation;
        _zoom = ActiveDocument.Zoom;
    }

    private async Task ActivateDocumentAsync(PdfDocumentWorkspace document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (ReferenceEquals(ActiveDocument, document))
            return;

        CaptureActiveDocumentPendingState();
        SaveActiveDocumentReaderState();
        DetachActiveDocumentPendingUi();
        _resizeRenderScheduler.CancelCurrent();
        _readerRenderScheduler.CancelCurrent();

        BindLegacyReaderStateToDocument(document);
        _zplDocument = null;
        _renderedZplLabels = Array.Empty<ZplRenderedLabel>();

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
            RestoreDocumentPendingState(document);
        }
        finally
        {
            _updatingReaderScroll = false;
        }
    }

    private async Task<bool> TryCloseDocumentWithGuard(PdfDocumentWorkspace document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!_openDocuments.Contains(document))
            return true;

        var wasActive = ReferenceEquals(ActiveDocument, document);
        if (wasActive)
        {
            CaptureActiveDocumentPendingState();
            SaveActiveDocumentReaderState();
        }

        if (document.PendingState.IsDirty && !_confirmCloseDirtyDocument(document))
            return false;

        var fallback = wasActive
            ? _openDocuments.FirstOrDefault(candidate => !ReferenceEquals(candidate, document))
            : null;

        if (fallback is not null)
        {
            await ActivateDocumentAsync(fallback);
        }
        else if (wasActive)
        {
            DetachActiveDocumentPendingUi();
            ActiveDocument = null;
            _session = null;
            _navigation = null;
            _zoom = new Pdf.PdfZoomState();
            _readerPages.Clear();
            _readerPageSizes = Array.Empty<Pdf.PdfPageSize>();
            _readerGeometry = Array.Empty<Features.Reader.ReaderPageGeometry>();
            if (_readerContinuousSurface is not null)
                _readerContinuousSurface.Visibility = Visibility.Collapsed;
            PdfScrollViewer.Visibility = Visibility.Visible;
            PdfImage.Source = null;
            PdfImage.Visibility = Visibility.Collapsed;
            EmptyStateText.Visibility = Visibility.Visible;
            Title = "SG PDF Editor";
            UpdateViewerControlsUi();
        }

        UntrackOpenedDocument(document);
        return true;
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
