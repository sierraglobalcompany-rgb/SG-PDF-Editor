using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Labels;

namespace SGPdf.App;

public partial class MainWindow
{
    private const double SheetPreviewPixelsPerMm = 3d;

    private LabelLayoutSettings _labelLayoutSettings = new(LabelMediaKind.Thermal);
    private LabelLayoutPlan? _labelLayoutPlan;
    private string? _labelLayoutValidationMessage;
    private bool _showSheetPreview;
    private long _selectedLabelSheetIndex;

    private void ResetLabelLayoutState()
    {
        _labelLayoutSettings = new LabelLayoutSettings(LabelMediaKind.Thermal);
        _showSheetPreview = false;
        _selectedLabelSheetIndex = 0;
        RebuildLabelLayoutPlan(showPreview: false);
    }

    private void ClearLabelLayoutState()
    {
        _labelLayoutSettings = new LabelLayoutSettings(LabelMediaKind.Thermal);
        _labelLayoutPlan = null;
        _labelLayoutValidationMessage = null;
        _showSheetPreview = false;
        _selectedLabelSheetIndex = 0;
        LabelSheetCanvas.Children.Clear();
        LabelSheetCanvas.Visibility = Visibility.Collapsed;
    }

    private void PreviewMode_Checked(object sender, RoutedEventArgs e)
    {
        if (_updatingLabelPropertiesUi || _zplDocument is null)
            return;

        _showSheetPreview = PreviewSheetRadio.IsChecked == true && _labelLayoutPlan is not null;
        if (_showSheetPreview)
        {
            _selectedLabelSheetIndex = Math.Clamp(
                _selectedLabelSheetIndex,
                0,
                _labelLayoutPlan!.PageCount - 1);
        }

        ShowCurrentZplPreview();
    }

    private void SheetInputSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingLabelPropertiesUi || _zplDocument is null)
            return;

        UpdateSheetInputVisibility();
    }

    private void ApplySheetLayout_Click(object sender, RoutedEventArgs e)
    {
        if (_zplDocument is null || _isBusy)
            return;

        try
        {
            _labelLayoutSettings = ReadLabelLayoutSettingsFromUi();
            RebuildLabelLayoutPlan(showPreview: true);
            UpdateLabelPropertiesUi();
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        {
            _labelLayoutPlan = null;
            _labelLayoutValidationMessage = "Revisa las medidas, márgenes, separaciones y filas/columnas.";
            PreviewSheetRadio.IsEnabled = false;
            LayoutValidationText.Text = _labelLayoutValidationMessage;
            LayoutValidationText.Visibility = Visibility.Visible;
            if (_showSheetPreview)
            {
                _showSheetPreview = false;
                PreviewLabelRadio.IsChecked = true;
                ShowSelectedZplPreview();
            }
        }
    }

    private LabelLayoutSettings ReadLabelLayoutSettingsFromUi()
    {
        var mediaKind = SheetMediaComboBox.SelectedIndex switch
        {
            0 => LabelMediaKind.Thermal,
            1 => LabelMediaKind.A4,
            2 => LabelMediaKind.Letter,
            3 => LabelMediaKind.Custom,
            _ => LabelMediaKind.Thermal
        };

        var orientation = SheetOrientationComboBox.SelectedIndex == 1
            ? LabelPageOrientation.Landscape
            : LabelPageOrientation.Portrait;
        var rotation = SheetRotationComboBox.SelectedIndex == 1
            ? LabelRotation.Degrees90
            : LabelRotation.Degrees0;

        if (!TryParseNonNegativeMillimeters(SheetHorizontalMarginTextBox.Text, out var horizontalMarginMm) ||
            !TryParseNonNegativeMillimeters(SheetVerticalMarginTextBox.Text, out var verticalMarginMm) ||
            !TryParseNonNegativeMillimeters(SheetHorizontalGapTextBox.Text, out var horizontalGapMm) ||
            !TryParseNonNegativeMillimeters(SheetVerticalGapTextBox.Text, out var verticalGapMm))
        {
            throw new ArgumentException("Invalid sheet margins or gaps.");
        }

        var customPageWidthMm = 0d;
        var customPageHeightMm = 0d;
        if (mediaKind == LabelMediaKind.Custom &&
            (!TryParsePositiveMillimeters(CustomSheetWidthTextBox.Text, out customPageWidthMm) ||
             !TryParsePositiveMillimeters(CustomSheetHeightTextBox.Text, out customPageHeightMm)))
        {
            throw new ArgumentException("Invalid custom sheet size.");
        }

        if (mediaKind == LabelMediaKind.Thermal)
        {
            return new LabelLayoutSettings(
                mediaKind,
                labelsPerPage: 1,
                orientation: LabelPageOrientation.Portrait,
                rotation: rotation);
        }

        int labelsPerPage;
        int? rows = null;
        int? columns = null;
        if (SheetLayoutComboBox.SelectedIndex == 8)
        {
            if (!int.TryParse(CustomSheetRowsTextBox.Text, out var parsedRows) || parsedRows < 1 ||
                !int.TryParse(CustomSheetColumnsTextBox.Text, out var parsedColumns) || parsedColumns < 1)
            {
                throw new ArgumentException("Invalid custom grid.");
            }

            rows = parsedRows;
            columns = parsedColumns;
            labelsPerPage = checked(parsedRows * parsedColumns);
        }
        else
        {
            labelsPerPage = SheetLayoutComboBox.SelectedIndex switch
            {
                0 => 1,
                1 => 2,
                2 => 3,
                3 => 4,
                4 => 6,
                5 => 8,
                6 => 10,
                7 => 12,
                _ => 1
            };
        }

        return new LabelLayoutSettings(
            mediaKind,
            labelsPerPage,
            rows,
            columns,
            orientation,
            rotation,
            horizontalMarginMm,
            verticalMarginMm,
            horizontalGapMm,
            verticalGapMm,
            customPageWidthMm,
            customPageHeightMm);
    }

    private void RebuildLabelLayoutPlan(bool showPreview = true)
    {
        if (_zplDocument is null || _renderedZplLabels.Count == 0)
        {
            _labelLayoutPlan = null;
            _labelLayoutValidationMessage = null;
            return;
        }

        try
        {
            var sequence = new LabelOutputSequence(_zplDocument, _zplQuantitySelection);
            _labelLayoutPlan = LabelLayoutPlanner.CreatePlan(
                sequence,
                _labelLayoutSettings,
                _zplRenderOptions.WidthMm,
                _zplRenderOptions.HeightMm);
            _labelLayoutValidationMessage = null;
            _selectedLabelSheetIndex = Math.Clamp(
                _selectedLabelSheetIndex,
                0,
                _labelLayoutPlan.PageCount - 1);
        }
        catch (InvalidOperationException ex)
        {
            _labelLayoutPlan = null;
            _labelLayoutValidationMessage = ex.Message;
            _selectedLabelSheetIndex = 0;
        }

        if (_labelLayoutPlan is null && _showSheetPreview)
            _showSheetPreview = false;

        if (showPreview)
            ShowCurrentZplPreview();
    }

    private void ShowCurrentZplPreview()
    {
        if (_showSheetPreview && _labelLayoutPlan is not null)
            ShowSelectedZplSheetPreview();
        else
            ShowSelectedZplPreview();
    }

    private void NavigateLabelSheet(int offset)
    {
        if (_labelLayoutPlan is null || _labelLayoutPlan.PageCount == 0)
            return;

        var candidateIndex = Math.Clamp(
            _selectedLabelSheetIndex + offset,
            0,
            _labelLayoutPlan.PageCount - 1);
        if (candidateIndex == _selectedLabelSheetIndex)
            return;

        _selectedLabelSheetIndex = candidateIndex;
        ShowSelectedZplSheetPreview();
    }

    private void ShowSelectedZplSheetPreview()
    {
        if (_labelLayoutPlan is null || _renderedZplLabels.Count == 0)
            return;

        var page = _labelLayoutPlan.GetPage(_selectedLabelSheetIndex);
        LabelSheetCanvas.Children.Clear();
        LabelSheetCanvas.Width = _labelLayoutPlan.PageWidthMm * SheetPreviewPixelsPerMm;
        LabelSheetCanvas.Height = _labelLayoutPlan.PageHeightMm * SheetPreviewPixelsPerMm;

        foreach (var placement in page.Placements)
        {
            var source = CreateZplBitmapSource(_renderedZplLabels[placement.DesignIndex].PngBytes);
            BitmapSource previewSource = source;
            if (placement.Rotation == LabelRotation.Degrees90)
            {
                var rotated = new TransformedBitmap(source, new RotateTransform(90));
                rotated.Freeze();
                previewSource = rotated;
            }

            var image = new Image
            {
                Source = previewSource,
                Width = placement.WidthMm * SheetPreviewPixelsPerMm,
                Height = placement.HeightMm * SheetPreviewPixelsPerMm,
                Stretch = Stretch.Fill
            };
            Canvas.SetLeft(image, placement.XMm * SheetPreviewPixelsPerMm);
            Canvas.SetTop(image, placement.YMm * SheetPreviewPixelsPerMm);
            LabelSheetCanvas.Children.Add(image);
        }

        PdfImage.Visibility = Visibility.Collapsed;
        LabelSheetCanvas.Visibility = Visibility.Visible;
        EmptyStateText.Visibility = Visibility.Collapsed;
        LabelNavigationBar.Visibility = Visibility.Visible;
        UpdateLabelNavigationUi();
        UpdateCurrentZplStatus();
    }

    private void UpdateLabelLayoutPropertiesUiCore()
    {
        PreviewLabelRadio.IsChecked = !_showSheetPreview;
        PreviewSheetRadio.IsChecked = _showSheetPreview;
        PreviewSheetRadio.IsEnabled = _labelLayoutPlan is not null;

        SheetMediaComboBox.SelectedIndex = _labelLayoutSettings.MediaKind switch
        {
            LabelMediaKind.Thermal => 0,
            LabelMediaKind.A4 => 1,
            LabelMediaKind.Letter => 2,
            LabelMediaKind.Custom => 3,
            _ => 0
        };
        SheetOrientationComboBox.SelectedIndex = _labelLayoutSettings.Orientation == LabelPageOrientation.Landscape ? 1 : 0;
        SheetRotationComboBox.SelectedIndex = _labelLayoutSettings.Rotation == LabelRotation.Degrees90 ? 1 : 0;

        if (_labelLayoutSettings.Rows is not null && _labelLayoutSettings.Columns is not null)
        {
            SheetLayoutComboBox.SelectedIndex = 8;
            CustomSheetRowsTextBox.Text = _labelLayoutSettings.Rows.Value.ToString(CultureInfo.CurrentCulture);
            CustomSheetColumnsTextBox.Text = _labelLayoutSettings.Columns.Value.ToString(CultureInfo.CurrentCulture);
        }
        else
        {
            SheetLayoutComboBox.SelectedIndex = _labelLayoutSettings.LabelsPerPage switch
            {
                1 => 0,
                2 => 1,
                3 => 2,
                4 => 3,
                6 => 4,
                8 => 5,
                10 => 6,
                12 => 7,
                _ => 0
            };
        }

        SheetHorizontalMarginTextBox.Text = FormatMillimeters(_labelLayoutSettings.HorizontalMarginMm);
        SheetVerticalMarginTextBox.Text = FormatMillimeters(_labelLayoutSettings.VerticalMarginMm);
        SheetHorizontalGapTextBox.Text = FormatMillimeters(_labelLayoutSettings.HorizontalGapMm);
        SheetVerticalGapTextBox.Text = FormatMillimeters(_labelLayoutSettings.VerticalGapMm);

        if (_labelLayoutSettings.MediaKind == LabelMediaKind.Custom)
        {
            CustomSheetWidthTextBox.Text = FormatMillimeters(_labelLayoutSettings.CustomPageWidthMm);
            CustomSheetHeightTextBox.Text = FormatMillimeters(_labelLayoutSettings.CustomPageHeightMm);
        }

        LayoutValidationText.Text = _labelLayoutValidationMessage ?? string.Empty;
        LayoutValidationText.Visibility = string.IsNullOrWhiteSpace(_labelLayoutValidationMessage)
            ? Visibility.Collapsed
            : Visibility.Visible;

        UpdateSheetInputVisibility();
    }

    private void UpdateSheetInputVisibility()
    {
        var isThermal = SheetMediaComboBox.SelectedIndex == 0;
        var isCustomMedia = SheetMediaComboBox.SelectedIndex == 3;
        var isCustomGrid = SheetLayoutComboBox.SelectedIndex == 8;

        CustomSheetSizePanel.Visibility = isCustomMedia ? Visibility.Visible : Visibility.Collapsed;
        CustomSheetGridPanel.Visibility = !isThermal && isCustomGrid ? Visibility.Visible : Visibility.Collapsed;
        SheetOrientationComboBox.IsEnabled = !isThermal;
        SheetLayoutComboBox.IsEnabled = !isThermal;
        SheetHorizontalMarginTextBox.IsEnabled = !isThermal;
        SheetVerticalMarginTextBox.IsEnabled = !isThermal;
        SheetHorizontalGapTextBox.IsEnabled = !isThermal;
        SheetVerticalGapTextBox.IsEnabled = !isThermal;
    }

    private static bool TryParseNonNegativeMillimeters(string text, out double value)
    {
        var parsed = double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        if (!parsed)
            parsed = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        if (!parsed || !double.IsFinite(value) || value < 0d)
        {
            value = 0d;
            return false;
        }

        return true;
    }
}
