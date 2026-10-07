using System.Globalization;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using SGPdf.App.Features.Labels;

namespace SGPdf.App.Printing;

public static class WindowsThermalPrintWorkflow
{
    public static bool TryPrint(
        Window owner,
        LabelLayoutPlan plan,
        IReadOnlyList<ZplRenderedLabel> renderedLabels,
        int requestedDpi)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(renderedLabels);
        if (requestedDpi <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedDpi));

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
            return false;

        var queue = dialog.PrintQueue;
        var ticket = dialog.PrintTicket;
        if (queue is null || ticket is null)
        {
            MessageBox.Show(
                owner,
                "Windows no devolvió una impresora y configuración válidas.",
                "SG PDF Editor",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        var preflight = WindowsThermalPrintPreflight.Run(
            queue,
            ticket,
            plan.PageWidthMm,
            plan.PageHeightMm,
            requestedDpi);
        if (!preflight.CanPrint || preflight.ValidatedPrintTicket is null)
        {
            MessageBox.Show(
                owner,
                preflight.BlockReason ?? "La impresora no puede usar el tamaño térmico solicitado.",
                "SG PDF Editor",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        var summary = BuildSummary(
            queue.FullName,
            plan,
            preflight.ValidatedDpi,
            preflight.Warnings);
        if (MessageBox.Show(
                owner,
                summary,
                "Confirmar impresión de etiquetas",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information) != MessageBoxResult.OK)
        {
            return false;
        }

        dialog.PrintTicket = preflight.ValidatedPrintTicket;
        var paginator = new LabelPrintPaginator(plan, renderedLabels);
        dialog.PrintDocument(paginator, "SG PDF Editor — etiquetas térmicas");
        return true;
    }

    internal static string BuildSummary(
        string printerName,
        LabelLayoutPlan plan,
        int? validatedDpi,
        IReadOnlyList<string> warnings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(printerName);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(warnings);

        var builder = new StringBuilder();
        builder.AppendLine($"Impresora: {printerName}");
        builder.AppendLine($"Tamaño: {FormatMillimeters(plan.PageWidthMm)} × {FormatMillimeters(plan.PageHeightMm)} mm");
        builder.AppendLine(validatedDpi is int dpi
            ? $"Resolución: {dpi} dpi"
            : "Resolución: la validada por el driver");
        builder.AppendLine($"Etiquetas: {plan.TotalLabels.ToString(CultureInfo.CurrentCulture)}");
        builder.AppendLine("Copias de Windows: 1");
        builder.Append("Escalado SG PDF Editor: ninguno");

        if (warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine("Advertencias:");
            foreach (var warning in warnings)
                builder.AppendLine($"• {warning}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatMillimeters(double value)
        => value.ToString("0.###", CultureInfo.CurrentCulture);
}
