using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool ImageEditLoadedHookRegistered = RegisterImageEditLoadedHook();

    private bool _imageEditUiInitialized;
    private bool _imageEditModeActive;
    private Button? _imageEditModeButton;
    private Canvas? _imageEditOverlayCanvas;
    private ImageEditWorkspace? _imageEditWorkspace;
    private IReadOnlyList<PdfImageObjectInfo> _activeImageObjects = Array.Empty<PdfImageObjectInfo>();
    private ImageObjectKey? _selectedImageKey;

    private Func<PdfDocumentSession, int, CancellationToken, IReadOnlyList<PdfImageObjectInfo>> _getImageObjects =
        static (session, pageIndex, token) => session.GetImageObjects(pageIndex, token);

    private static bool RegisterImageEditLoadedHook()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(ImageEditHost_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void ImageEditHost_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializeImageEditUi();
    }

    private void InitializeImageEditUi()
    {
        if (_imageEditUiInitialized)
            return;

        _imageEditUiInitialized = true;
        _ = ImageEditLoadedHookRegistered;

        InitializeOrganizeUi();
        var modePanel = FindModePanel();
        var editModeButton = new Button
        {
            Name = "EditModeButton",
            Content = "EDITAR",
            Padding = new Thickness(14, 4, 14, 4),
            Margin = new Thickness(4, 0, 0, 0),
            IsEnabled = false
        };
        editModeButton.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(IsEnabled)) { Source = PrintPdfMenuItem });
        editModeButton.Click += ImageEditMode_Click;

        var organizeIndex = _organizeModeButton is null
            ? -1
            : modePanel.Children.IndexOf(_organizeModeButton);
        if (organizeIndex >= 0)
            modePanel.Children.Insert(organizeIndex, editModeButton);
        else
            modePanel.Children.Add(editModeButton);

        RegisterName(editModeButton.Name, editModeButton);
        _imageEditModeButton = editModeButton;

        var pageGrid = PdfImage.Parent as Grid
            ?? throw new InvalidOperationException("No se encontró el contenedor de página para EDITAR.");
        var overlay = new Canvas
        {
            Name = "ImageEditOverlayCanvas",
            Margin = PdfImage.Margin,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Brushes.Transparent,
            Visibility = Visibility.Collapsed
        };
        overlay.MouseLeftButtonDown += ImageEditOverlay_MouseLeftButtonDown;
        overlay.MouseMove += ImageEditOverlay_MouseMove;
        overlay.MouseLeftButtonUp += ImageEditOverlay_MouseLeftButtonUp;
        Panel.SetZIndex(overlay, 40);
        pageGrid.Children.Add(overlay);
        RegisterName(overlay.Name, overlay);
        _imageEditOverlayCanvas = overlay;

        if (_readModeButton is not null)
            _readModeButton.Click += ExistingModeAfterImageEdit_Click;
        if (_signModeButton is not null)
            _signModeButton.Click += ExistingModeAfterImageEdit_Click;
        if (_organizeModeButton is not null)
            _organizeModeButton.Click += ExistingModeAfterImageEdit_Click;

        PreviewKeyDown += ImageEditHost_PreviewKeyDown;
    }

    private async void ImageEditMode_Click(object sender, RoutedEventArgs e)
        => await TryEnterImageEditModeAsync();

    private async Task<bool> TryEnterImageEditModeAsync()
    {
        InitializeImageEditUi();
        if (_session is null || _navigation is null || _isBusy)
            return false;
        if (_imageEditModeActive)
            return true;

        if (_organizeModeActive && !TryLeaveOrganizeModeWithGuard())
            return false;

        if (_signatureEditState?.IsDirty == true &&
            !TryResolvePendingSignatureEdits(SignatureGuardReason.LeaveSignMode))
        {
            return false;
        }

        var sourceSession = _session;
        if (sourceSession.OpenedWithPassword)
        {
            ShowImageEditBlock("Este PDF se abrió con contraseña y no se puede editar de forma segura en esta versión.");
            return false;
        }

        int signatureCount;
        try
        {
            signatureCount = _getCryptographicSignatureCount(sourceSession);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo comprobar si el PDF se puede editar: {ex.Message}";
            return false;
        }

        if (signatureCount > 0)
        {
            ShowImageEditBlock("Este PDF contiene una firma criptográfica y no se puede editar de forma segura en esta versión.");
            return false;
        }

        var pageIndex = _navigation.CurrentPageIndex;
        try
        {
            await EnsureEditSurfaceForCurrentPageAsync();
            if (!ReferenceEquals(_session, sourceSession) ||
                _navigation?.CurrentPageIndex != pageIndex ||
                _currentPdfDeviceTransform is null)
            {
                return false;
            }

            var images = await Task.Run(() =>
                _getImageObjects(sourceSession, pageIndex, CancellationToken.None));
            if (!ReferenceEquals(_session, sourceSession) || _navigation?.CurrentPageIndex != pageIndex)
                return false;

            var workspace = ImageEditWorkspace.Create(
                sourceSession.FilePath,
                sourceSession.OpenedWithPassword);
            foreach (var image in images)
                workspace.EnsureObject(image);

            _imageEditWorkspace = workspace;
            _activeImageObjects = images.ToArray();
            _selectedImageKey = null;
            _imageEditModeActive = true;
            _signatureModeActive = false;

            if (_signatureOverlayCanvas is not null)
                _signatureOverlayCanvas.Visibility = Visibility.Collapsed;
            if (_signaturePropertiesPanel is not null)
                _signaturePropertiesPanel.Visibility = Visibility.Collapsed;
            if (_organizeSurface is not null)
                _organizeSurface.Visibility = Visibility.Collapsed;
            LabelPropertiesPanel.Visibility = Visibility.Collapsed;
            PropertiesPlaceholderText.Visibility = Visibility.Visible;
            LabelSheetCanvas.Visibility = Visibility.Collapsed;
            LabelNavigationBar.Visibility = Visibility.Collapsed;

            _imageEditOverlayCanvas!.Visibility = Visibility.Visible;
            RefreshImageEditOverlay();
            StatusText.Text = images.Count == 0
                ? "EDITAR activo. No se encontraron imágenes editables en esta página."
                : $"EDITAR activo. {images.Count} imagen(es) editable(s) en esta página.";
            return true;
        }
        catch (Exception ex)
        {
            ResetImageEditState();
            StatusText.Text = $"No se pudo iniciar EDITAR: {ex.Message}";
            ShowContinuousReaderSurface();
            return false;
        }
    }

    private void ShowImageEditBlock(string message)
    {
        StatusText.Text = message;
        if (IsVisible)
        {
            MessageBox.Show(
                this,
                message,
                "SG PDF Editor",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private bool SelectImageAtDevicePoint(double deviceX, double deviceY)
    {
        if (!_imageEditModeActive || _currentPdfDeviceTransform is not PdfPageDeviceTransform transform)
        {
            _selectedImageKey = null;
            RefreshImageEditOverlay();
            return false;
        }

        var pdfPoint = ImageHitTester.DeviceToPdf(deviceX, deviceY, transform);
        _selectedImageKey = ImageHitTester.HitTest(CurrentSelectableImageObjects(), pdfPoint);
        CancelImageEditGesture();
        RefreshImageEditOverlay();
        return _selectedImageKey is not null;
    }

    private void HandleImageEditEscape()
    {
        if (!_imageEditModeActive)
            return;

        CancelImageEditGesture();
        _selectedImageKey = null;
        RefreshImageEditOverlay();
    }

    private void RefreshImageEditOverlay()
    {
        if (_imageEditOverlayCanvas is null)
            return;

        _imageEditOverlayCanvas.Children.Clear();
        if (!_imageEditModeActive ||
            _imageEditWorkspace is null ||
            _currentPdfDeviceTransform is not PdfPageDeviceTransform transform)
        {
            return;
        }

        _imageEditOverlayCanvas.Width = transform.DeviceWidth;
        _imageEditOverlayCanvas.Height = transform.DeviceHeight;
        if (_selectedImageKey is not ImageObjectKey selectedKey)
            return;

        ImageEditState state;
        try
        {
            state = _imageEditWorkspace.GetState(selectedKey);
        }
        catch (KeyNotFoundException)
        {
            _selectedImageKey = null;
            return;
        }

        if (state.Deleted)
            return;

        var matrix = CurrentDisplayMatrix(state);
        var devicePoints = ImageHitTester.GetQuad(matrix)
            .Select(point => ImageHitTester.PdfToDevice(point, transform))
            .ToArray();

        var outline = new Polygon
        {
            Points = new PointCollection(devicePoints.Select(point => new Point(point.X, point.Y))),
            Fill = Brushes.Transparent,
            Stroke = Brushes.DodgerBlue,
            StrokeThickness = 1.5,
            Cursor = Cursors.SizeAll,
            Tag = "ImageMoveBody",
            IsHitTestVisible = true
        };
        _imageEditOverlayCanvas.Children.Add(outline);

        var corners = new[]
        {
            ImageResizeCorner.BottomLeft,
            ImageResizeCorner.BottomRight,
            ImageResizeCorner.TopRight,
            ImageResizeCorner.TopLeft
        };
        for (var index = 0; index < devicePoints.Length; index++)
        {
            var point = devicePoints[index];
            var handle = new Rectangle
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.White,
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 1.5,
                Cursor = Cursors.SizeNWSE,
                Tag = corners[index],
                IsHitTestVisible = true
            };
            Canvas.SetLeft(handle, point.X - 5d);
            Canvas.SetTop(handle, point.Y - 5d);
            _imageEditOverlayCanvas.Children.Add(handle);
        }

        var minX = devicePoints.Min(point => point.X);
        var minY = devicePoints.Min(point => point.Y);
        var toolbarY = Math.Max(0d, minY - 30d);

        var rotate = new Button
        {
            Content = "↻",
            ToolTip = "Rotar 90°",
            Padding = new Thickness(6, 2, 6, 2),
            Tag = "ImageRotateButton"
        };
        rotate.Click += (_, _) => RotateSelectedImage(90d);
        Canvas.SetLeft(rotate, minX);
        Canvas.SetTop(rotate, toolbarY);
        _imageEditOverlayCanvas.Children.Add(rotate);

        var delete = new Button
        {
            Content = "Eliminar",
            ToolTip = "Eliminar imagen",
            Padding = new Thickness(6, 2, 6, 2),
            Tag = "ImageDeleteButton"
        };
        delete.Click += (_, _) => DeleteSelectedImage();
        Canvas.SetLeft(delete, minX + 34d);
        Canvas.SetTop(delete, toolbarY);
        _imageEditOverlayCanvas.Children.Add(delete);

        var replace = new Button
        {
            Content = "Reemplazar...",
            ToolTip = "Reemplazar contenido de imagen",
            Padding = new Thickness(6, 2, 6, 2),
            Tag = "ImageReplaceButton"
        };
        replace.Click += (_, _) => TryReplaceSelectedImageFromDialog();
        Canvas.SetLeft(replace, minX + 100d);
        Canvas.SetTop(replace, toolbarY);
        _imageEditOverlayCanvas.Children.Add(replace);

        var extract = new Button
        {
            Content = "Extraer PNG",
            ToolTip = "Extraer imagen como PNG",
            Padding = new Thickness(6, 2, 6, 2),
            Tag = "ImageExtractButton"
        };
        extract.Click += (_, _) => TryExtractSelectedImageFromDialog();
        Canvas.SetLeft(extract, minX + 196d);
        Canvas.SetTop(extract, toolbarY);
        _imageEditOverlayCanvas.Children.Add(extract);
    }

    private void ImageEditOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_imageEditOverlayCanvas is null)
            return;

        var point = e.GetPosition(_imageEditOverlayCanvas);
        var tag = FindImageEditInteractionTag(e.OriginalSource as DependencyObject);
        if (tag is ImageResizeCorner corner)
        {
            if (BeginImageResize(corner, point.X, point.Y))
            {
                _imageEditOverlayCanvas.CaptureMouse();
                e.Handled = true;
            }
            return;
        }

        if (Equals(tag, "ImageMoveBody"))
        {
            if (BeginImageMoveAtDevicePoint(point.X, point.Y))
            {
                _imageEditOverlayCanvas.CaptureMouse();
                e.Handled = true;
            }
            return;
        }

        if (Equals(tag, "ImageRotateButton") ||
            Equals(tag, "ImageDeleteButton") ||
            Equals(tag, "ImageReplaceButton") ||
            Equals(tag, "ImageExtractButton"))
        {
            return;
        }

        SelectImageAtDevicePoint(point.X, point.Y);
        e.Handled = true;
    }

    private void ImageEditOverlay_MouseMove(object sender, MouseEventArgs e)
    {
        if (_imageEditOverlayCanvas is null ||
            !_imageEditOverlayCanvas.IsMouseCaptured ||
            e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var point = e.GetPosition(_imageEditOverlayCanvas);
        if (UpdateActiveImageGestureAtDevicePoint(
                point.X,
                point.Y,
                (Keyboard.Modifiers & ModifierKeys.Shift) != 0))
        {
            e.Handled = true;
        }
    }

    private void ImageEditOverlay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_imageEditOverlayCanvas?.IsMouseCaptured != true)
            return;

        var changed = CompleteActiveImageGesture();
        _imageEditOverlayCanvas.ReleaseMouseCapture();
        e.Handled = changed;
    }

    private object? FindImageEditInteractionTag(DependencyObject? source)
    {
        for (var current = source; current is not null && !ReferenceEquals(current, _imageEditOverlayCanvas);)
        {
            if (current is FrameworkElement { Tag: not null } element)
                return element.Tag;

            if (current is Visual visual)
            {
                var parent = VisualTreeHelper.GetParent(visual);
                if (parent is not null)
                {
                    current = parent;
                    continue;
                }
            }

            current = LogicalTreeHelper.GetParent(current);
        }

        return null;
    }

    private void ImageEditHost_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_imageEditModeActive || IsReaderShortcutEditableSource(e.OriginalSource as DependencyObject))
            return;

        if (e.Key == Key.Escape)
        {
            HandleImageEditEscape();
            e.Handled = true;
            return;
        }

        if (TryHandleImageEditShortcut(e.Key, Keyboard.Modifiers, e.OriginalSource as DependencyObject))
            e.Handled = true;
    }

    private void ExistingModeAfterImageEdit_Click(object sender, RoutedEventArgs e)
    {
        if (_imageEditModeActive)
            ResetImageEditState();
    }

    private void ResetImageEditState()
    {
        CancelImageEditGesture();
        _imageEditModeActive = false;
        _imageEditWorkspace = null;
        _activeImageObjects = Array.Empty<PdfImageObjectInfo>();
        _selectedImageKey = null;

        if (_imageEditOverlayCanvas is null)
            return;

        _imageEditOverlayCanvas.Children.Clear();
        _imageEditOverlayCanvas.Visibility = Visibility.Collapsed;
    }
}
