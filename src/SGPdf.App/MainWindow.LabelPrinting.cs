using System.Windows;
using SGPdf.App.Features.Labels;
using SGPdf.App.Printing;

namespace SGPdf.App;

public partial class MainWindow
{
    private Func<Window, LabelLayoutPlan, IReadOnlyList<ZplRenderedLabel>, int, bool> _printThermalLabels =
        WindowsThermalPrintWorkflow.TryPrint;

    private void PrintLabels_Click(object sender, RoutedEventArgs e)
    {
        if (_zplDocument is null ||
            _renderedZplLabels.Count == 0 ||
            _labelLayoutPlan is null ||
            _labelLayoutSettings.MediaKind != LabelMediaKind.Thermal ||
            _isBusy)
        {
            return;
        }

        var plan = _labelLayoutPlan;
        var renderedLabels = _renderedZplLabels;
        var requestedDpi = GetRequestedPrinterDpi(_zplRenderOptions.Dpmm);

        SetBusy(true);
        UpdateLabelPropertiesUi();
        StatusText.Text = "Preparando impresión...";

        try
        {
            var submitted = _printThermalLabels(this, plan, renderedLabels, requestedDpi);
            StatusText.Text = submitted
                ? "Trabajo de impresión enviado."
                : "Impresión cancelada.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "No se pudo imprimir las etiquetas.";
            MessageBox.Show(
                this,
                $"No se pudo enviar las etiquetas a la impresora.\n\n{ex.Message}",
                "SG PDF Editor",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
            UpdateLabelPropertiesUi();
        }
    }

    private static int GetRequestedPrinterDpi(int dpmm)
        => dpmm switch
        {
            6 => 152,
            8 => 203,
            12 => 300,
            24 => 600,
            _ => checked((int)Math.Round(dpmm * 25.4d, MidpointRounding.AwayFromZero))
        };
}
