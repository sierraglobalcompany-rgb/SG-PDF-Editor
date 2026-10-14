using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using SGPdf.App.Features.Compare;

namespace SGPdf.App;

public partial class MainWindow
{
    private bool _compareUiInitialized;

    private void InitializeCompareUi()
    {
        if (_compareUiInitialized)
            return;

        if (LogicalTreeHelper.GetParent(OpenPdfMenuItem) is not MenuItem fileMenu)
            return;

        var compareMenuItem = new MenuItem
        {
            Header = "_Comparar con otro PDF..."
        };
        compareMenuItem.Click += ComparePdf_Click;
        compareMenuItem.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(MenuItem.IsEnabled)) { Source = PrintPdfMenuItem });

        int printIndex = fileMenu.Items.IndexOf(PrintPdfMenuItem);
        fileMenu.Items.Insert(printIndex >= 0 ? printIndex + 1 : fileMenu.Items.Count, compareMenuItem);
        _compareUiInitialized = true;
    }

    private async void ComparePdf_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null || _navigation is null || _isBusy)
            return;

        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar PDF para comparar",
            Filter = "Archivos PDF (*.pdf)|*.pdf|Todos los archivos (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
            return;

        string leftPath = _session.FilePath;
        string rightPath = Path.GetFullPath(dialog.FileName);
        SetBusy(true);
        StatusText.Text = "Comparando PDF...";

        try
        {
            PdfCompareResult result = await Task.Run(() => PdfCompareService.Compare(leftPath, rightPath));
            if (_session is null ||
                !string.Equals(_session.FilePath, leftPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ComparePresentation presentation = BuildComparePresentation(
                Path.GetFileName(leftPath),
                Path.GetFileName(rightPath),
                result);
            ShowComparePresentation(presentation);
            StatusText.Text = "Comparación completada.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "No se pudo comparar el PDF.";
            if (IsVisible)
            {
                MessageBox.Show(
                    this,
                    $"No se pudo comparar el PDF seleccionado.\n\n{ex.Message}",
                    "SG PDF Editor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowComparePresentation(ComparePresentation presentation)
    {
        LabelPropertiesPanel.Visibility = Visibility.Collapsed;
        PropertiesPlaceholderText.Visibility = Visibility.Visible;
        PropertiesPlaceholderText.TextWrapping = TextWrapping.Wrap;
        PropertiesPlaceholderText.FontWeight = FontWeights.Normal;
        PropertiesPlaceholderText.Text = BuildComparePanelText(presentation);
    }

    private static string BuildComparePanelText(ComparePresentation presentation)
    {
        static string PageCount(int count)
            => $"{count} {(count == 1 ? "página" : "páginas")}";

        return
            $"Comparación PDF\n\n" +
            $"PDF A: {presentation.LeftDisplayName} ({PageCount(presentation.LeftPageCount)})\n" +
            $"PDF B: {presentation.RightDisplayName} ({PageCount(presentation.RightPageCount)})\n\n" +
            $"Texto: {presentation.TextStatus}\n" +
            $"{presentation.VisualLabel}: {presentation.VisualStatus}\n" +
            $"Estructura: {presentation.StructureStatus}\n\n" +
            $"Resultado: {presentation.Conclusion}";
    }

    private static ComparePresentation BuildComparePresentation(
        string leftDisplayName,
        string rightDisplayName,
        PdfCompareResult result)
    {
        bool samePageCount = result.LeftPageCount == result.RightPageCount;
        bool hasDifferences = !samePageCount || result.TextChanged || result.VisualChanged;

        return new ComparePresentation(
            leftDisplayName,
            rightDisplayName,
            result.LeftPageCount,
            result.RightPageCount,
            result.TextChanged ? "Cambios detectados" : "Sin cambios",
            "Cambio visual independiente del texto",
            result.VisualChanged ? "Cambios detectados" : "Sin cambios",
            samePageCount ? "Igual cantidad de páginas" : "Cantidad de páginas diferente",
            hasDifferences ? "Se encontraron diferencias" : "Sin diferencias detectadas");
    }
}

internal sealed record ComparePresentation(
    string LeftDisplayName,
    string RightDisplayName,
    int LeftPageCount,
    int RightPageCount,
    string TextStatus,
    string VisualLabel,
    string VisualStatus,
    string StructureStatus,
    string Conclusion);
