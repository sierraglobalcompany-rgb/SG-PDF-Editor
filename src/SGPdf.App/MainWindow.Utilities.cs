using System.IO;
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

        var utilities = UtilitiesMenuItem;
        var watermark = new MenuItem
        {
            Name = "WatermarkTextMenuItem",
            Header = "Marca de agua de _texto..."
        };
        var batchWatermark = new MenuItem
        {
            Name = "BatchWatermarkMenuItem",
            Header = "Marca de agua por _lotes..."
        };
        var numbering = new MenuItem
        {
            Name = "PageNumberingMenuItem",
            Header = "_Numerar páginas / Bates..."
        };
        var protection = new MenuItem
        {
            Name = "ProtectionMenuItem",
            Header = "_Proteger con contraseña..."
        };
        var ocr = new MenuItem
        {
            Name = "OcrMenuItem",
            Header = "OCR / Hacer PDF buscable…"
        };
        var qrCode = new MenuItem
        {
            Name = "QrCodeMenuItem",
            Header = "Generar código _QR..."
        };
        var pdfToPng = new MenuItem
        {
            Name = "PdfToPngMenuItem",
            Header = "Exportar PDF a _PNG..."
        };
        var pdfToJpg = new MenuItem
        {
            Name = "PdfToJpgMenuItem",
            Header = "Exportar PDF a _JPG..."
        };
        var compression = new MenuItem
        {
            Name = "CompressPdfMenuItem",
            Header = "_Comprimir PDF..."
        };
        var unlock = new MenuItem
        {
            Name = "UnlockPdfMenuItem",
            Header = "Desbloquear PDF con contraseña..."
        };
        var crop = new MenuItem
        {
            Name = "CropPdfMenuItem",
            Header = "Recortar área visible..."
        };
        var imageWatermark = new MenuItem
        {
            Name = "ImageWatermarkMenuItem",
            Header = "Marca de agua de _imagen..."
        };

        watermark.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(IsEnabled)) { Source = PrintPdfMenuItem });
        batchWatermark.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(IsEnabled)) { Source = OpenPdfMenuItem });
        numbering.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(IsEnabled)) { Source = PrintPdfMenuItem });
        protection.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(IsEnabled)) { Source = PrintPdfMenuItem });
        ocr.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(IsEnabled)) { Source = PrintPdfMenuItem });
        pdfToPng.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(IsEnabled)) { Source = PrintPdfMenuItem });
        pdfToJpg.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(IsEnabled)) { Source = PrintPdfMenuItem });
        compression.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(IsEnabled)) { Source = PrintPdfMenuItem });
        unlock.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(IsEnabled)) { Source = PrintPdfMenuItem });
        crop.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(IsEnabled)) { Source = PrintPdfMenuItem });
        imageWatermark.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(IsEnabled)) { Source = PrintPdfMenuItem });
        watermark.Click += WatermarkText_Click;
        batchWatermark.Click += BatchWatermark_Click;
        numbering.Click += PageNumbering_Click;
        protection.Click += Protection_Click;
        ocr.Click += Ocr_Click;
        qrCode.Click += QrCode_Click;
        pdfToPng.Click += ExportPdfToPng_Click;
        pdfToJpg.Click += ExportPdfToJpg_Click;
        compression.Click += CompressPdf_Click;
        unlock.Click += UnlockPdf_Click;
        crop.Click += CropPdf_Click;
        imageWatermark.Click += ImageWatermark_Click;
        utilities.Items.Add(watermark);
        utilities.Items.Add(batchWatermark);
        utilities.Items.Add(numbering);
        utilities.Items.Add(protection);
        utilities.Items.Add(ocr);
        utilities.Items.Add(qrCode);
        utilities.Items.Add(pdfToPng);
        utilities.Items.Add(pdfToJpg);
        utilities.Items.Add(compression);
        utilities.Items.Add(unlock);
        utilities.Items.Add(crop);
        utilities.Items.Add(imageWatermark);

        RegisterName(watermark.Name, watermark);
        RegisterName(batchWatermark.Name, batchWatermark);
        RegisterName(numbering.Name, numbering);
        RegisterName(protection.Name, protection);
        RegisterName(ocr.Name, ocr);
        RegisterName(qrCode.Name, qrCode);
        RegisterName(pdfToPng.Name, pdfToPng);
        RegisterName(pdfToJpg.Name, pdfToJpg);
        RegisterName(compression.Name, compression);
        RegisterName(unlock.Name, unlock);
        RegisterName(crop.Name, crop);
        RegisterName(imageWatermark.Name, imageWatermark);
        UpdateUtilityCommandAvailability();
    }

    private async void CompressPdf_Click(object sender, RoutedEventArgs e)
    {
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede ejecutar la utilidad en el estado actual del editor.";
            return;
        }

        var sourceName = Path.GetFileNameWithoutExtension(_session!.FilePath);
        var destination = SelectUtilityDestination(
            "Guardar copia comprimida como",
            $"{sourceName}-comprimido.pdf",
            overwritePrompt: false);
        await TryRunPdfCompressionUtilityAsync(destination);
    }

    private async Task<bool> TryRunPdfCompressionUtilityAsync(string? destinationPath)
    {
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede ejecutar la utilidad en el estado actual del editor.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            StatusText.Text = "Compresión cancelada.";
            return false;
        }

        _utilityExecuting = true;
        UpdateUtilityCommandAvailability();
        StatusText.Text = "Comprimiendo una copia del PDF...";
        try
        {
            var sourceSession = _session!;
            var result = await new PdfCompressionCoordinator().CompressCopyAsync(
                sourceSession,
                destinationPath,
                CancellationToken.None);

            if (!result.Succeeded)
            {
                StatusText.Text = result.NoReduction
                    ? "No se obtuvo reducción; el original no cambió."
                    : $"No se pudo comprimir el PDF: {result.Error}";
                return false;
            }

            var savings = result.SourceLength - result.OutputLength;
            var savingsPercent = result.SourceLength > 0
                ? savings * 100d / result.SourceLength
                : 0d;
            StatusText.Text =
                $"Copia comprimida guardada: {Path.GetFileName(destinationPath)} " +
                $"({result.SourceLength:N0} → {result.OutputLength:N0} bytes; ahorro {savingsPercent:N1}%).";
            return true;
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Compresión cancelada.";
            return false;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo comprimir el PDF: {ex.Message}";
            return false;
        }
        finally
        {
            _utilityExecuting = false;
            UpdateUtilityCommandAvailability();
        }
    }

    private void UpdateCompressionCommandAvailability()
        => UpdateUtilityCommandAvailability();

    private void UpdateUtilityCommandAvailability()
    {
        foreach (var name in new[] { "CompressPdfMenuItem", "UnlockPdfMenuItem", "CropPdfMenuItem", "ImageWatermarkMenuItem" })
        {
            if (FindName(name) is MenuItem command)
            {
                command.SetCurrentValue(
                    IsEnabledProperty,
                    PrintPdfMenuItem.IsEnabled && CanRunUtility());
            }
        }
    }

    private async void UnlockPdf_Click(object sender, RoutedEventArgs e)
    {
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede ejecutar la utilidad en el estado actual del editor.";
            return;
        }

        var dialog = new PdfUnlockDialog { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Request is null)
        {
            StatusText.Text = "Desbloqueo cancelado.";
            return;
        }

        var destination = SelectUtilityDestination(
            "Guardar copia desbloqueada como",
            $"{Path.GetFileNameWithoutExtension(_session!.FilePath)}-sin-contrasena.pdf",
            overwritePrompt: false);
        if (string.IsNullOrWhiteSpace(destination))
        {
            dialog.Request.Clear();
            StatusText.Text = "Desbloqueo cancelado: no se seleccionó un destino.";
            return;
        }

        var request = dialog.Request;
        var password = request.Password;
        SetUtilityExecuting(true);
        StatusText.Text = "Verificando el PDF y creando una copia sin contraseña...";
        try
        {
            var result = await new PdfUnlockCoordinator().CreateUnlockedCopyAsync(
                _session!.FilePath,
                destination,
                password,
                request.AuthorizationConfirmed,
                CancellationToken.None);
            StatusText.Text = result.Succeeded
                ? $"Copia sin contraseña guardada: {Path.GetFileName(destination)}"
                : $"No se pudo desbloquear el PDF: {result.Error}";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Desbloqueo cancelado.";
        }
        catch (Exception)
        {
            StatusText.Text = "No se pudo completar el desbloqueo; el original quedó intacto.";
        }
        finally
        {
            password = string.Empty;
            request.Clear();
            SetUtilityExecuting(false);
        }
    }

    private async void CropPdf_Click(object sender, RoutedEventArgs e)
    {
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede ejecutar la utilidad en el estado actual del editor.";
            return;
        }

        var pageSize = _session!.GetPageSize(0);
        var dialog = new PdfCropDialog(pageSize.Width, pageSize.Height) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Request is null)
        {
            StatusText.Text = "Recorte cancelado.";
            return;
        }

        var destination = SelectUtilityDestination(
            "Guardar PDF recortado como",
            $"{Path.GetFileNameWithoutExtension(_session.FilePath)}-recortado.pdf",
            overwritePrompt: false);
        if (string.IsNullOrWhiteSpace(destination))
        {
            StatusText.Text = "Recorte cancelado: no se seleccionó un destino.";
            return;
        }

        SetUtilityExecuting(true);
        StatusText.Text = "Verificando el PDF y creando la copia recortada...";
        try
        {
            var result = await new PdfCropCoordinator().CreateCroppedCopyAsync(
                _session,
                destination,
                dialog.Request,
                CancellationToken.None);
            StatusText.Text = result.Succeeded
                ? $"Copia recortada guardada: {Path.GetFileName(destination)}"
                : $"No se pudo recortar el PDF: {result.Error}";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Recorte cancelado.";
        }
        catch (Exception)
        {
            StatusText.Text = "No se pudo validar el recorte seguro; el original quedó intacto.";
        }
        finally
        {
            SetUtilityExecuting(false);
        }
    }

    private async void ImageWatermark_Click(object sender, RoutedEventArgs e)
    {
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede ejecutar la utilidad en el estado actual del editor.";
            return;
        }

        var sourceSession = _session!;
        var dialog = new ImageWatermarkDialog(sourceSession.PageCount, _navigation!.CurrentPageIndex)
        {
            Owner = this
        };
        if (dialog.ShowDialog() != true || dialog.Request is null)
        {
            StatusText.Text = "Marca de agua cancelada.";
            return;
        }

        var request = dialog.Request;
        var destination = SelectUtilityDestination(
            "Guardar copia con marca de agua de imagen como",
            $"{Path.GetFileNameWithoutExtension(sourceSession.FilePath)}-marca-imagen.pdf",
            overwritePrompt: false);
        if (string.IsNullOrWhiteSpace(destination))
        {
            StatusText.Text = "Marca de agua cancelada: no se seleccionó un destino.";
            return;
        }

        SetUtilityExecuting(true);
        StatusText.Text = "Validando la imagen y creando una copia con marca de agua...";
        try
        {
            var result = await new PdfImageWatermarkCoordinator().CreateCopyAsync(
                sourceSession.FilePath,
                request.ImagePath,
                destination,
                request.Range,
                request.WidthPercent,
                request.OpacityPercent,
                request.Anchor,
                CancellationToken.None);
            StatusText.Text = result.Succeeded
                ? $"Copia con marca de agua guardada: {Path.GetFileName(destination)}"
                : $"No se pudo crear la copia con marca de agua: {result.Error}";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Marca de agua cancelada.";
        }
        catch (Exception)
        {
            StatusText.Text = "No se pudo validar la copia con marca de agua; el PDF original quedó intacto.";
        }
        finally
        {
            SetUtilityExecuting(false);
        }
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

    private async void Protection_Click(object sender, RoutedEventArgs e)
    {
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede ejecutar la utilidad en el estado actual del editor.";
            return;
        }

        var dialog = new ProtectionDialog(_session!.OpenedWithPassword)
        {
            Owner = this
        };
        if (dialog.ShowDialog() != true || dialog.Request is null)
        {
            StatusText.Text = "Protección cancelada.";
            return;
        }

        var destination = SelectUtilityDestination("Guardar PDF protegido como", "protegido.pdf");
        await TryRunProtectionUtilityAsync(dialog.Request, destination);
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

        SetUtilityExecuting(true);
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
            SetUtilityExecuting(false);
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

        SetUtilityExecuting(true);
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
            SetUtilityExecuting(false);
        }
    }

    private async Task<bool> TryRunProtectionUtilityAsync(
        PdfProtectionRequest request,
        string? destinationPath)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!CanRunUtility())
        {
            StatusText.Text = "No se puede ejecutar la utilidad en el estado actual del editor.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            StatusText.Text = "Protección cancelada: no se seleccionó un destino.";
            return false;
        }

        if (!request.TryBuild(_session!.OpenedWithPassword, out var settings, out var validationError))
        {
            StatusText.Text = validationError ?? "Los datos de protección no son válidos.";
            return false;
        }

        SetUtilityExecuting(true);
        try
        {
            var sourcePath = _session.FilePath;
            var fingerprint = PdfEditSourceFingerprint.Capture(sourcePath);
            var result = await new PdfProtectionWriter().SaveAsProtectedCopyAsync(
                sourcePath,
                fingerprint,
                settings!,
                destinationPath,
                CancellationToken.None);

            if (!result.Succeeded)
            {
                StatusText.Text = $"No se pudo guardar la copia protegida: {result.Error}";
                return false;
            }

            StatusText.Text = $"Copia protegida guardada: {destinationPath}";
            return true;
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Protección cancelada.";
            return false;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo guardar la copia protegida: {ex.Message}";
            return false;
        }
        finally
        {
            SetUtilityExecuting(false);
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

    private void SetUtilityExecuting(bool value)
    {
        _utilityExecuting = value;
        UpdateUtilityCommandAvailability();
    }

    private string? SelectUtilityDestination(
        string title,
        string suggestedFileName,
        bool overwritePrompt = true)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = "Archivo PDF (*.pdf)|*.pdf",
            AddExtension = true,
            DefaultExt = ".pdf",
            OverwritePrompt = overwritePrompt,
            FileName = suggestedFileName
        };
        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }
}
