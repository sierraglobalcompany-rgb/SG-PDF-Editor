using System.Windows;
using SGPdf.App.Features.Comments;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Features.Organize;
using SGPdf.App.Pdf;
using SGPdf.App.Shell;

namespace SGPdf.App;

public partial class MainWindow
{
    private void CaptureActiveDocumentPendingState()
    {
        if (ActiveDocument is null)
            return;

        if (_commentInlineNoteEditor is not null)
            CommitInlineNoteEditor(commitOnlyIfActiveMode: false);

        var state = ActiveDocument.PendingState;
        state.Mode = ResolvePendingMode();

        state.ImageEditWorkspace = _imageEditWorkspace;
        state.TextEditWorkspace = _textEditWorkspace;
        state.RedactionWorkspace = _redactionWorkspace;
        state.ActiveImageObjects = _activeImageObjects.ToArray();
        state.ActiveTextObjects = _activeTextObjects.ToArray();
        state.SelectedImageKey = _selectedImageKey;
        state.SelectedTextKey = _selectedTextKey;

        state.SignatureEditState = _signatureEditState;

        state.CommentWorkspace = _commentWorkspace;
        state.CommentTool = _commentTool;
        state.SelectedCommentKey = _selectedCommentKey;
        state.CommentKnownKeys = _commentKnownKeys.ToArray();
        state.CommentSourceInfoByKey = _commentSourceInfoByKey.ToDictionary(pair => pair.Key, pair => pair.Value);
        state.CommentReadOnlyByPage = _commentReadOnlyByPage.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<PdfCommentInfo>)pair.Value.ToArray());
        state.CommentLoadedPages = new HashSet<int>(_commentLoadedPages);
        state.CommentDefaultColor = _commentDefaultColor;
        state.CommentDefaultBorderWidth = _commentDefaultBorderWidth;

        state.OrganizePlan = _organizePlan;
        state.OrganizeDirty = _organizePlanDirty;
        state.OrganizeSourcePageSizes = _organizeSourcePageSizes.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<PdfPageSize>)pair.Value.ToArray());

        state.SetAcroFormState(
            _acroFormOriginalValues,
            CaptureAcroFormCurrentValues(),
            _acroFormPropertiesScrollViewer?.Visibility == Visibility.Visible);
    }

    private PdfDocumentPendingMode ResolvePendingMode()
    {
        if (_organizeModeActive)
            return PdfDocumentPendingMode.Organize;
        if (_commentModeActive)
            return PdfDocumentPendingMode.Comment;
        if (_editModeActive)
            return PdfDocumentPendingMode.Edit;
        if (_signatureModeActive)
            return PdfDocumentPendingMode.Sign;
        if (_acroFormPropertiesScrollViewer?.Visibility == Visibility.Visible)
            return PdfDocumentPendingMode.AcroForm;
        return PdfDocumentPendingMode.Reader;
    }

    private void DetachActiveDocumentPendingUi()
    {
        _editModeActive = false;
        _imageEditWorkspace = null;
        _textEditWorkspace = null;
        _redactionWorkspace = null;
        _activeImageObjects = Array.Empty<PdfImageObjectInfo>();
        _activeTextObjects = Array.Empty<PdfTextObjectInfo>();
        _selectedImageKey = null;
        _selectedTextKey = null;
        if (_editOverlayCanvas is not null)
        {
            _editOverlayCanvas.Children.Clear();
            _editOverlayCanvas.Visibility = Visibility.Collapsed;
        }
        if (_editTextPropertiesScrollViewer is not null)
            _editTextPropertiesScrollViewer.Visibility = Visibility.Collapsed;
        UpdateEditSaveCommandAvailability();

        _signatureModeActive = false;
        _signatureEditState = null;
        if (_signatureOverlayCanvas is not null)
        {
            _signatureOverlayCanvas.Children.Clear();
            _signatureOverlayCanvas.Visibility = Visibility.Collapsed;
        }
        if (_signaturePropertiesPanel is not null)
            _signaturePropertiesPanel.Visibility = Visibility.Collapsed;

        CancelCommentCreationGesture(refresh: false);
        CancelCommentSelectionGesture(refresh: false);
        _commentModeActive = false;
        _commentMaterializing = false;
        _commentWorkspace = null;
        _selectedCommentKey = null;
        _commentKnownKeys.Clear();
        _commentSourceInfoByKey.Clear();
        _commentReadOnlyByPage.Clear();
        _commentLoadedPages.Clear();
        if (_commentOverlayCanvas is not null)
        {
            _commentOverlayCanvas.Children.Clear();
            _commentOverlayCanvas.Visibility = Visibility.Collapsed;
        }
        if (_commentToolbar is not null)
            _commentToolbar.Visibility = Visibility.Collapsed;
        if (_commentPropertiesPanel is not null)
            _commentPropertiesPanel.Visibility = Visibility.Collapsed;
        UpdateCommentCommands();

        _organizeThumbnailRenderScheduler.CancelCurrent();
        _organizeThumbnailRefreshQueued = false;
        _organizeModeActive = false;
        _organizePlan = null;
        _organizePlanDirty = false;
        foreach (var item in _organizePages)
            item.ReleaseBitmap();
        _organizePages.Clear();
        _organizeSourcePageSizes.Clear();
        if (_organizeSurface is not null)
            _organizeSurface.Visibility = Visibility.Collapsed;

        DetachAcroFormUiForDocumentSwitch();
    }

    private void RestoreDocumentPendingState(PdfDocumentWorkspace document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var state = document.PendingState;

        switch (state.Mode)
        {
            case PdfDocumentPendingMode.Edit:
                RestoreEditPendingState(state);
                break;
            case PdfDocumentPendingMode.Sign:
                RestoreSignaturePendingState(state);
                break;
            case PdfDocumentPendingMode.Comment:
                RestoreCommentPendingState(state);
                break;
            case PdfDocumentPendingMode.Organize:
                RestoreOrganizePendingState(state);
                break;
            case PdfDocumentPendingMode.AcroForm:
                RestoreAcroFormPendingState(state.AcroFormOriginalValues, state.AcroFormCurrentValues);
                break;
            default:
                PropertiesPlaceholderText.Visibility = Visibility.Visible;
                break;
        }
    }

    private void RestoreEditPendingState(PdfDocumentPendingState state)
    {
        InitializeEditUi();
        _imageEditWorkspace = state.ImageEditWorkspace;
        _textEditWorkspace = state.TextEditWorkspace;
        _redactionWorkspace = state.RedactionWorkspace;
        _activeImageObjects = state.ActiveImageObjects.ToArray();
        _activeTextObjects = state.ActiveTextObjects.ToArray();
        _selectedImageKey = state.SelectedImageKey;
        _selectedTextKey = state.SelectedTextKey;
        _editModeActive = _imageEditWorkspace is not null;

        if (!_editModeActive)
            return;

        ShowActiveReaderPageForPendingOverlay();
        if (_editOverlayCanvas is not null)
        {
            _editOverlayCanvas.Visibility = Visibility.Visible;
            RefreshImageEditOverlay();
        }
        LabelPropertiesPanel.Visibility = Visibility.Collapsed;
        PropertiesPlaceholderText.Visibility = Visibility.Visible;
        UpdateEditSaveCommandAvailability();
    }

    private void RestoreSignaturePendingState(PdfDocumentPendingState state)
    {
        _signatureEditState = state.SignatureEditState;
        _signatureModeActive = _signatureEditState is not null;
        if (!_signatureModeActive)
            return;

        ShowActiveReaderPageForPendingOverlay();
        if (_signatureOverlayCanvas is not null)
            _signatureOverlayCanvas.Visibility = Visibility.Visible;
        if (_signaturePropertiesPanel is not null)
            _signaturePropertiesPanel.Visibility = Visibility.Visible;
        LabelPropertiesPanel.Visibility = Visibility.Collapsed;
        PropertiesPlaceholderText.Visibility = Visibility.Collapsed;
        RefreshSignatureOverlay();
    }

    private void RestoreCommentPendingState(PdfDocumentPendingState state)
    {
        InitializeCommentUi();
        _commentWorkspace = state.CommentWorkspace;
        _commentModeActive = _commentWorkspace is not null;
        if (!_commentModeActive)
            return;

        _commentTool = state.CommentTool;
        _selectedCommentKey = state.SelectedCommentKey;
        _commentKnownKeys.Clear();
        _commentKnownKeys.AddRange(state.CommentKnownKeys);
        _commentSourceInfoByKey.Clear();
        foreach (var pair in state.CommentSourceInfoByKey)
            _commentSourceInfoByKey[pair.Key] = pair.Value;
        _commentReadOnlyByPage.Clear();
        foreach (var pair in state.CommentReadOnlyByPage)
            _commentReadOnlyByPage[pair.Key] = pair.Value.ToList();
        _commentLoadedPages.Clear();
        foreach (var pageIndex in state.CommentLoadedPages)
            _commentLoadedPages.Add(pageIndex);
        _commentDefaultColor = state.CommentDefaultColor;
        _commentDefaultBorderWidth = state.CommentDefaultBorderWidth;

        ShowContinuousReaderSurface();
        if (_navigation is not null && !_commentLoadedPages.Contains(_navigation.CurrentPageIndex))
            LoadCommentPageIfNeeded(_navigation.CurrentPageIndex);
        if (_commentToolbar is not null)
            _commentToolbar.Visibility = Visibility.Visible;
        if (_commentPropertiesPanel is not null)
            _commentPropertiesPanel.Visibility = Visibility.Visible;
        if (_commentOverlayCanvas is not null)
            _commentOverlayCanvas.Visibility = Visibility.Visible;
        PropertiesPlaceholderText.Visibility = Visibility.Collapsed;
        RefreshCommentOverlay();
        RefreshCommentPropertiesPanel();
        UpdateCommentCommands();
    }

    private void RestoreOrganizePendingState(PdfDocumentPendingState state)
    {
        if (state.OrganizePlan is null)
            return;

        InitializeOrganizeUi();
        _organizePlan = state.OrganizePlan;
        _organizePlanDirty = state.OrganizeDirty;
        _organizeSourcePageSizes.Clear();
        foreach (var pair in state.OrganizeSourcePageSizes)
            _organizeSourcePageSizes[pair.Key] = pair.Value.ToArray();
        ApplyOrganizePlanSnapshot(state.OrganizePlan);
        _organizeModeActive = true;

        if (_readerContinuousSurface is not null)
            _readerContinuousSurface.Visibility = Visibility.Collapsed;
        PdfScrollViewer.Visibility = Visibility.Collapsed;
        PdfImage.Visibility = Visibility.Collapsed;
        NavigationBar.Visibility = Visibility.Collapsed;
        LabelPropertiesPanel.Visibility = Visibility.Collapsed;
        PropertiesPlaceholderText.Visibility = Visibility.Collapsed;
        if (_organizeSurface is not null)
            _organizeSurface.Visibility = Visibility.Visible;
        UpdateReaderNavigationVisibility();
        QueueOrganizeThumbnailRefresh();
    }

    private void ShowActiveReaderPageForPendingOverlay()
    {
        if (_navigation is null || _readerPages.Count == 0)
            return;

        var pageIndex = Math.Clamp(_navigation.CurrentPageIndex, 0, _readerPages.Count - 1);
        var item = _readerPages[pageIndex];
        if (item.Bitmap is not null)
            PdfImage.Source = item.Bitmap;
        if (item.DeviceTransform is PdfPageDeviceTransform transform)
            _currentPdfDeviceTransform = transform;
        else
            SeedEditTransformForPage(pageIndex);

        if (_readerContinuousSurface is not null)
            _readerContinuousSurface.Visibility = Visibility.Collapsed;
        PdfScrollViewer.Visibility = Visibility.Visible;
        PdfImage.Visibility = Visibility.Visible;
        EmptyStateText.Visibility = Visibility.Collapsed;
    }
}
