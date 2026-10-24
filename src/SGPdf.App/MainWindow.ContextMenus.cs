using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SGPdf.App;

public partial class MainWindow
{
    private ContextMenu? _selectedImageContextMenu;
    private MenuItem? _selectedImageRotateContextMenuItem;
    private MenuItem? _selectedImageReplaceContextMenuItem;
    private MenuItem? _selectedImageExtractContextMenuItem;
    private MenuItem? _selectedImageDeleteContextMenuItem;

    private bool CanOpenPdfWorkspaceContextMenu()
        => _session is not null &&
           _navigation is not null &&
           ActiveDocument is not null &&
           ReferenceEquals(ActiveDocument.Session, _session) &&
           _zplDocument is null &&
           !_isBusy &&
           !_utilityExecuting &&
           !_homeActionStarting &&
           !_documentActivationInProgress &&
           !_editMaterializing &&
           !_commentMaterializing &&
           !_organizeMaterializing &&
           !_editModeActive &&
           !_commentModeActive &&
           !_organizeModeActive &&
           !_signatureModeActive &&
           _acroFormPropertiesScrollViewer?.Visibility != Visibility.Visible &&
           (_readerContinuousSurface?.Visibility == Visibility.Visible ||
            PdfScrollViewer.Visibility == Visibility.Visible);

    private bool HasCurrentReaderTextSelection()
        => CanOpenPdfWorkspaceContextMenu() &&
           _readerTextSelection is { CharacterCount: > 0 } selection &&
           selection.PageIndex >= 0 &&
           selection.PageIndex < _navigation!.PageCount;

    private void PdfWorkspaceContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (!CanOpenPdfWorkspaceContextMenu())
        {
            e.Handled = true;
            return;
        }

        UpdatePdfWorkspaceContextMenuState();
    }

    private void PdfWorkspaceContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        UpdatePdfWorkspaceContextMenuState();
        if (!CanOpenPdfWorkspaceContextMenu() && sender is ContextMenu menu)
            menu.IsOpen = false;
    }

    private void UpdatePdfWorkspaceContextMenuState()
    {
        if (PdfScrollViewer.ContextMenu is not ContextMenu menu || menu.Items.Count < 5)
            return;

        var canUseReader = CanOpenPdfWorkspaceContextMenu();
        var canCopy = canUseReader && HasCurrentReaderTextSelection();
        if (menu.Items[0] is MenuItem copy)
        {
            copy.Visibility = canCopy ? Visibility.Visible : Visibility.Collapsed;
            copy.IsEnabled = canCopy;
        }
        if (menu.Items[1] is Separator separator)
            separator.Visibility = canCopy ? Visibility.Visible : Visibility.Collapsed;
        if (menu.Items[2] is MenuItem fitPage)
            fitPage.IsEnabled = canUseReader;
        if (menu.Items[3] is MenuItem fitWidth)
            fitWidth.IsEnabled = canUseReader;
        if (menu.Items[4] is MenuItem print)
            print.IsEnabled = canUseReader;
    }

    private void PdfWorkspaceCopyText_Click(object sender, RoutedEventArgs e)
    {
        if (HasCurrentReaderTextSelection())
            HandleReaderSelectionKey(System.Windows.Input.Key.C, System.Windows.Input.ModifierKeys.Control);
    }

    private void PdfWorkspaceFitPage_Click(object sender, RoutedEventArgs e)
    {
        if (CanOpenPdfWorkspaceContextMenu())
            FitPage_Click(sender, e);
    }

    private void PdfWorkspaceFitWidth_Click(object sender, RoutedEventArgs e)
    {
        if (CanOpenPdfWorkspaceContextMenu())
            FitWidth_Click(sender, e);
    }

    private void PdfWorkspacePrint_Click(object sender, RoutedEventArgs e)
    {
        if (CanOpenPdfWorkspaceContextMenu())
            PrintPdf_Click(sender, e);
    }

    private void AttachSelectedImageContextMenu(Polygon outline)
    {
        ArgumentNullException.ThrowIfNull(outline);
        outline.ContextMenu = GetOrCreateSelectedImageContextMenu();
        outline.ContextMenuOpening += SelectedImageContextMenu_ContextMenuOpening;
    }

    private ContextMenu GetOrCreateSelectedImageContextMenu()
    {
        if (_selectedImageContextMenu is not null)
            return _selectedImageContextMenu;

        var menu = new ContextMenu();
        var rotate = new MenuItem { Header = "Rotar 90°" };
        var replace = new MenuItem { Header = "Reemplazar..." };
        var extract = new MenuItem { Header = "Extraer PNG..." };
        var delete = new MenuItem { Header = "Eliminar imagen" };
        rotate.Click += (_, _) => InvokeSelectedImageContextAction(() => RotateSelectedImage(90d));
        replace.Click += (_, _) => InvokeSelectedImageContextAction(() => TryReplaceSelectedImageFromDialog());
        extract.Click += (_, _) => InvokeSelectedImageContextAction(() => TryExtractSelectedImageFromDialog());
        delete.Click += (_, _) => InvokeSelectedImageContextAction(() => DeleteSelectedImage());
        menu.Items.Add(rotate);
        menu.Items.Add(replace);
        menu.Items.Add(extract);
        menu.Items.Add(new Separator());
        menu.Items.Add(delete);
        menu.Opened += SelectedImageContextMenu_Opened;

        _selectedImageContextMenu = menu;
        _selectedImageRotateContextMenuItem = rotate;
        _selectedImageReplaceContextMenuItem = replace;
        _selectedImageExtractContextMenuItem = extract;
        _selectedImageDeleteContextMenuItem = delete;
        return menu;
    }

    private bool CanUseSelectedImageContextAction(Polygon? outline)
        => outline is not null &&
           _editModeActive &&
           _session is not null &&
           ActiveDocument is not null &&
           ReferenceEquals(ActiveDocument.Session, _session) &&
           _zplDocument is null &&
           !_isBusy &&
           !_utilityExecuting &&
           !_homeActionStarting &&
           !_documentActivationInProgress &&
           !_editMaterializing &&
           !_commentMaterializing &&
           !_organizeMaterializing &&
           !_commentModeActive &&
           !_organizeModeActive &&
           !_signatureModeActive &&
           _acroFormPropertiesScrollViewer?.Visibility != Visibility.Visible &&
           ReferenceEquals(outline.ContextMenu, _selectedImageContextMenu) &&
           Equals(outline.Tag, "ImageMoveBody") &&
           _editOverlayCanvas?.Children.Contains(outline) == true &&
           TryGetSelectedImageState(out _);

    private void SelectedImageContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not Polygon outline || !CanUseSelectedImageContextAction(outline))
        {
            e.Handled = true;
            return;
        }

        SetSelectedImageContextMenuItemsEnabled(true);
    }

    private void SelectedImageContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        UpdateSelectedImageContextMenuState();
        if (_selectedImageRotateContextMenuItem?.IsEnabled != true && sender is ContextMenu menu)
            menu.IsOpen = false;
    }

    private void UpdateSelectedImageContextMenuState()
    {
        var canUse = _selectedImageContextMenu?.PlacementTarget is Polygon outline &&
                     CanUseSelectedImageContextAction(outline);
        SetSelectedImageContextMenuItemsEnabled(canUse);
    }

    private void SetSelectedImageContextMenuItemsEnabled(bool enabled)
    {
        if (_selectedImageRotateContextMenuItem is not null)
            _selectedImageRotateContextMenuItem.IsEnabled = enabled;
        if (_selectedImageReplaceContextMenuItem is not null)
            _selectedImageReplaceContextMenuItem.IsEnabled = enabled;
        if (_selectedImageExtractContextMenuItem is not null)
            _selectedImageExtractContextMenuItem.IsEnabled = enabled;
        if (_selectedImageDeleteContextMenuItem is not null)
            _selectedImageDeleteContextMenuItem.IsEnabled = enabled;
    }

    private void InvokeSelectedImageContextAction(Action action)
    {
        if (_selectedImageContextMenu?.PlacementTarget is not Polygon outline ||
            !CanUseSelectedImageContextAction(outline))
        {
            return;
        }

        action();
    }
}
