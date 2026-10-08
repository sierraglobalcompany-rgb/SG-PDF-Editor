using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SGPdf.App.Features.Sign;
using SGPdf.App.Pdf;

namespace SGPdf.App;

internal enum PendingSignatureDecision
{
    Save,
    Discard,
    Cancel
}

internal enum SignatureGuardReason
{
    PageNavigation,
    OpenPdf,
    OpenZpl,
    LeaveSignMode,
    WindowClosing
}

public partial class MainWindow
{
    private SignatureEditState? _signatureEditState;
    private PdfPageDeviceTransform? _currentPdfDeviceTransform;
    private bool _signatureModeActive;
    private bool _signatureUiInitialized;
    private Button? _readModeButton;
    private Button? _signModeButton;
    private Canvas? _signatureOverlayCanvas;
    private StackPanel? _signaturePropertiesPanel;

    private Func<string, SignatureAsset> _loadSignaturePng = SignaturePngLoader.Load;
    private Func<Window, string, SignatureAsset?> _prepareSignaturePhoto =
        static (owner, path) => SignaturePhotoDialog.Prepare(owner, path);
    private Func<Window, SignatureAsset?> _drawSignature =
        static owner => SignatureDrawDialog.Draw(owner);
    private Action<string, string, IReadOnlyList<SignaturePlacement>, CancellationToken> _saveVisualSignatures =
        static (source, destination, placements, token) =>
            new PdfVisualSignatureWriter().SaveAsCopy(source, destination, placements, token);
    private Func<PdfDocumentSession, int> _getCryptographicSignatureCount =
        static session => session.GetCryptographicSignatureCount();
    private Func<int, bool> _confirmSignedPdfModification = static count =>
        MessageBox.Show(
            $"Este PDF contiene {count} firma(s) criptográfica(s). Modificarlo y guardar una copia puede invalidarlas.\n\n¿Deseas continuar?",
            "SG PDF Editor",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
    private Func<SignatureGuardReason, PendingSignatureDecision> _resolvePendingSignatureDecision = static _ =>
    {
        var result = MessageBox.Show(
            "Hay firmas visuales sin guardar.\n\nSí: Guardar como...\nNo: Descartar cambios\nCancelar: permanecer aquí",
            "SG PDF Editor",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);
        return result switch
        {
            MessageBoxResult.Yes => PendingSignatureDecision.Save,
            MessageBoxResult.No => PendingSignatureDecision.Discard,
            _ => PendingSignatureDecision.Cancel
        };
    };
    private Func<string?> _selectSignaturePdfDestination = static () =>
    {
        var dialog = new SaveFileDialog
        {
            Title = "Guardar PDF firmado como",
            Filter = "Archivo PDF (*.pdf)|*.pdf",
            AddExtension = true,
            DefaultExt = ".pdf",
            OverwritePrompt = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    };

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        InitializeSignatureUi();
        Closing += Window_Closing;
    }

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        if (_signatureEditState?.IsDirty == true)
        {
            var source = e.OriginalSource as DependencyObject;
            if (IsInside(source, PreviousPageButton) || IsInside(source, NextPageButton))
            {
                if (!TryResolvePendingSignatureEdits(SignatureGuardReason.PageNavigation))
                {
                    e.Handled = true;
                    return;
                }
            }
            else if (IsInside(source, OpenPdfMenuItem))
            {
                if (!TryResolvePendingSignatureEdits(SignatureGuardReason.OpenPdf))
                {
                    e.Handled = true;
                    return;
                }
            }
            else if (IsInside(source, OpenZplMenuItem))
            {
                if (!TryResolvePendingSignatureEdits(SignatureGuardReason.OpenZpl))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        base.OnPreviewMouseDown(e);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (_signatureEditState?.IsDirty == true && e.Key == Key.Enter && ReferenceEquals(e.OriginalSource, PageNumberTextBox))
        {
            if (!TryResolvePendingSignatureEdits(SignatureGuardReason.PageNavigation))
            {
                e.Handled = true;
                return;
            }
        }

        if (_signatureModeActive && e.Key == Key.Delete && _signatureEditState?.DeleteSelected() == true)
        {
            RefreshSignatureOverlay();
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    private static bool IsInside(DependencyObject? source, DependencyObject target)
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, target))
                return true;
        }
        return false;
    }

    private void InitializeSignatureUi()
    {
        if (_signatureUiInitialized)
            return;

        _signatureUiInitialized = true;
        var root = (DockPanel)Content;

        var modePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(8, 5, 8, 5)
        };
        _readModeButton = new Button { Name = "ReadModeButton", Content = "LEER", Padding = new Thickness(14, 4, 14, 4), Margin = new Thickness(0, 0, 4, 0) };
        _signModeButton = new Button { Name = "SignModeButton", Content = "FIRMAR", Padding = new Thickness(14, 4, 14, 4), IsEnabled = false };
        RegisterName(_readModeButton.Name, _readModeButton);
        RegisterName(_signModeButton.Name, _signModeButton);
        _signModeButton.SetBinding(IsEnabledProperty, new Binding(nameof(IsEnabled)) { Source = PrintPdfMenuItem });
        _readModeButton.Click += ReadMode_Click;
        _signModeButton.Click += SignMode_Click;
        modePanel.Children.Add(_readModeButton);
        modePanel.Children.Add(_signModeButton);
        var modeBorder = new Border { BorderThickness = new Thickness(0, 0, 0, 1), BorderBrush = Brushes.LightGray, Background = Brushes.White, Child = modePanel };
        DockPanel.SetDock(modeBorder, Dock.Top);
        root.Children.Insert(Math.Min(1, root.Children.Count), modeBorder);

        var pageGrid = (Grid)PdfImage.Parent;
        _signatureOverlayCanvas = new Canvas
        {
            Name = "SignatureOverlayCanvas",
            Margin = PdfImage.Margin,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Brushes.Transparent,
            Visibility = Visibility.Collapsed
        };
        RegisterName(_signatureOverlayCanvas.Name, _signatureOverlayCanvas);
        pageGrid.Children.Add(_signatureOverlayCanvas);

        var propertyScroll = (ScrollViewer)LabelPropertiesPanel.Parent;
        var propertyGrid = (Grid)propertyScroll.Parent;
        _signaturePropertiesPanel = BuildSignaturePropertiesPanel();
        RegisterName(_signaturePropertiesPanel.Name, _signaturePropertiesPanel);
        propertyGrid.Children.Add(_signaturePropertiesPanel);
    }

    private StackPanel BuildSignaturePropertiesPanel()
    {
        var panel = new StackPanel { Name = "SignaturePropertiesPanel", Margin = new Thickness(16), Visibility = Visibility.Collapsed };
        panel.Children.Add(new TextBlock { Text = "Firma visual", FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 14) });

        var load = new Button { Content = "Cargar PNG transparente...", Padding = new Thickness(10, 5, 10, 5), HorizontalAlignment = HorizontalAlignment.Left };
        load.Click += LoadSignature_Click;
        panel.Children.Add(load);

        var photo = new Button
        {
            Name = "CreateSignatureFromPhotoButton",
            Content = "Crear desde foto...",
            Padding = new Thickness(10, 5, 10, 5),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 0)
        };
        RegisterName(photo.Name, photo);
        photo.Click += PrepareSignaturePhoto_Click;
        panel.Children.Add(photo);

        var draw = new Button
        {
            Name = "DrawSignatureButton",
            Content = "Dibujar firma...",
            Padding = new Thickness(10, 5, 10, 5),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 0)
        };
        RegisterName(draw.Name, draw);
        draw.Click += DrawSignature_Click;
        panel.Children.Add(draw);

        var duplicate = new Button { Content = "Duplicar", Padding = new Thickness(10, 5, 10, 5), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
        duplicate.Click += (_, _) => { if (_signatureEditState?.DuplicateSelected() is not null) RefreshSignatureOverlay(); };
        panel.Children.Add(duplicate);

        var delete = new Button { Content = "Eliminar", Padding = new Thickness(10, 5, 10, 5), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
        delete.Click += (_, _) => { if (_signatureEditState?.DeleteSelected() == true) RefreshSignatureOverlay(); };
        panel.Children.Add(delete);

        var save = new Button { Content = "Guardar como...", Padding = new Thickness(10, 5, 10, 5), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 14, 0, 0) };
        save.Click += (_, _) => TrySavePendingVisualSignatures();
        panel.Children.Add(save);

        panel.Children.Add(new TextBlock
        {
            Text = "Puedes cargar un PNG transparente, preparar una firma desde una foto/escaneo o dibujarla directamente.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 14, 0, 0)
        });
        return panel;
    }

    private void SignMode_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null || _navigation is null || _isBusy)
            return;

        EnsureCurrentDeviceTransform();
        _signatureModeActive = true;
        _signatureOverlayCanvas!.Visibility = Visibility.Visible;
        _signaturePropertiesPanel!.Visibility = Visibility.Visible;
        LabelPropertiesPanel.Visibility = Visibility.Collapsed;
        PropertiesPlaceholderText.Visibility = Visibility.Collapsed;
        RefreshSignatureOverlay();
    }

    private void ReadMode_Click(object sender, RoutedEventArgs e)
    {
        if (_signatureEditState?.IsDirty == true && !TryResolvePendingSignatureEdits(SignatureGuardReason.LeaveSignMode))
            return;

        _signatureModeActive = false;
        if (_signatureOverlayCanvas is not null)
            _signatureOverlayCanvas.Visibility = Visibility.Collapsed;
        if (_signaturePropertiesPanel is not null)
            _signaturePropertiesPanel.Visibility = Visibility.Collapsed;
        PropertiesPlaceholderText.Visibility = _zplDocument is null ? Visibility.Visible : Visibility.Collapsed;
        if (_zplDocument is not null)
            LabelPropertiesPanel.Visibility = Visibility.Visible;
    }

    private void EnsureCurrentDeviceTransform()
    {
        if (_currentPdfDeviceTransform is not null || _session is null || _navigation is null)
            return;

        var source = PdfImage.Source as BitmapSource;
        if (source is null)
            throw new InvalidOperationException("No hay una página renderizada para firmar.");

        var page = _session.GetPageSize(_navigation.CurrentPageIndex);
        var width = Math.Max(1, source.PixelWidth);
        var height = Math.Max(1, source.PixelHeight);
        _currentPdfDeviceTransform = new PdfPageDeviceTransform(
            0d,
            page.Height,
            page.Width / width,
            0d,
            0d,
            -page.Height / height,
            width,
            height);
    }

    private void AddSignatureAsset(SignatureAsset asset)
    {
        if (_session is null || _navigation is null)
            throw new InvalidOperationException("Abre un PDF antes de agregar una firma.");

        EnsureCurrentDeviceTransform();
        var transform = _currentPdfDeviceTransform ?? throw new InvalidOperationException("No hay geometría de página disponible.");
        var pageBounds = SignatureCoordinateMapper.GetVisiblePdfBounds(transform);

        if (_signatureEditState is null || _signatureEditState.PageIndex != _navigation.CurrentPageIndex)
            _signatureEditState = new SignatureEditState(_navigation.CurrentPageIndex, pageBounds);

        _signatureEditState.AddCentered(asset);
        RefreshSignatureOverlay();
    }

    private void LoadSignature_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Cargar firma transparente",
            Filter = "Imagen PNG (*.png)|*.png",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) == true)
            TryLoadSignatureFromPath(dialog.FileName);
    }

    private void PrepareSignaturePhoto_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Crear firma desde foto o escaneo",
            Filter = "Imágenes compatibles (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|PNG (*.png)|*.png|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) == true)
            TryPrepareSignaturePhotoFromPath(dialog.FileName);
    }

    private void DrawSignature_Click(object sender, RoutedEventArgs e)
        => TryDrawSignature();

    private bool TryLoadSignatureFromPath(string path)
    {
        try
        {
            var asset = _loadSignaturePng(path);
            AddSignatureAsset(asset);
            StatusText.Text = "Firma agregada. Arrástrala o redimensiónala y luego usa Guardar como...";
            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text = "No se pudo cargar la firma.";
            ShowSignatureMessage($"No se pudo cargar la firma PNG.\n\n{ex.Message}", MessageBoxImage.Error);
            return false;
        }
    }

    private bool TryPrepareSignaturePhotoFromPath(string path)
    {
        try
        {
            var asset = _prepareSignaturePhoto(this, path);
            if (asset is null)
                return false;

            AddSignatureAsset(asset);
            StatusText.Text = "Firma preparada y agregada. Arrástrala o redimensiónala y luego usa Guardar como...";
            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text = "No se pudo preparar la firma desde la foto.";
            ShowSignatureMessage($"No se pudo preparar la firma desde la foto o escaneo.\n\n{ex.Message}", MessageBoxImage.Error);
            return false;
        }
    }

    private bool TryDrawSignature()
    {
        try
        {
            var asset = _drawSignature(this);
            if (asset is null)
                return false;

            AddSignatureAsset(asset);
            StatusText.Text = "Firma dibujada y agregada. Arrástrala o redimensiónala y luego usa Guardar como...";
            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text = "No se pudo dibujar la firma.";
            ShowSignatureMessage($"No se pudo preparar la firma dibujada.\n\n{ex.Message}", MessageBoxImage.Error);
            return false;
        }
    }

    private void RefreshSignatureOverlay()
    {
        if (_signatureOverlayCanvas is null)
            return;

        _signatureOverlayCanvas.Children.Clear();
        if (_signatureEditState is null || _currentPdfDeviceTransform is not PdfPageDeviceTransform transform)
            return;

        _signatureOverlayCanvas.Width = transform.DeviceWidth;
        _signatureOverlayCanvas.Height = transform.DeviceHeight;

        foreach (var placement in _signatureEditState.Placements)
        {
            var rect = SignatureCoordinateMapper.PdfRectToDeviceRect(placement.Bounds, transform);
            var border = BuildPlacementVisual(placement, rect);
            Canvas.SetLeft(border, rect.X);
            Canvas.SetTop(border, rect.Y);
            _signatureOverlayCanvas.Children.Add(border);
        }
    }

    private Border BuildPlacementVisual(SignaturePlacement placement, SignatureDeviceRect rect)
    {
        var bitmap = BitmapSource.Create(
            placement.Asset.PixelWidth,
            placement.Asset.PixelHeight,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            placement.Asset.BgraPixels.ToArray(),
            placement.Asset.Stride);
        bitmap.Freeze();

        var grid = new Grid();
        grid.Children.Add(new Image { Source = bitmap, Stretch = Stretch.Fill, IsHitTestVisible = false });
        var border = new Border
        {
            Width = rect.Width,
            Height = rect.Height,
            BorderThickness = new Thickness(_signatureEditState?.SelectedId == placement.Id ? 1.5 : 0),
            BorderBrush = Brushes.DodgerBlue,
            Background = Brushes.Transparent,
            Child = grid,
            Tag = placement.Id,
            Cursor = Cursors.SizeAll
        };

        var handle = new Thumb
        {
            Width = 12,
            Height = 12,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Cursor = Cursors.SizeNWSE,
            Background = Brushes.DodgerBlue
        };
        handle.DragDelta += (_, args) => ResizePlacementFromDeviceDelta(placement.Id, args.HorizontalChange);
        grid.Children.Add(handle);

        Point start = default;
        SignatureDeviceRect startRect = default;
        var dragging = false;
        border.MouseLeftButtonDown += (_, args) =>
        {
            _signatureEditState?.Select(placement.Id);
            RefreshSignatureOverlay();
            var refreshed = _signatureOverlayCanvas!.Children.OfType<Border>().FirstOrDefault(item => Equals(item.Tag, placement.Id));
            if (refreshed is null)
                return;
            start = args.GetPosition(_signatureOverlayCanvas);
            startRect = SignatureCoordinateMapper.PdfRectToDeviceRect(placement.Bounds, _currentPdfDeviceTransform!.Value);
            dragging = true;
            refreshed.CaptureMouse();
            args.Handled = true;
        };
        border.MouseMove += (_, args) =>
        {
            if (!dragging || args.LeftButton != MouseButtonState.Pressed || _signatureEditState is null || _currentPdfDeviceTransform is null)
                return;
            var point = args.GetPosition(_signatureOverlayCanvas);
            var proposed = startRect with { X = startRect.X + point.X - start.X, Y = startRect.Y + point.Y - start.Y };
            _signatureEditState.Select(placement.Id);
            _signatureEditState.SetSelectedBounds(SignatureCoordinateMapper.DeviceRectToPdfRect(proposed, _currentPdfDeviceTransform.Value));
            RefreshSignatureOverlay();
        };
        border.MouseLeftButtonUp += (_, args) =>
        {
            dragging = false;
            Mouse.Capture(null);
            args.Handled = true;
        };
        return border;
    }

    private void ResizePlacementFromDeviceDelta(Guid id, double horizontalChange)
    {
        if (_signatureEditState is null || _currentPdfDeviceTransform is null)
            return;
        var placement = _signatureEditState.Placements.FirstOrDefault(item => item.Id == id);
        if (placement is null)
            return;
        _signatureEditState.Select(id);
        var device = SignatureCoordinateMapper.PdfRectToDeviceRect(placement.Bounds, _currentPdfDeviceTransform.Value);
        var width = Math.Max(1d, device.Width + horizontalChange);
        var height = width / placement.Asset.AspectRatio;
        var proposed = new SignatureDeviceRect(device.X, device.Y, width, height);
        _signatureEditState.SetSelectedBounds(SignatureCoordinateMapper.DeviceRectToPdfRect(proposed, _currentPdfDeviceTransform.Value));
        RefreshSignatureOverlay();
    }

    private bool TrySavePendingVisualSignatures()
    {
        if (_session is null || _signatureEditState is null || !_signatureEditState.IsDirty || _signatureEditState.Placements.Count == 0)
            return true;

        var destination = _selectSignaturePdfDestination();
        if (string.IsNullOrWhiteSpace(destination))
            return false;

        try
        {
            var source = Path.GetFullPath(_session.FilePath);
            var target = Path.GetFullPath(destination);
            if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
            {
                ShowSignatureMessage("F3.1 protege el original. Elige un nombre o ubicación diferente.", MessageBoxImage.Warning);
                return false;
            }

            int signatureCount;
            try
            {
                signatureCount = _getCryptographicSignatureCount(_session);
            }
            catch (Exception ex)
            {
                StatusText.Text = "No se pudo verificar si el PDF contiene firmas criptográficas.";
                ShowSignatureMessage($"No se guardó la copia porque no se pudo evaluar la seguridad de firmas existentes.\n\n{ex.Message}", MessageBoxImage.Warning);
                return false;
            }

            if (signatureCount > 0 && !_confirmSignedPdfModification(signatureCount))
                return false;

            SetBusy(true);
            StatusText.Text = "Guardando copia firmada...";
            _saveVisualSignatures(source, target, _signatureEditState.Placements.ToArray(), CancellationToken.None);
            _signatureEditState.MarkSaved();
            StatusText.Text = $"Copia firmada guardada: {Path.GetFileName(target)}";
            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text = "No se pudo guardar la copia firmada.";
            ShowSignatureMessage($"No se pudo guardar el PDF firmado.\n\n{ex.Message}", MessageBoxImage.Error);
            return false;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowSignatureMessage(string message, MessageBoxImage image)
    {
        if (IsVisible)
            MessageBox.Show(this, message, "SG PDF Editor", MessageBoxButton.OK, image);
    }

    private bool TryResolvePendingSignatureEdits(SignatureGuardReason reason)
    {
        if (_signatureEditState?.IsDirty != true)
            return true;

        return _resolvePendingSignatureDecision(reason) switch
        {
            PendingSignatureDecision.Cancel => false,
            PendingSignatureDecision.Discard => DiscardPendingSignatures(),
            PendingSignatureDecision.Save => TrySavePendingVisualSignatures(),
            _ => false
        };
    }

    private bool DiscardPendingSignatures()
    {
        _signatureEditState?.DiscardAll();
        RefreshSignatureOverlay();
        return true;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!TryResolvePendingSignatureEdits(SignatureGuardReason.WindowClosing))
            e.Cancel = true;
    }
}
