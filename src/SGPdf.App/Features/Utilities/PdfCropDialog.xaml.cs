using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace SGPdf.App.Features.Utilities;

public partial class PdfCropDialog : Window
{
    private const double MinimumPageDimensionPoints = 20d;
    private readonly double _sourceWidthMm;
    private readonly double _sourceHeightMm;
    private bool _uiInitialized;

    internal PdfCropDialog(double sourceWidthPoints, double sourceHeightPoints)
    {
        InitializeComponent();
        _sourceWidthMm = sourceWidthPoints * 25.4d / 72d;
        _sourceHeightMm = sourceHeightPoints * 25.4d / 72d;
        _uiInitialized = true;
        UpdatePreview();
    }

    internal PdfCropMargins? Request { get; private set; }

    private void Window_Loaded(object sender, RoutedEventArgs e)
        => LeftTextBox.Focus();

    private void Margin_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_uiInitialized)
            UpdatePreview();
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;
        if (!TryReadMargins(out var margins, out var error))
        {
            ValidationText.Text = error;
            return;
        }

        if ((_sourceWidthMm - margins.LeftMm - margins.RightMm) * 72d / 25.4d < MinimumPageDimensionPoints ||
            (_sourceHeightMm - margins.TopMm - margins.BottomMm) * 72d / 25.4d < MinimumPageDimensionPoints)
        {
            ValidationText.Text = "Los márgenes dejarían una página demasiado pequeña.";
            return;
        }

        Request = margins;
        DialogResult = true;
    }

    private void UpdatePreview()
    {
        if (!TryReadMargins(out var margins, out var error))
        {
            OutputSizeText.Text = "Tamaño estimado de salida: revisa los márgenes.";
            if (IsLoaded)
                ValidationText.Text = error;
            return;
        }

        var width = _sourceWidthMm - margins.LeftMm - margins.RightMm;
        var height = _sourceHeightMm - margins.TopMm - margins.BottomMm;
        OutputSizeText.Text = $"Primera página: {_sourceWidthMm:0.##} × {_sourceHeightMm:0.##} mm → {width:0.##} × {height:0.##} mm. Se aplicará a todas las páginas.";
        if (IsLoaded)
            ValidationText.Text = string.Empty;
    }

    private bool TryReadMargins(out PdfCropMargins margins, out string error)
    {
        if (!TryRead(LeftTextBox.Text, out var left) ||
            !TryRead(RightTextBox.Text, out var right) ||
            !TryRead(TopTextBox.Text, out var top) ||
            !TryRead(BottomTextBox.Text, out var bottom))
        {
            margins = default!;
            error = "Escribe márgenes numéricos finitos iguales o mayores que cero.";
            return false;
        }

        margins = new PdfCropMargins(left, right, top, bottom);
        error = string.Empty;
        return true;
    }

    private static bool TryRead(string? text, out double value)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) &&
            double.IsFinite(value) && value >= 0d)
        {
            return true;
        }

        value = 0d;
        return false;
    }
}
