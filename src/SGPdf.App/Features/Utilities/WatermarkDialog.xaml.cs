using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace SGPdf.App.Features.Utilities;

public partial class WatermarkDialog : Window
{
    private readonly int _pageCount;
    private readonly int _currentPageNumber;

    internal WatermarkDialog(int pageCount, int currentPageNumber)
        : this(pageCount, currentPageNumber, allowCurrentPage: true)
    {
    }

    internal WatermarkDialog(int pageCount, int currentPageNumber, bool allowCurrentPage)
    {
        if (pageCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageCount));
        if (currentPageNumber < 1 || currentPageNumber > pageCount)
            throw new ArgumentOutOfRangeException(nameof(currentPageNumber));

        _pageCount = pageCount;
        _currentPageNumber = currentPageNumber;
        InitializeComponent();
        EndPageTextBox.Text = pageCount.ToString(CultureInfo.CurrentCulture);

        if (!allowCurrentPage)
        {
            var currentPageItem = ScopeComboBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(item => string.Equals(
                    item.Tag as string,
                    "Current",
                    StringComparison.Ordinal));
            if (currentPageItem is not null)
                ScopeComboBox.Items.Remove(currentPageItem);
        }
    }

    internal WatermarkUtilityRequest? Request { get; private set; }

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
        if (!TryParseDouble(FontSizeTextBox.Text, out var fontSize) || fontSize <= 0d ||
            !TryParseDouble(OpacityTextBox.Text, out var opacityPercent) || opacityPercent < 0d || opacityPercent > 100d ||
            !TryParseDouble(RotationTextBox.Text, out var rotationDegrees) ||
            !byte.TryParse(RedTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var red) ||
            !byte.TryParse(GreenTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var green) ||
            !byte.TryParse(BlueTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var blue))
        {
            ValidationText.Text = "Revisa los valores numéricos. Tamaño, opacidad, rotación y RGB deben ser válidos.";
            return;
        }

        var scope = SelectedTag(ScopeComboBox);
        var useCurrent = scope == "Current";
        var useAll = scope == "All";
        var startPage = useAll ? 1 : _currentPageNumber;
        var endPage = useAll ? _pageCount : _currentPageNumber;
        if (scope == "Range" &&
            (!int.TryParse(StartPageTextBox.Text, out startPage) ||
             !int.TryParse(EndPageTextBox.Text, out endPage)))
        {
            ValidationText.Text = "El rango de páginas no es válido.";
            return;
        }

        var request = useAll
            ? WatermarkUtilityRequest.CreateAllPages(
                WatermarkTextBox.Text,
                fontSize,
                opacityPercent / 100d,
                rotationDegrees,
                0d,
                0d,
                SelectedTag(LayerComboBox) == "Background",
                SelectedTag(AnchorComboBox) ?? string.Empty,
                red,
                green,
                blue)
            : new WatermarkUtilityRequest(
                WatermarkTextBox.Text,
                fontSize,
                opacityPercent / 100d,
                rotationDegrees,
                0d,
                0d,
                startPage,
                endPage,
                useCurrent,
                SelectedTag(LayerComboBox) == "Background",
                SelectedTag(AnchorComboBox) ?? string.Empty,
                red,
                green,
                blue);

        if (!request.TryBuild(_pageCount, _currentPageNumber - 1, out _, out _, out _, out var error))
        {
            ValidationText.Text = error ?? "Los valores de la marca de agua no son válidos.";
            return;
        }

        Request = request;
        DialogResult = true;
    }

    private static string? SelectedTag(ComboBox comboBox)
        => (comboBox.SelectedItem as ComboBoxItem)?.Tag as string;

    private static bool TryParseDouble(string value, out double result)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result) ||
           double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
}
