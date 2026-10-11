using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Utilities;

internal enum TextOverlayLayer
{
    Background,
    Foreground
}

internal sealed record TextOverlaySettings(
    string Text,
    double FontSize,
    double Opacity,
    TextOverlayLayer Layer);

internal sealed record TextOverlayPlacement(
    int PageIndex,
    string Text,
    double FontSize,
    double Opacity,
    TextOverlayLayer Layer,
    PdfObjectMatrix Matrix);
