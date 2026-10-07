using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SGPdf.App.Features.Labels;

namespace SGPdf.App;

public partial class MainWindow
{
    private ZplDocument? _zplDocument;
    private IReadOnlyList<ZplRenderedLabel> _renderedZplLabels = Array.Empty<ZplRenderedLabel>();
    private int _selectedZplDesignIndex;
    private CancellationTokenSource? _labelRenderCts;
    private ZplQuantitySelection _zplQuantitySelection = new(ZplQuantityMode.FromFile);
    private ZplRenderOptions _zplRenderOptions = ZplRenderOptions.Default;
    private Func<ZplDocument, ZplRenderOptions, CancellationToken, Task<IReadOnlyList<ZplRenderedLabel>>> _renderZplAsync =
        static (document, options, cancellationToken) =>
            new LabelizeProcessRenderer().RenderAsync(document, options, cancellationToken);
    private bool _updatingLabelPropertiesUi;

    private async void OpenZpl_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Abrir etiquetas ZPL",
            Filter = "ZPL / TXT / PRN (*.zpl;*.txt;*.prn)|*.zpl;*.txt;*.prn|Todos los archivos (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
            return;

        SetBusy(true);
        StatusText.Text = "Abriendo archivo ZPL...";

        CancellationTokenSource? renderCts = null;
        CancellationToken renderToken = default;

        try
        {
            var candidate = await Task.Run(() => ZplFileLoader.Load(dialog.FileName));

            renderCts = new CancellationTokenSource();
            renderToken = renderCts.Token;
            _labelRenderCts = renderCts;

            StatusText.Text = "Renderizando etiquetas ZPL...";
            var rendered = await _renderZplAsync(
                candidate,
                _zplRenderOptions,
                renderToken);

            renderToken.ThrowIfCancellationRequested();
            if (!ReferenceEquals(_labelRenderCts, renderCts))
                return;

            CommitLoadedZpl(candidate, rendered);
        }
        catch (OperationCanceledException) when (renderToken.IsCancellationRequested)
        {
            // Closing the window cancels the active Labelize process. Keep the previous workspace intact.
        }
        catch (Exception ex)
        {
            StatusText.Text = "No se pudo abrir el archivo ZPL.";
            MessageBox.Show(
                this,
                $"No se pudo abrir, analizar o renderizar el archivo ZPL.\n\n{ex.Message}",
                "SG PDF Editor",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            if (renderCts is not null)
            {
                if (ReferenceEquals(_labelRenderCts, renderCts))
                    _labelRenderCts = null;
                renderCts.Dispose();
            }

            SetBusy(false);
        }
    }

    private void CommitLoadedZpl(
        ZplDocument document,
        IReadOnlyList<ZplRenderedLabel> renderedLabels)
    {
        ArgumentNullException.ThrowIfNull(document);
        ValidateRenderedLabels(document, renderedLabels);

        _resizeRenderScheduler.CancelCurrent();

        var previousSession = _session;
        _session = null;
        _navigation = null;
        _zplDocument = document;
        _renderedZplLabels = renderedLabels.ToArray();
        _selectedZplDesignIndex = 0;
        _zplQuantitySelection = new ZplQuantitySelection(ZplQuantityMode.FromFile);

        var firstRendered = _renderedZplLabels[0];
        _zplRenderOptions = new ZplRenderOptions(
            firstRendered.WidthMm,
            firstRendered.HeightMm,
            firstRendered.Dpmm);

        ResetLabelLayoutState();
        previousSession?.Dispose();

        UpdateViewerControlsUi();
        ShowCurrentZplPreview();
        UpdateLabelPropertiesUi();

        var fileName = Path.GetFileName(document.SourcePath);
        Title = $"SG PDF Editor — {fileName}";
        UpdateCurrentZplStatus();
    }

    private static void ValidateRenderedLabels(
        ZplDocument document,
        IReadOnlyList<ZplRenderedLabel> renderedLabels)
    {
        ArgumentNullException.ThrowIfNull(renderedLabels);
        if (renderedLabels.Count != document.Designs.Count || renderedLabels.Count == 0)
        {
            throw new ArgumentException(
                "Rendered label count must match the printable ZPL design count.",
                nameof(renderedLabels));
        }
    }

    private void PreviousLabel_Click(object sender, RoutedEventArgs e)
    {
        NavigateLabel(-1);
    }

    private void NextLabel_Click(object sender, RoutedEventArgs e)
    {
        NavigateLabel(1);
    }

    private void NavigateLabel(int offset)
    {
        if (_isBusy || _zplDocument is null || _renderedZplLabels.Count == 0)
            return;

        if (_showSheetPreview)
        {
            NavigateLabelSheet(offset);
            return;
        }

        var candidateIndex = Math.Clamp(
            _selectedZplDesignIndex + offset,
            0,
            _renderedZplLabels.Count - 1);
        if (candidateIndex == _selectedZplDesignIndex)
            return;

        _selectedZplDesignIndex = candidateIndex;
        ShowSelectedZplPreview();
    }

    private void ShowSelectedZplPreview()
    {
        if (_zplDocument is null || _renderedZplLabels.Count == 0)
            return;

        PdfImage.Source = CreateZplBitmapSource(_renderedZplLabels[_selectedZplDesignIndex].PngBytes);
        PdfImage.Visibility = Visibility.Visible;
        LabelSheetCanvas.Visibility = Visibility.Collapsed;
        EmptyStateText.Visibility = Visibility.Collapsed;
        LabelNavigationBar.Visibility = Visibility.Visible;

        UpdateLabelNavigationUi();
        UpdateCurrentZplStatus();
    }

    private static BitmapSource CreateZplBitmapSource(byte[] pngBytes)
    {
        ArgumentNullException.ThrowIfNull(pngBytes);

        using var stream = new MemoryStream(pngBytes, writable: false);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void UpdateLabelNavigationUi()
    {
        if (_zplDocument is null || _renderedZplLabels.Count == 0 || _session is not null)
        {
            LabelNavigationBar.Visibility = Visibility.Collapsed;
            LabelCountText.Text = "Etiqueta 0 de 0";
            PreviousLabelButton.IsEnabled = false;
            NextLabelButton.IsEnabled = false;
            return;
        }

        LabelNavigationBar.Visibility = Visibility.Visible;
        if (_showSheetPreview && _labelLayoutPlan is not null)
        {
            LabelCountText.Text = $"Hoja {_selectedLabelSheetIndex + 1} de {_labelLayoutPlan.PageCount}";
            PreviousLabelButton.IsEnabled = _selectedLabelSheetIndex > 0;
            NextLabelButton.IsEnabled = _selectedLabelSheetIndex < _labelLayoutPlan.PageCount - 1;
            return;
        }

        LabelCountText.Text = $"Etiqueta {_selectedZplDesignIndex + 1} de {_renderedZplLabels.Count}";
        PreviousLabelButton.IsEnabled = _selectedZplDesignIndex > 0;
        NextLabelButton.IsEnabled = _selectedZplDesignIndex < _renderedZplLabels.Count - 1;
    }

    private void QuantityMode_Checked(object sender, RoutedEventArgs e)
    {
        UpdateQuantitySelectionFromUi();
    }

    private void CustomQuantityTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_zplDocument is not null && QuantityCustomRadio.IsChecked == true)
            UpdateQuantitySelectionFromUi();
    }

    private void UpdateQuantitySelectionFromUi()
    {
        if (_updatingLabelPropertiesUi || _zplDocument is null)
            return;

        var mode = QuantityCustomRadio.IsChecked == true
            ? ZplQuantityMode.Custom
            : QuantityOneEachRadio.IsChecked == true
                ? ZplQuantityMode.OneEach
                : ZplQuantityMode.FromFile;

        var customQuantity = _zplQuantitySelection.CustomQuantity;
        if (mode == ZplQuantityMode.Custom)
        {
            if (!long.TryParse(CustomQuantityTextBox.Text, out customQuantity) ||
                customQuantity < 1 ||
                customQuantity > ZplQuantitySelection.MaxCustomQuantity)
            {
                OutputQuantityText.Text = "Cantidad de salida: —";
                return;
            }
        }

        _zplQuantitySelection = new ZplQuantitySelection(mode, customQuantity);
        RebuildLabelLayoutPlan();
        UpdateLabelPropertiesUi();
        UpdateCurrentZplStatus();
    }

    private async void LabelSizeComboBox_SelectionChanged(
        object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_updatingLabelPropertiesUi || _zplDocument is null || _isBusy)
            return;

        if (LabelSizeComboBox.SelectedIndex == 3)
        {
            CustomSizePanel.Visibility = Visibility.Visible;
            return;
        }

        ZplRenderOptions? candidate = LabelSizeComboBox.SelectedIndex switch
        {
            0 => new ZplRenderOptions(102d, 152d, _zplRenderOptions.Dpmm),
            1 => new ZplRenderOptions(100d, 150d, _zplRenderOptions.Dpmm),
            2 => new ZplRenderOptions(100d, 100d, _zplRenderOptions.Dpmm),
            _ => null
        };

        if (candidate is not null)
            await ReRenderCurrentZplAsync(candidate);
    }

    private async void LabelDpmmComboBox_SelectionChanged(
        object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_updatingLabelPropertiesUi || _zplDocument is null || _isBusy)
            return;

        var dpmm = LabelDpmmComboBox.SelectedIndex switch
        {
            0 => 6,
            1 => 8,
            2 => 12,
            3 => 24,
            _ => _zplRenderOptions.Dpmm
        };

        await ReRenderCurrentZplAsync(new ZplRenderOptions(
            _zplRenderOptions.WidthMm,
            _zplRenderOptions.HeightMm,
            dpmm));
    }

    private async void ApplyCustomSize_Click(object sender, RoutedEventArgs e)
    {
        if (_zplDocument is null || _isBusy)
            return;

        if (!TryParsePositiveMillimeters(CustomWidthTextBox.Text, out var widthMm) ||
            !TryParsePositiveMillimeters(CustomHeightTextBox.Text, out var heightMm))
        {
            StatusText.Text = "Ingresa un ancho y alto válidos en milímetros.";
            return;
        }

        await ReRenderCurrentZplAsync(new ZplRenderOptions(
            widthMm,
            heightMm,
            _zplRenderOptions.Dpmm));
    }

    private async Task ReRenderCurrentZplAsync(ZplRenderOptions candidateOptions)
    {
        if (_zplDocument is null || _isBusy)
            return;

        if (RenderOptionsEqual(candidateOptions, _zplRenderOptions))
        {
            UpdateLabelPropertiesUi();
            return;
        }

        var sourceDocument = _zplDocument;
        var renderCts = new CancellationTokenSource();
        var renderToken = renderCts.Token;
        _labelRenderCts = renderCts;

        SetBusy(true);
        StatusText.Text = "Renderizando etiquetas ZPL...";

        try
        {
            var rendered = await _renderZplAsync(
                sourceDocument,
                candidateOptions,
                renderToken);

            renderToken.ThrowIfCancellationRequested();
            if (!ReferenceEquals(_zplDocument, sourceDocument) ||
                !ReferenceEquals(_labelRenderCts, renderCts))
            {
                return;
            }

            ValidateRenderedLabels(sourceDocument, rendered);
            _renderedZplLabels = rendered.ToArray();
            _zplRenderOptions = candidateOptions;
            _selectedZplDesignIndex = Math.Clamp(
                _selectedZplDesignIndex,
                0,
                _renderedZplLabels.Count - 1);

            RebuildLabelLayoutPlan(showPreview: false);
            ShowCurrentZplPreview();
            UpdateLabelPropertiesUi();
        }
        catch (OperationCanceledException) when (renderToken.IsCancellationRequested)
        {
            // An interrupted re-render keeps the last valid preview/settings intact.
        }
        catch (Exception)
        {
            if (ReferenceEquals(_zplDocument, sourceDocument))
            {
                StatusText.Text = "No se pudo aplicar el tamaño o resolución solicitados.";
                UpdateLabelPropertiesUi();
            }
        }
        finally
        {
            if (ReferenceEquals(_labelRenderCts, renderCts))
                _labelRenderCts = null;

            renderCts.Dispose();
            SetBusy(false);
            UpdateLabelPropertiesUi();
        }
    }

    private static bool RenderOptionsEqual(ZplRenderOptions left, ZplRenderOptions right)
        => Math.Abs(left.WidthMm - right.WidthMm) < 0.001d &&
           Math.Abs(left.HeightMm - right.HeightMm) < 0.001d &&
           left.Dpmm == right.Dpmm;

    private static bool TryParsePositiveMillimeters(string text, out double value)
    {
        var parsed = double.TryParse(
            text,
            NumberStyles.Float,
            CultureInfo.CurrentCulture,
            out value);

        if (!parsed)
        {
            parsed = double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        if (!parsed || !double.IsFinite(value) || value <= 0d)
        {
            value = 0d;
            return false;
        }

        return true;
    }

    private void UpdateLabelPropertiesUi()
    {
        if (_zplDocument is null)
        {
            LabelPropertiesPanel.Visibility = Visibility.Collapsed;
            PropertiesPlaceholderText.Visibility = Visibility.Visible;
            return;
        }

        LabelPropertiesPanel.Visibility = Visibility.Visible;
        PropertiesPlaceholderText.Visibility = Visibility.Collapsed;

        _updatingLabelPropertiesUi = true;
        try
        {
            QuantityFromFileRadio.IsChecked = _zplQuantitySelection.Mode == ZplQuantityMode.FromFile;
            QuantityOneEachRadio.IsChecked = _zplQuantitySelection.Mode == ZplQuantityMode.OneEach;
            QuantityCustomRadio.IsChecked = _zplQuantitySelection.Mode == ZplQuantityMode.Custom;

            var customQuantityText = _zplQuantitySelection.CustomQuantity.ToString();
            if (!string.Equals(CustomQuantityTextBox.Text, customQuantityText, StringComparison.Ordinal))
                CustomQuantityTextBox.Text = customQuantityText;

            CustomQuantityTextBox.IsEnabled =
                !_isBusy && _zplQuantitySelection.Mode == ZplQuantityMode.Custom;
            OutputQuantityText.Text =
                $"Cantidad de salida: {_zplQuantitySelection.GetTotalQuantity(_zplDocument)}";

            var sizeIndex = GetSizePresetIndex(_zplRenderOptions);
            LabelSizeComboBox.SelectedIndex = sizeIndex;
            CustomSizePanel.Visibility = sizeIndex == 3
                ? Visibility.Visible
                : Visibility.Collapsed;

            var widthText = FormatMillimeters(_zplRenderOptions.WidthMm);
            var heightText = FormatMillimeters(_zplRenderOptions.HeightMm);
            if (!string.Equals(CustomWidthTextBox.Text, widthText, StringComparison.Ordinal))
                CustomWidthTextBox.Text = widthText;
            if (!string.Equals(CustomHeightTextBox.Text, heightText, StringComparison.Ordinal))
                CustomHeightTextBox.Text = heightText;

            LabelDpmmComboBox.SelectedIndex = GetDpmmIndex(_zplRenderOptions.Dpmm);
            RenderSettingsText.Text =
                $"{widthText} × {heightText} mm · {_zplRenderOptions.Dpmm} dpmm ({GetApproximateDpi(_zplRenderOptions.Dpmm)})";

            UpdateLabelLayoutPropertiesUiCore();
        }
        finally
        {
            _updatingLabelPropertiesUi = false;
        }
    }

    private static int GetSizePresetIndex(ZplRenderOptions options)
    {
        if (SameSize(options, 102d, 152d))
            return 0;
        if (SameSize(options, 100d, 150d))
            return 1;
        if (SameSize(options, 100d, 100d))
            return 2;
        return 3;
    }

    private static bool SameSize(ZplRenderOptions options, double widthMm, double heightMm)
        => Math.Abs(options.WidthMm - widthMm) < 0.001d &&
           Math.Abs(options.HeightMm - heightMm) < 0.001d;

    private static int GetDpmmIndex(int dpmm)
        => dpmm switch
        {
            6 => 0,
            8 => 1,
            12 => 2,
            24 => 3,
            _ => 1
        };

    private static string GetApproximateDpi(int dpmm)
        => dpmm switch
        {
            6 => "~152 dpi",
            8 => "~203 dpi",
            12 => "~300 dpi",
            24 => "~600 dpi",
            _ => $"~{Math.Round(dpmm * 25.4d)} dpi"
        };

    private static string FormatMillimeters(double value)
        => value.ToString("0.##", CultureInfo.CurrentCulture);

    private void UpdateCurrentZplStatus()
    {
        if (_zplDocument is null || _renderedZplLabels.Count == 0)
            return;

        var fileName = Path.GetFileName(_zplDocument.SourcePath);
        if (_showSheetPreview && _labelLayoutPlan is not null)
        {
            StatusText.Text =
                $"{fileName} — hoja {_selectedLabelSheetIndex + 1} de {_labelLayoutPlan.PageCount} — cantidad de salida {_zplQuantitySelection.GetTotalQuantity(_zplDocument)}";
            return;
        }

        StatusText.Text =
            $"{fileName} — etiqueta {_selectedZplDesignIndex + 1} de {_renderedZplLabels.Count} — cantidad de salida {_zplQuantitySelection.GetTotalQuantity(_zplDocument)}";
    }

    private void NavigationBar_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (NavigationBar.Visibility == Visibility.Visible && _session is not null)
            ClearZplWorkspaceState();
    }

    private void PdfImage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (PdfImage.Visibility == Visibility.Visible && _session is not null)
            ClearZplWorkspaceState();
    }

    private void ClearZplWorkspaceState()
    {
        _zplDocument = null;
        _renderedZplLabels = Array.Empty<ZplRenderedLabel>();
        _selectedZplDesignIndex = 0;
        _zplQuantitySelection = new ZplQuantitySelection(ZplQuantityMode.FromFile);
        ClearLabelLayoutState();
        LabelNavigationBar.Visibility = Visibility.Collapsed;
        LabelCountText.Text = "Etiqueta 0 de 0";
        PreviousLabelButton.IsEnabled = false;
        NextLabelButton.IsEnabled = false;
        UpdateLabelPropertiesUi();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _labelRenderCts?.Cancel();
        _labelRenderCts = null;
        _zplDocument = null;
        _renderedZplLabels = Array.Empty<ZplRenderedLabel>();
        _labelLayoutPlan = null;
    }
}
