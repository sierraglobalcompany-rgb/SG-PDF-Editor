using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Utilities;

internal enum TextOverlayLayer
{
    Background,
    Foreground
}

internal static class UtilityOverlayColors
{
    internal static PdfTextFillColor OpaqueBlack { get; } = new(0u, 0u, 0u, 255u);

    internal static void Validate(PdfTextFillColor fillColor, string parameterName)
    {
        if (fillColor.Red > 255u || fillColor.Green > 255u ||
            fillColor.Blue > 255u || fillColor.Alpha > 255u)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Overlay color channels must be between 0 and 255.");
        }
    }
}

internal sealed record TextOverlaySettings
{
    internal TextOverlaySettings(
        string text,
        double fontSize,
        double opacity,
        TextOverlayLayer layer)
        : this(text, fontSize, opacity, layer, UtilityOverlayColors.OpaqueBlack)
    {
    }

    internal TextOverlaySettings(
        string text,
        double fontSize,
        double opacity,
        TextOverlayLayer layer,
        PdfTextFillColor fillColor)
    {
        UtilityOverlayColors.Validate(fillColor, nameof(fillColor));
        Text = text;
        FontSize = fontSize;
        Opacity = opacity;
        Layer = layer;
        FillColor = fillColor;
    }

    internal string Text { get; }
    internal double FontSize { get; }
    internal double Opacity { get; }
    internal TextOverlayLayer Layer { get; }
    internal PdfTextFillColor FillColor { get; }
}

internal sealed record TextOverlayPlacement
{
    internal TextOverlayPlacement(
        int pageIndex,
        string text,
        double fontSize,
        double opacity,
        TextOverlayLayer layer,
        PdfObjectMatrix matrix)
        : this(
            pageIndex,
            text,
            fontSize,
            opacity,
            layer,
            matrix,
            UtilityOverlayColors.OpaqueBlack)
    {
    }

    internal TextOverlayPlacement(
        int pageIndex,
        string text,
        double fontSize,
        double opacity,
        TextOverlayLayer layer,
        PdfObjectMatrix matrix,
        PdfTextFillColor fillColor)
    {
        UtilityOverlayColors.Validate(fillColor, nameof(fillColor));
        PageIndex = pageIndex;
        Text = text;
        FontSize = fontSize;
        Opacity = opacity;
        Layer = layer;
        Matrix = matrix;
        FillColor = fillColor;
    }

    internal int PageIndex { get; }
    internal string Text { get; }
    internal double FontSize { get; }
    internal double Opacity { get; }
    internal TextOverlayLayer Layer { get; }
    internal PdfObjectMatrix Matrix { get; }
    internal PdfTextFillColor FillColor { get; }
}
