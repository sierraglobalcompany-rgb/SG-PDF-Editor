using System.IO;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SGPdf.App.Features.Sign;

internal static class SignatureInkRenderer
{
    internal const double RasterScale = 300d / 96d;

    internal static bool HasUsefulInk(StrokeCollection strokes)
    {
        ArgumentNullException.ThrowIfNull(strokes);

        Rect? union = null;
        foreach (var stroke in strokes)
        {
            if (stroke.StylusPoints.Count < 2)
                continue;

            var bounds = stroke.GetBounds();
            union = union is null ? bounds : Rect.Union(union.Value, bounds);
        }

        return union is Rect useful && useful.Width >= 4d && useful.Height >= 4d;
    }

    internal static SignatureAsset Render(StrokeCollection strokes, string? sourceName = "drawn-signature")
    {
        ArgumentNullException.ThrowIfNull(strokes);
        if (!HasUsefulInk(strokes))
            throw new InvalidDataException("Dibuja una firma más grande antes de aplicar.");

        var snapshot = new StrokeCollection();
        foreach (var stroke in strokes)
            snapshot.Add(stroke.Clone());

        var bounds = snapshot.GetBounds();
        var maxStrokeWidth = snapshot.Max(stroke => Math.Max(stroke.DrawingAttributes.Width, stroke.DrawingAttributes.Height));
        var paddingDip = Math.Clamp(Math.Max(8d, maxStrokeWidth * 2d), 8d, 24d);
        var padded = new Rect(
            bounds.Left - paddingDip,
            bounds.Top - paddingDip,
            bounds.Width + (paddingDip * 2d),
            bounds.Height + (paddingDip * 2d));

        var pixelWidth = ToPixelDimension(padded.Width);
        var pixelHeight = ToPixelDimension(padded.Height);
        SignatureImageLimits.ValidatePixelCount(pixelWidth, pixelHeight);

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var transform = new Matrix(
                RasterScale,
                0d,
                0d,
                RasterScale,
                -padded.Left * RasterScale,
                -padded.Top * RasterScale);
            context.PushTransform(new MatrixTransform(transform));
            snapshot.Draw(context);
            context.Pop();
        }

        var rendered = new RenderTargetBitmap(pixelWidth, pixelHeight, 96d, 96d, PixelFormats.Pbgra32);
        rendered.Render(visual);
        rendered.Freeze();

        var normalized = new FormatConvertedBitmap(rendered, PixelFormats.Bgra32, null, 0d);
        normalized.Freeze();

        var stride = checked(pixelWidth * 4);
        var pixels = new byte[checked(stride * pixelHeight)];
        normalized.CopyPixels(pixels, stride, 0);
        return new SignatureAsset(pixelWidth, pixelHeight, stride, pixels, sourceName);
    }

    private static int ToPixelDimension(double dip)
    {
        var scaled = Math.Ceiling(dip * RasterScale);
        if (!double.IsFinite(scaled) || scaled < 1d || scaled > int.MaxValue)
            throw new InvalidDataException("La geometría de la firma no es válida.");
        return checked((int)scaled);
    }
}
