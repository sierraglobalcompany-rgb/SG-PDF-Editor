using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using SGPdf.App.Features.Reader;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool ReaderShortcutLoadedHandlerRegistered = RegisterReaderShortcutLoadedHandler();

    private RecentPdfStore _recentPdfStore = new();
    private bool _readerShortcutUiInitialized;
    private Action? _openPdfShortcutAction;
    private Action? _printPdfShortcutAction;

    private static bool RegisterReaderShortcutLoadedHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(ReaderShortcutHost_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void ReaderShortcutHost_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializeReaderShortcutUi();
    }

    private void InitializeReaderShortcutUi()
    {
        _ = ReaderShortcutLoadedHandlerRegistered;
        if (_readerShortcutUiInitialized)
            return;

        _readerShortcutUiInitialized = true;
        PreviewKeyDown += ReaderShortcutWindow_PreviewKeyDown;
    }

    private void ReaderShortcutWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (TryHandleReaderShortcut(e.Key, Keyboard.Modifiers, e.OriginalSource as DependencyObject))
            e.Handled = true;
    }

    private bool TryHandleReaderShortcut(Key key, ModifierKeys modifiers, DependencyObject? source)
    {
        if (IsReaderShortcutEditableSource(source))
            return false;

        var hasControl = (modifiers & ModifierKeys.Control) != 0;
        if (hasControl && key == Key.O)
        {
            if (_isBusy)
                return true;
            if (_signatureEditState?.IsDirty == true &&
                !TryResolvePendingSignatureEdits(SignatureGuardReason.OpenPdf))
            {
                return true;
            }

            if (_openPdfShortcutAction is not null)
                _openPdfShortcutAction();
            else
                OpenPdf_Click(OpenPdfMenuItem, new RoutedEventArgs(MenuItem.ClickEvent));
            return true;
        }

        if (hasControl && key == Key.P)
        {
            if (_session is null || _navigation is null || _isBusy || _zplDocument is not null)
                return false;

            if (_printPdfShortcutAction is not null)
                _printPdfShortcutAction();
            else
                PrintPdf_Click(PrintPdfMenuItem, new RoutedEventArgs(MenuItem.ClickEvent));
            return true;
        }

        if (hasControl && key is Key.OemPlus or Key.Add)
        {
            if (_session is null || _navigation is null || _isBusy || _zplDocument is not null)
                return false;
            _ = ApplyZoomAsync((zoom, dpi) => zoom.ZoomIn(dpi));
            return true;
        }

        if (hasControl && key is Key.OemMinus or Key.Subtract)
        {
            if (_session is null || _navigation is null || _isBusy || _zplDocument is not null)
                return false;
            _ = ApplyZoomAsync((zoom, dpi) => zoom.ZoomOut(dpi));
            return true;
        }

        if (hasControl && key is Key.D0 or Key.NumPad0)
        {
            if (_session is null || _navigation is null || _isBusy || _zplDocument is not null)
                return false;
            _ = ApplyZoomAsync((zoom, _) => zoom.ActualSize());
            return true;
        }

        if (modifiers != ModifierKeys.None || !IsContinuousReaderActive())
            return false;

        if (key == Key.Home)
        {
            ScrollReaderToPage(0);
            _ = RefreshReaderRenderWindowAsync();
            return true;
        }

        if (key == Key.End)
        {
            ScrollReaderToPage(_readerGeometry.Count - 1);
            _ = RefreshReaderRenderWindowAsync();
            return true;
        }

        if (key == Key.PageUp)
        {
            ScrollReaderByViewport(-1);
            return true;
        }

        if (key == Key.PageDown)
        {
            ScrollReaderByViewport(1);
            return true;
        }

        return false;
    }

    private void ScrollReaderByViewport(int direction)
    {
        if (direction == 0 || _readerGeometry.Count == 0)
            return;

        var viewportHeight = GetReaderViewportSize().Height;
        var last = _readerGeometry[^1];
        var contentHeight = last.Top + last.DisplayHeight;
        var maximumOffset = Math.Max(0d, contentHeight - viewportHeight);
        _readerVerticalOffset = Math.Clamp(
            _readerVerticalOffset + (direction * viewportHeight),
            0d,
            maximumOffset);
        ScrollInternalReader(_readerVerticalOffset);
        UpdateReaderCurrentPageFromViewport();
        _ = RefreshReaderRenderWindowAsync();
    }

    private static bool IsReaderShortcutEditableSource(DependencyObject? source)
    {
        for (var current = source; current is not null; current = GetReaderShortcutParent(current))
        {
            if (current is TextBoxBase or PasswordBox)
                return true;
            if (current is ComboBox { IsEditable: true })
                return true;
        }

        return false;
    }

    private static DependencyObject? GetReaderShortcutParent(DependencyObject current)
    {
        if (current is Visual or Visual3D)
        {
            var parent = VisualTreeHelper.GetParent(current);
            if (parent is not null)
                return parent;
        }

        return LogicalTreeHelper.GetParent(current);
    }

    private Task<bool> HandleReaderSearchKeyAsync(Key key, ModifierKeys modifiers, DependencyObject? source)
    {
        if (IsReaderShortcutEditableSource(source) &&
            (_readerFindTextBox is null || !IsInside(source, _readerFindTextBox)))
        {
            return Task.FromResult(false);
        }

        return HandleReaderSearchKeyAsync(key, modifiers);
    }

    private bool HandleReaderSelectionKey(Key key, ModifierKeys modifiers, DependencyObject? source)
        => !IsReaderShortcutEditableSource(source) && HandleReaderSelectionKey(key, modifiers);

    private void RecentFilesMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        => RebuildRecentFilesMenu();

    private void RebuildRecentFilesMenu()
    {
        var entries = _recentPdfStore.Load();
        RecentFilesMenuItem.Items.Clear();

        if (entries.Count == 0)
        {
            RecentFilesMenuItem.Items.Add(new MenuItem
            {
                Header = "(Sin archivos recientes)",
                IsEnabled = false
            });
        }
        else
        {
            foreach (var entry in entries)
            {
                var item = new MenuItem
                {
                    Header = Path.GetFileName(entry.FullPath),
                    ToolTip = entry.FullPath,
                    Tag = entry.FullPath
                };
                item.Click += RecentFileMenuItem_Click;
                RecentFilesMenuItem.Items.Add(item);
            }

            RecentFilesMenuItem.Items.Add(new Separator());
        }

        ClearRecentFilesMenuItem.IsEnabled = entries.Count > 0;
        RecentFilesMenuItem.Items.Add(ClearRecentFilesMenuItem);
    }

    private async void RecentFileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string path })
            return;

        var opened = await TryOpenPdfPathAsync(path);
        if (!opened)
        {
            try
            {
                if (!File.Exists(path))
                    _recentPdfStore.Remove(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                // The user explicitly selected the path. A failed cleanup must never disturb the open document.
            }
        }

        RebuildRecentFilesMenu();
    }

    private void ClearRecentFilesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _recentPdfStore.Clear();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = "No se pudo limpiar la lista de archivos recientes.";
        }

        RebuildRecentFilesMenu();
    }

    private void TryRecordRecentPdf(string path)
    {
        try
        {
            _recentPdfStore.RecordSuccessfulOpen(path, DateTimeOffset.UtcNow);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            // Recents are optional metadata. A successful PDF open remains successful if persistence fails.
        }
    }
}
