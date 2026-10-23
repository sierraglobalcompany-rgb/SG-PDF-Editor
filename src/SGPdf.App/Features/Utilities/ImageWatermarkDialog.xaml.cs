using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SGPdf.App.Features.Utilities;

internal sealed record ImageWatermarkRequest(
    string ImagePath,
    UtilityPageRange Range,
    double WidthPercent,
    double OpacityPercent,
    WatermarkAnchor Anchor);

public partial class ImageWatermarkDialog : Window
{
    private readonly int _pageCount;
    private readonly int _currentPageIndex;

    internal ImageWatermarkDialog(int pageCount, int currentPageIndex)
    {
        if (pageCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageCount));
        if (currentPageIndex < 0 || currentPageIndex >= pageCount)
            throw new ArgumentOutOfRangeException(nameof(currentPageIndex));

        _pageCount = pageCount;
        _currentPageIndex = currentPageIndex;
        InitializeComponent();
        EndPageTextBox.Text = pageCount.ToString(CultureInfo.CurrentCulture);
    }

    internal ImageWatermarkRequest? Request { get; private set; }

    private void Window_Loaded(object sender, RoutedEventArgs e)
        => ChooseImageButton.Focus();

    private void ChooseImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar imagen para la marca de agua",
            Filter = "Imágenes PNG y JPEG (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
            return;

        ImagePathTextBox.Text = dialog.FileName;
        ContinueButton.IsEnabled = true;
        ValidationText.Text = string.Empty;
    }

    private void ScopeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RangePanel is null)
            return;
        RangePanel.Visibility = SelectedTag(ScopeComboBox) == "Range"
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;
        if (string.IsNullOrWhiteSpace(ImagePathTextBox.Text) || !File.Exists(ImagePathTextBox.Text))
        {
            ValidationText.Text = "Elige una imagen PNG o JPEG disponible.";
            ChooseImageButton.Focus();
            return;
        }

        if (!TryParseDouble(WidthPercentTextBox.Text, out var widthPercent) || widthPercent is < 10d or > 80d ||
            !TryParseDouble(OpacityPercentTextBox.Text, out var opacityPercent) || opacityPercent is < 10d or > 100d)
        {
            ValidationText.Text = "El ancho debe estar entre 10 y 80 %, y la opacidad entre 10 y 100 %.";
            WidthPercentTextBox.Focus();
            return;
        }

        UtilityPageRange pageRange;
        switch (SelectedTag(ScopeComboBox))
        {
            case "All":
                pageRange = UtilityPageRange.All;
                break;
            case "Current":
                pageRange = UtilityPageRange.Current(_currentPageIndex);
                break;
            case "Range":
                if (!int.TryParse(StartPageTextBox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var firstPage) ||
                    !int.TryParse(EndPageTextBox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var lastPage) ||
                    firstPage < 1 || lastPage < firstPage || lastPage > _pageCount)
                {
                    ValidationText.Text = $"Escribe un intervalo inclusivo entre 1 y {_pageCount}.";
                    StartPageTextBox.Focus();
                    return;
                }
                if (lastPage - firstPage + 1 > 150)
                {
                    ValidationText.Text = "Se pueden seleccionar hasta 150 páginas por operación.";
                    StartPageTextBox.Focus();
                    return;
                }
                pageRange = UtilityPageRange.Explicit(firstPage - 1, lastPage - 1);
                break;
            default:
                ValidationText.Text = "Selecciona un rango de páginas válido.";
                ScopeComboBox.Focus();
                return;
        }

        if (pageRange.Resolve(_pageCount).Count > 150)
        {
            ValidationText.Text = "Se pueden seleccionar hasta 150 páginas por operación.";
            ScopeComboBox.Focus();
            return;
        }

        if (!Enum.TryParse<WatermarkAnchor>(SelectedTag(AnchorComboBox), out var anchor))
        {
            ValidationText.Text = "Selecciona una posición válida.";
            AnchorComboBox.Focus();
            return;
        }

        Request = new ImageWatermarkRequest(
            ImagePathTextBox.Text,
            pageRange,
            widthPercent,
            opacityPercent,
            anchor);
        DialogResult = true;
    }

    private static string? SelectedTag(ComboBox comboBox)
        => (comboBox.SelectedItem as ComboBoxItem)?.Tag as string;

    private static bool TryParseDouble(string? text, out double value)
    {
        if ((double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
             double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) &&
            double.IsFinite(value))
        {
            return true;
        }

        value = 0d;
        return false;
    }
}
