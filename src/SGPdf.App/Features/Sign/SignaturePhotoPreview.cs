using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SGPdf.App.Features.Sign;

internal static class SignaturePhotoPreview
{
    internal const int MaxSide = 1200;

    internal static SignaturePhotoSource CreateReducedSource(SignaturePhotoSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var longest = Math.Max(source.PixelWidth, source.PixelHeight);
        if (longest <= MaxSide)
        {
            return new SignaturePhotoSource(
                source.PixelWidth,
                source.PixelHeight,
                source.Stride,
                source.BgraPixels.ToArray(),
                source.SourceName);
        }

        var scale = (double)MaxSide / longest;
        var bitmap = BitmapSource.Create(
            source.PixelWidth,
            source.PixelHeight,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            source.BgraPixels.ToArray(),
            source.Stride);
        bitmap.Freeze();

        var transformed = new TransformedBitmap(bitmap, new ScaleTransform(scale, scale));
        transformed.Freeze();

        BitmapSource normalized = transformed;
        if (normalized.Format != PixelFormats.Bgra32)
        {
            var converted = new FormatConvertedBitmap(normalized, PixelFormats.Bgra32, null, 0d);
            converted.Freeze();
            normalized = converted;
        }

        var stride = checked(normalized.PixelWidth * 4);
        var pixels = new byte[checked(stride * normalized.PixelHeight)];
        normalized.CopyPixels(pixels, stride, 0);
        return new SignaturePhotoSource(
            normalized.PixelWidth,
            normalized.PixelHeight,
            stride,
            pixels,
            source.SourceName);
    }
}

internal sealed class SignaturePhotoPreviewVersion
{
    private long _version;

    internal long BeginRequest() => Interlocked.Increment(ref _version);
    internal bool IsCurrent(long version) => Volatile.Read(ref _version) == version;
}
