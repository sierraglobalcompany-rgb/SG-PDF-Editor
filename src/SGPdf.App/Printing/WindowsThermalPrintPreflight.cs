using System.Printing;

namespace SGPdf.App.Printing;

public sealed record WindowsThermalPrintPreflightResult(
    bool CanPrint,
    PrintTicket? ValidatedPrintTicket,
    int? ValidatedDpi,
    IReadOnlyList<string> Warnings,
    string? BlockReason);

public static class WindowsThermalPrintPreflight
{
    private const double WpfUnitsPerMillimeter = 96d / 25.4d;
    private const double MillimetersPerWpfUnit = 25.4d / 96d;

    public static WindowsThermalPrintPreflightResult Run(
        PrintQueue queue,
        PrintTicket dialogTicket,
        double requestedWidthMm,
        double requestedHeightMm,
        int requestedDpi)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(dialogTicket);

        if (!double.IsFinite(requestedWidthMm) || requestedWidthMm <= 0d)
            throw new ArgumentOutOfRangeException(nameof(requestedWidthMm));
        if (!double.IsFinite(requestedHeightMm) || requestedHeightMm <= 0d)
            throw new ArgumentOutOfRangeException(nameof(requestedHeightMm));
        if (requestedDpi <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedDpi));

        try
        {
            var capabilities = queue.GetPrintCapabilities(dialogTicket);
            var matchingResolution = SelectMatchingPageResolution(
                requestedDpi,
                capabilities.PageResolutionCapability);

            var candidate = CreateCandidateTicket(
                dialogTicket,
                requestedWidthMm,
                requestedHeightMm,
                matchingResolution);

            var validation = queue.MergeAndValidatePrintTicket(dialogTicket, candidate);
            var validatedTicket = validation.ValidatedPrintTicket;
            if (validatedTicket is null)
                return Block("El controlador de la impresora no devolvió una configuración de impresión válida.");

            var validatedCapabilities = queue.GetPrintCapabilities(validatedTicket);
            var validatedMedia = validatedTicket.PageMediaSize;
            var validatedWidthMm = validatedMedia?.Width is double width
                ? WpfUnitsToMillimeters(width)
                : (double?)null;
            var validatedHeightMm = validatedMedia?.Height is double height
                ? WpfUnitsToMillimeters(height)
                : (double?)null;
            var validatedDpi = GetRepresentativeDpi(validatedTicket.PageResolution);
            var imageableArea = ConvertImageableAreaToMillimeters(validatedCapabilities.PageImageableArea);

            var decision = ThermalPrintPreflightPolicy.Evaluate(new ThermalPrintPreflightInput(
                requestedWidthMm,
                requestedHeightMm,
                validatedWidthMm,
                validatedHeightMm,
                requestedDpi,
                validatedDpi,
                dialogTicket.CopyCount,
                imageableArea));

            return new WindowsThermalPrintPreflightResult(
                decision.CanPrint,
                decision.CanPrint ? validatedTicket : null,
                validatedDpi,
                decision.Warnings,
                decision.BlockReason);
        }
        catch (Exception ex) when (ex is PrintSystemException or InvalidOperationException or ArgumentException)
        {
            return Block($"No se pudieron validar las capacidades de la impresora. {ex.Message}");
        }
    }

    internal static PrintTicket CreateCandidateTicket(
        PrintTicket dialogTicket,
        double requestedWidthMm,
        double requestedHeightMm,
        PageResolution? matchingResolution)
    {
        ArgumentNullException.ThrowIfNull(dialogTicket);
        if (!double.IsFinite(requestedWidthMm) || requestedWidthMm <= 0d)
            throw new ArgumentOutOfRangeException(nameof(requestedWidthMm));
        if (!double.IsFinite(requestedHeightMm) || requestedHeightMm <= 0d)
            throw new ArgumentOutOfRangeException(nameof(requestedHeightMm));

        var candidate = dialogTicket.Clone();
        candidate.PageMediaSize = new PageMediaSize(
            requestedWidthMm * WpfUnitsPerMillimeter,
            requestedHeightMm * WpfUnitsPerMillimeter);
        candidate.CopyCount = 1;
        if (matchingResolution is not null)
            candidate.PageResolution = matchingResolution;

        return candidate;
    }

    internal static ThermalImageableAreaMm ConvertImageableAreaToMillimeters(
        double originWidth,
        double originHeight,
        double extentWidth,
        double extentHeight)
        => new(
            WpfUnitsToMillimeters(originWidth),
            WpfUnitsToMillimeters(originHeight),
            WpfUnitsToMillimeters(extentWidth),
            WpfUnitsToMillimeters(extentHeight));

    private static ThermalImageableAreaMm? ConvertImageableAreaToMillimeters(
        PrintDocumentImageableArea? imageableArea)
    {
        if (imageableArea is null)
            return null;

        return ConvertImageableAreaToMillimeters(
            imageableArea.OriginWidth,
            imageableArea.OriginHeight,
            imageableArea.ExtentWidth,
            imageableArea.ExtentHeight);
    }

    private static PageResolution? SelectMatchingPageResolution(
        int requestedDpi,
        IReadOnlyCollection<PageResolution> capabilities)
    {
        var usable = capabilities
            .Where(capability => capability.X.HasValue && capability.Y.HasValue)
            .Select(capability => new
            {
                Source = capability,
                Resolution = new ThermalPrinterResolution(capability.X!.Value, capability.Y!.Value)
            })
            .ToArray();

        var selected = ThermalPrintPreflightPolicy.SelectMatchingResolution(
            requestedDpi,
            usable.Select(item => item.Resolution).ToArray());
        if (selected is null)
            return null;

        return usable.First(item => item.Resolution == selected).Source;
    }

    private static int? GetRepresentativeDpi(PageResolution? resolution)
    {
        if (resolution?.X is not int x || resolution.Y is not int y)
            return null;

        return x == y ? x : (int)Math.Round((x + y) / 2d, MidpointRounding.AwayFromZero);
    }

    private static double WpfUnitsToMillimeters(double value)
        => value * MillimetersPerWpfUnit;

    private static WindowsThermalPrintPreflightResult Block(string reason)
        => new(
            CanPrint: false,
            ValidatedPrintTicket: null,
            ValidatedDpi: null,
            Warnings: Array.Empty<string>(),
            BlockReason: reason);
}
