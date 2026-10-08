using System.Windows.Ink;
using System.Windows.Media;

namespace SGPdf.App.Features.Sign;

internal enum SignatureInkColor
{
    Black,
    Blue
}

internal enum SignatureInkWidth
{
    Thin,
    Medium,
    Thick
}

internal static class SignatureStrokeStyle
{
    internal static Color GetColor(SignatureInkColor color)
        => color switch
        {
            SignatureInkColor.Black => Color.FromRgb(0x00, 0x00, 0x00),
            SignatureInkColor.Blue => Color.FromRgb(0x19, 0x41, 0x96),
            _ => throw new ArgumentOutOfRangeException(nameof(color))
        };

    internal static double GetWidthDip(SignatureInkWidth width)
        => width switch
        {
            SignatureInkWidth.Thin => 2d,
            SignatureInkWidth.Medium => 3.5d,
            SignatureInkWidth.Thick => 5d,
            _ => throw new ArgumentOutOfRangeException(nameof(width))
        };

    internal static DrawingAttributes CreateDrawingAttributes(SignatureInkColor color, SignatureInkWidth width)
    {
        var size = GetWidthDip(width);
        return new DrawingAttributes
        {
            Color = GetColor(color),
            Width = size,
            Height = size,
            StylusTip = StylusTip.Ellipse,
            IgnorePressure = true
        };
    }
}
