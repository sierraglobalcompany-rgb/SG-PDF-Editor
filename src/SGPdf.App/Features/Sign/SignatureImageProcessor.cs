using System.IO;

namespace SGPdf.App.Features.Sign;

internal static class SignatureImageProcessor
{
    private const int UsableAlpha = 64;
    private const int CropAlpha = 16;

    internal static SignaturePaperColor EstimatePaper(SignaturePhotoSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var width = source.PixelWidth;
        var height = source.PixelHeight;
        var band = Math.Max(1, (int)Math.Round(Math.Min(width, height) * 0.05, MidpointRounding.AwayFromZero));
        band = Math.Min(band, Math.Max(1, (Math.Min(width, height) + 1) / 2));

        var rs = new List<byte>();
        var gs = new List<byte>();
        var bs = new List<byte>();
        var pixels = source.BgraPixels.Span;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (x >= band && x < width - band && y >= band && y < height - band)
                    continue;

                var offset = y * source.Stride + x * 4;
                var b = pixels[offset];
                var g = pixels[offset + 1];
                var r = pixels[offset + 2];
                var a = pixels[offset + 3];
                if (a < 32)
                    continue;

                var luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;
                if (luminance < 180d)
                    continue;

                rs.Add(r);
                gs.Add(g);
                bs.Add(b);
            }
        }

        if (rs.Count < 8)
            throw new InvalidDataException("No se detectó un fondo de papel claro suficiente para preparar la firma.");

        rs.Sort();
        gs.Sort();
        bs.Sort();
        return new SignaturePaperColor(Median(rs), Median(gs), Median(bs));
    }

    internal static SignatureAsset Process(
        SignaturePhotoSource source,
        SignatureImageProcessingSettings settings,
        SignaturePaperColor? paper = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        settings.Validate();
        var paperColor = paper ?? EstimatePaper(source);

        var width = source.PixelWidth;
        var height = source.PixelHeight;
        var stride = checked(width * 4);
        var output = new byte[checked(stride * height)];
        var input = source.BgraPixels.Span;

        var low = 8d + settings.BackgroundRemoval * 0.32d;
        var high = low + 32d;
        var brightnessDelta = (int)Math.Round(settings.Brightness * 255d / 100d, MidpointRounding.AwayFromZero);
        var contrastFactor = (100d + settings.Contrast) / 100d;

        var usableCount = 0;
        var usableMinX = width;
        var usableMinY = height;
        var usableMaxX = -1;
        var usableMaxY = -1;
        var cropMinX = width;
        var cropMinY = height;
        var cropMaxX = -1;
        var cropMaxY = -1;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sourceOffset = y * source.Stride + x * 4;
                var targetOffset = y * stride + x * 4;
                var b = input[sourceOffset];
                var g = input[sourceOffset + 1];
                var r = input[sourceOffset + 2];
                var originalA = input[sourceOffset + 3];

                var dr = r - paperColor.R;
                var dg = g - paperColor.G;
                var db = b - paperColor.B;
                var distance = Math.Sqrt(dr * dr + dg * dg + db * db);
                var t = Math.Clamp((distance - low) / (high - low), 0d, 1d);
                var cleanup = t * t * (3d - 2d * t);
                var alpha = ClampByte(Math.Round(originalA * cleanup, MidpointRounding.AwayFromZero));

                var adjustedR = AdjustChannel(r, brightnessDelta, contrastFactor);
                var adjustedG = AdjustChannel(g, brightnessDelta, contrastFactor);
                var adjustedB = AdjustChannel(b, brightnessDelta, contrastFactor);

                switch (settings.InkStyle)
                {
                    case SignatureInkStyle.Black:
                        adjustedR = adjustedG = adjustedB = 0;
                        break;
                    case SignatureInkStyle.Blue:
                        adjustedR = 25;
                        adjustedG = 65;
                        adjustedB = 150;
                        break;
                }

                output[targetOffset] = adjustedB;
                output[targetOffset + 1] = adjustedG;
                output[targetOffset + 2] = adjustedR;
                output[targetOffset + 3] = alpha;

                if (alpha >= CropAlpha)
                {
                    cropMinX = Math.Min(cropMinX, x);
                    cropMinY = Math.Min(cropMinY, y);
                    cropMaxX = Math.Max(cropMaxX, x);
                    cropMaxY = Math.Max(cropMaxY, y);
                }

                if (alpha >= UsableAlpha)
                {
                    usableCount++;
                    usableMinX = Math.Min(usableMinX, x);
                    usableMinY = Math.Min(usableMinY, y);
                    usableMaxX = Math.Max(usableMaxX, x);
                    usableMaxY = Math.Max(usableMaxY, y);
                }
            }
        }

        var usableWidth = usableMaxX >= usableMinX ? usableMaxX - usableMinX + 1 : 0;
        var usableHeight = usableMaxY >= usableMinY ? usableMaxY - usableMinY + 1 : 0;
        if (usableCount < 32 || usableWidth < 4 || usableHeight < 4)
        {
            throw new InvalidDataException(
                "No se pudo aislar una firma útil. Intenta una foto sobre papel blanco, con buena luz y sin sombras fuertes.");
        }

        if (!settings.AutoCrop)
            return new SignatureAsset(width, height, stride, output, source.SourceName);

        if (cropMaxX < cropMinX || cropMaxY < cropMinY)
            throw new InvalidDataException("No se pudo aislar una firma útil.");

        var contentWidth = cropMaxX - cropMinX + 1;
        var contentHeight = cropMaxY - cropMinY + 1;
        var padding = Math.Clamp(
            (int)Math.Round(Math.Max(contentWidth, contentHeight) * 0.02d, MidpointRounding.AwayFromZero),
            4,
            32);

        var left = Math.Max(0, cropMinX - padding);
        var top = Math.Max(0, cropMinY - padding);
        var right = Math.Min(width - 1, cropMaxX + padding);
        var bottom = Math.Min(height - 1, cropMaxY + padding);
        var croppedWidth = right - left + 1;
        var croppedHeight = bottom - top + 1;
        var croppedStride = checked(croppedWidth * 4);
        var cropped = new byte[checked(croppedStride * croppedHeight)];

        for (var y = 0; y < croppedHeight; y++)
        {
            Buffer.BlockCopy(
                output,
                (top + y) * stride + left * 4,
                cropped,
                y * croppedStride,
                croppedStride);
        }

        return new SignatureAsset(croppedWidth, croppedHeight, croppedStride, cropped, source.SourceName);
    }

    private static byte Median(List<byte> values)
    {
        var middle = values.Count / 2;
        if ((values.Count & 1) == 1)
            return values[middle];
        return (byte)((values[middle - 1] + values[middle]) / 2);
    }

    private static byte AdjustChannel(byte channel, int brightnessDelta, double contrastFactor)
    {
        var bright = Math.Clamp(channel + brightnessDelta, 0, 255);
        var contrasted = 128d + (bright - 128d) * contrastFactor;
        return ClampByte(Math.Round(contrasted, MidpointRounding.AwayFromZero));
    }

    private static byte ClampByte(double value) => (byte)Math.Clamp((int)value, 0, 255);
}
