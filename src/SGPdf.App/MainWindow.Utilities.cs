using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool UtilitiesLoadedHookRegistered = RegisterUtilitiesLoadedHook();

    private bool _utilityUiInitialized;
    private bool _utilityExecuting;

    private static bool RegisterUtilitiesLoadedHook()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(UtilitiesHost_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void UtilitiesHost_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializeUtilitiesUi();
    }

    private void InitializeUtilitiesUi()
    {
        if (_utilityUiInitialized)
            return;

        _utilityUiInitialized = true;
        _ = UtilitiesLoadedHookRegistered;

        var menu = (Content as DockPanel)?.Children.OfType<Menu>().FirstOrDefault()
            ?? throw new InvalidOperationException("No se encontró el menú principal para Herramientas.");

        var utilities = new MenuItem
        {
            Name = "UtilitiesMenuItem",
            Header = "_Herramientas"
        };
        var watermark = new MenuItem
        {
            Name = "WatermarkTextMenuItem",
            Header = "Marca de agua de _texto..."
        };
        var numbering = new MenuItem
        {
            Name = "PageNumberingMenuItem",
            Header = "_Numerar páginas / Bates..."
        };

        watermark.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(IsEnabled)) { Source = PrintPdfMenuItem });
        numbering.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(IsEnabled)) { Source = PrintPdfMenuItem });
        watermark.Click += WatermarkText_Click;
        numbering.Click += PageNumbering_Click;
        utilities.Items.Add(watermark);
        utilities.Items.Add(numbering);
        menu.Items.Add(utilities);

        RegisterName(utilities.Name, utilities);
        RegisterName(watermark.Name, watermark);
        RegisterName(numbering.Name, numbering);
    }

    private void WatermarkText_Click(object sender, RoutedEventArgs e)
    {
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede ejecutar la utilidad en el estado actual del editor.";
            return;
        }

        var dialog = new WatermarkDialog(_session!.PageCount, _navigation!.CurrentPageNumber)
        {
            Owner = this
        };
        if (dialog.ShowDialog() != true || dialog.Request is null)
        {
            StatusText.Text = "Marca de agua cancelada.";
            return;
        }

        var destination = SelectUtilityDestination("Guardar PDF con marca de agua como", "marca-agua.pdf");
        TryRunWatermarkUtility(dialog.Request, destination);
    }

    private void PageNumbering_Click(object sender, RoutedEventArgs e)
    {
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede ejecutar la utilidad en el estado actual del editor.";
            return;
        }

        var dialog = new PageNumberingDialog(_session!.PageCount, _navigation!.CurrentPageNumber)
        {
            Owner = this
        };
        if (dialog.ShowDialog() != true || dialog.Request is null)
        {
            StatusText.Text = "Numeración de páginas cancelada.";
            return;
        }

        var destination = SelectUtilityDestination("Guardar PDF numerado como", "numerado.pdf");
        TryRunPageNumberingUtility(dialog.Request, destination);
    }

    private bool TryRunWatermarkUtility(WatermarkUtilityRequest request, string? destinationPath)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede ejecutar la utilidad en el estado actual del editor.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            StatusText.Text = "Marca de agua cancelada: no se seleccionó un destino.";
            return false;
        }

        if (!request.TryBuild(
                _session!.PageCount,
                _navigation!.CurrentPageIndex,
                out var settings,
                out var range,
                out var anchor,
                out var validationError))
        {
            StatusText.Text = validationError ?? "Los valores de la marca de agua no son válidos.";
            return false;
        }

        _utilityExecuting = true;
        try
        {
            var sourcePath = _session.FilePath;
            var pageSizes = _session.GetPageSizes(CancellationToken.None);
            var placements = new WatermarkPlanner().Plan(
                settings!,
                range!,
                pageSizes,
                anchor,
                request.OffsetXPoints,
                request.OffsetYPoints,
                request.RotationDegrees);
            var fingerprint = PdfEditSourceFingerprint.Capture(sourcePath);
            var result = new PdfTextOverlayWriter().SaveAsCopy(
                sourcePath,
                fingerprint,
                placements,
                destinationPath,
                CancellationToken.None);

            if (!result.Succeeded)
            {
                StatusText.Text = $"No se pudo guardar la marca de agua: {result.Error}";
                return false;
            }

            StatusText.Text = $"Copia con marca de agua guardada: {destinationPath}";
            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo guardar la marca de agua: {ex.Message}";
            return false;
        }
        finally
        {
            _utilityExecuting = false;
        }
    }

    private bool TryRunPageNumberingUtility(PageNumberingUtilityRequest request, string? destinationPath)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede ejecutar la utilidad en el estado actual del editor.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            StatusText.Text = "Numeración de páginas cancelada: no se seleccionó un destino.";
            return false;
        }

        if (!request.TryBuild(
                _session!.PageCount,
                _navigation!.CurrentPageIndex,
                out var settings,
                out var range,
                out var anchor,
                out var validationError))
        {
            StatusText.Text = validationError ?? "Los valores de numeración no son válidos.";
            return false;
        }

        _utilityExecuting = true;
        try
        {
            var sourcePath = _session.FilePath;
            var pageSizes = _session.GetPageSizes(CancellationToken.None);
            var placements = new PageNumberingPlanner().Plan(
                settings!,
                range!,
                pageSizes,
                anchor,
                request.FontSize,
                request.Opacity,
                request.Background ? TextOverlayLayer.Background : TextOverlayLayer.Foreground,
                request.OffsetXPoints,
                request.OffsetYPoints);
            var fingerprint = PdfEditSourceFingerprint.Capture(sourcePath);
            var result = new PdfTextOverlayWriter().SaveAsCopy(
                sourcePath,
                fingerprint,
                placements,
                destinationPath,
                CancellationToken.None);

            if (!result.Succeeded)
            {
                StatusText.Text = $"No se pudo guardar la numeración: {result.Error}";
                return false;
            }

            StatusText.Text = $"Copia numerada guardada: {destinationPath}";
            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo guardar la numeración: {ex.Message}";
            return false;
        }
        finally
        {
            _utilityExecuting = false;
        }
    }

    private bool CanRunUtility()
        => _session is not null &&
           _navigation is not null &&
           !_isBusy &&
           !_utilityExecuting &&
           !_signatureModeActive &&
           !_editModeActive &&
           !_organizeModeActive &&
           _zplDocument is null;

    private string? SelectUtilityDestination(string title, string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = "Archivo PDF (*.pdf)|*.pdf",
            AddExtension = true,
            DefaultExt = ".pdf",
            OverwritePrompt = true,
            FileName = suggestedFileName
        };
        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }
}
