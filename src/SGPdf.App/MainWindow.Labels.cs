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
            var rendered = await new LabelizeProcessRenderer().RenderAsync(
                candidate,
                ZplRenderOptions.Default,
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
        ArgumentNullException.ThrowIfNull(renderedLabels);
        if (renderedLabels.Count != document.Designs.Count || renderedLabels.Count == 0)
        {
            throw new ArgumentException(
                "Rendered label count must match the printable ZPL design count.",
                nameof(renderedLabels));
        }

        _resizeRenderScheduler.CancelCurrent();

        var previousSession = _session;
        _session = null;
        _navigation = null;
        _zplDocument = document;
        _renderedZplLabels = renderedLabels.ToArray();
        _selectedZplDesignIndex = 0;
        _zplQuantitySelection = new ZplQuantitySelection(ZplQuantityMode.FromFile);
        previousSession?.Dispose();

        UpdateViewerControlsUi();
        ShowSelectedZplPreview();
        UpdateLabelPropertiesUi();

        var fileName = Path.GetFileName(document.SourcePath);
        Title = $"SG PDF Editor — {fileName}";
        UpdateCurrentZplStatus();
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
        UpdateLabelPropertiesUi();
        UpdateCurrentZplStatus();
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

            var customText = _zplQuantitySelection.CustomQuantity.ToString();
            if (!string.Equals(CustomQuantityTextBox.Text, customText, StringComparison.Ordinal))
                CustomQuantityTextBox.Text = customText;

            CustomQuantityTextBox.IsEnabled =
                !_isBusy && _zplQuantitySelection.Mode == ZplQuantityMode.Custom;
            OutputQuantityText.Text =
                $"Cantidad de salida: {_zplQuantitySelection.GetTotalQuantity(_zplDocument)}";
        }
        finally
        {
            _updatingLabelPropertiesUi = false;
        }
    }

    private void UpdateCurrentZplStatus()
    {
        if (_zplDocument is null || _renderedZplLabels.Count == 0)
            return;

        var fileName = Path.GetFileName(_zplDocument.SourcePath);
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
    }
}
