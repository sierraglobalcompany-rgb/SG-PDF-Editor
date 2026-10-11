using SGPdf.App.Features.Edit.Text;

namespace SGPdf.App.Features.Utilities;

internal enum TextOverlayLayer
{
    Background,
    Foreground
}

internal sealed record TextOverlaySettings(
    UtilityPageRange PageRange,
    double FontSize,
    PdfTextFillColor FillColor,
    double RotationDegrees,
    TextOverlayLayer Layer);
