namespace SGPdf.App.Printing;

public sealed record ThermalImageableAreaMm(
    double OriginX,
    double OriginY,
    double ExtentWidth,
    double ExtentHeight);

public sealed record ThermalPrinterResolution(int X, int Y);

public sealed record ThermalPrintPreflightInput(
    double RequestedWidthMm,
    double RequestedHeightMm,
    double? ValidatedWidthMm,
    double? ValidatedHeightMm,
    int RequestedDpi,
    int? ValidatedDpi,
    int? DialogCopyCount,
    ThermalImageableAreaMm? ImageableArea);

public sealed record ThermalPrintPreflightDecision(
    bool CanPrint,
    string? BlockReason,
    IReadOnlyList<string> Warnings,
    int EffectiveCopyCount);

public static class ThermalPrintPreflightPolicy
{
    public const double MediaToleranceMm = 0.5d;
    private const int ResolutionToleranceDpi = 2;

    public static ThermalPrintPreflightDecision Evaluate(ThermalPrintPreflightInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidatePositiveFinite(input.RequestedWidthMm, nameof(input.RequestedWidthMm));
        ValidatePositiveFinite(input.RequestedHeightMm, nameof(input.RequestedHeightMm));

        if (!IsPositiveFinite(input.ValidatedWidthMm) || !IsPositiveFinite(input.ValidatedHeightMm))
        {
            return Block("La impresora no devolvió un tamaño de medio válido para la etiqueta solicitada.");
        }

        if (Math.Abs(input.ValidatedWidthMm!.Value - input.RequestedWidthMm) > MediaToleranceMm ||
            Math.Abs(input.ValidatedHeightMm!.Value - input.RequestedHeightMm) > MediaToleranceMm)
        {
            return Block("El tamaño validado por la impresora no coincide con el tamaño físico solicitado.");
        }

        var warnings = new List<string>();

        if (DoesImageableAreaClip(input.ImageableArea, input.RequestedWidthMm, input.RequestedHeightMm))
        {
            warnings.Add("El área imprimible reportada no cubre toda la etiqueta; puede haber recorte.");
        }

        if (input.ValidatedDpi is null || Math.Abs(input.ValidatedDpi.Value - input.RequestedDpi) > ResolutionToleranceDpi)
        {
            warnings.Add("La impresora no confirmó una resolución equivalente a la solicitada; se usará la resolución validada por el driver.");
        }

        if (input.DialogCopyCount is > 1)
        {
            warnings.Add("SG PDF Editor controla la cantidad de etiquetas; las copias de Windows se ajustan a 1.");
        }

        return new ThermalPrintPreflightDecision(
            CanPrint: true,
            BlockReason: null,
            Warnings: warnings,
            EffectiveCopyCount: 1);
    }

    public static ThermalPrinterResolution? SelectMatchingResolution(
        int requestedDpi,
        IReadOnlyList<ThermalPrinterResolution> capabilities)
    {
        if (requestedDpi <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedDpi));

        ArgumentNullException.ThrowIfNull(capabilities);

        ThermalPrinterResolution? best = null;
        var bestScore = int.MaxValue;

        foreach (var capability in capabilities)
        {
            var xDifference = Math.Abs(capability.X - requestedDpi);
            var yDifference = Math.Abs(capability.Y - requestedDpi);
            if (xDifference > ResolutionToleranceDpi || yDifference > ResolutionToleranceDpi)
                continue;

            var score = checked(xDifference + yDifference);
            if (score >= bestScore)
                continue;

            best = capability;
            bestScore = score;
        }

        return best;
    }

    private static ThermalPrintPreflightDecision Block(string reason)
        => new(
            CanPrint: false,
            BlockReason: reason,
            Warnings: Array.Empty<string>(),
            EffectiveCopyCount: 1);

    private static bool DoesImageableAreaClip(
        ThermalImageableAreaMm? imageableArea,
        double requestedWidthMm,
        double requestedHeightMm)
    {
        if (imageableArea is null)
            return false;

        if (!double.IsFinite(imageableArea.OriginX) ||
            !double.IsFinite(imageableArea.OriginY) ||
            !double.IsFinite(imageableArea.ExtentWidth) ||
            !double.IsFinite(imageableArea.ExtentHeight) ||
            imageableArea.ExtentWidth <= 0d ||
            imageableArea.ExtentHeight <= 0d)
        {
            return true;
        }

        var coversLeft = imageableArea.OriginX <= 0d;
        var coversTop = imageableArea.OriginY <= 0d;
        var coversRight = imageableArea.OriginX + imageableArea.ExtentWidth >= requestedWidthMm;
        var coversBottom = imageableArea.OriginY + imageableArea.ExtentHeight >= requestedHeightMm;

        return !(coversLeft && coversTop && coversRight && coversBottom);
    }

    private static bool IsPositiveFinite(double? value)
        => value is > 0d && double.IsFinite(value.Value);

    private static void ValidatePositiveFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
