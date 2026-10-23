using System.IO;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SGPdf.App.Features.Utilities;

internal sealed record PdfImageWatermarkAsset(
    int PixelWidth,
    int PixelHeight,
    int Stride,
    ReadOnlyMemory<byte> BgraPixels,
    ReadOnlyMemory<byte> EncodedBytes,
    string Sha256);

internal static class PdfImageWatermarkAssetLoader
{
    private const long MaximumEncodedBytes = 20L * 1024L * 1024L;
    private const int MaximumDimension = 8_000;
    private const long MaximumPixelCount = 16_000_000L;
    private const string ExifOrientationQuery = "/app1/ifd/{ushort=274}";
    private static ReadOnlySpan<byte> PngSignature => [137, 80, 78, 71, 13, 10, 26, 10];

    internal static PdfImageWatermarkAsset Load(
        string path,
        double opacityPercent,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!double.IsFinite(opacityPercent) || opacityPercent is < 10d or > 100d)
            throw new ArgumentOutOfRangeException(nameof(opacityPercent), "La opacidad debe estar entre 10 % y 100 %.");

        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg"))
            throw new NotSupportedException("Selecciona una imagen PNG, JPG o JPEG.");

        cancellationToken.ThrowIfCancellationRequested();
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length > MaximumEncodedBytes)
            throw new InvalidDataException("La imagen supera el límite de 20 MiB.");
        if (file.Length <= 0)
            throw new InvalidDataException("El archivo de imagen está vacío.");

        var encodedBytes = new byte[checked((int)file.Length)];
        file.ReadExactly(encodedBytes);
        cancellationToken.ThrowIfCancellationRequested();

        var isPng = encodedBytes.AsSpan().StartsWith(PngSignature);
        var isJpeg = encodedBytes.Length >= 3 &&
                     encodedBytes[0] == 0xff && encodedBytes[1] == 0xd8 && encodedBytes[2] == 0xff;
        if ((extension == ".png" && !isPng) ||
            (extension is ".jpg" or ".jpeg" && !isJpeg))
        {
            throw new InvalidDataException("El contenido de la imagen no coincide con el formato indicado por su extensión.");
        }

        try
        {
            using var encodedStream = new MemoryStream(encodedBytes, writable: false);
            var decoder = BitmapDecoder.Create(
                encodedStream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.None);
            if (decoder.Frames.Count == 0)
                throw new InvalidDataException("La imagen no contiene un cuadro decodificable.");

            var frame = decoder.Frames[0];
            var width = frame.PixelWidth;
            var height = frame.PixelHeight;
            ValidateDimensions(width, height);
            if (isJpeg && GetExifOrientation(frame) != 1)
                throw new InvalidDataException("Esta versión no admite JPEG con orientación EXIF distinta de 1.");

            cancellationToken.ThrowIfCancellationRequested();
            BitmapSource bgra = frame.Format == PixelFormats.Bgra32
                ? frame
                : new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0d);
            bgra.Freeze();

            var stride = checked(width * 4);
            var pixels = new byte[checked(stride * height)];
            bgra.CopyPixels(pixels, stride, 0);
            for (var offset = 3; offset < pixels.Length; offset += 4)
            {
                if ((offset & 0x3ffff) == 3)
                    cancellationToken.ThrowIfCancellationRequested();
                var originalAlpha = pixels[offset];
                var effectiveAlpha = (byte)Math.Clamp(
                    (int)Math.Round(originalAlpha * opacityPercent / 100d, MidpointRounding.AwayFromZero),
                    0,
                    255);
                pixels[offset] = effectiveAlpha;
            }

            cancellationToken.ThrowIfCancellationRequested();
            return new PdfImageWatermarkAsset(
                width,
                height,
                stride,
                pixels,
                encodedBytes,
                Convert.ToHexString(SHA256.HashData(encodedBytes)));
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is FileFormatException or NotSupportedException or ArgumentException or
                InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            throw new InvalidDataException("No se pudo decodificar la imagen seleccionada.", exception);
        }
    }

    private static void ValidateDimensions(int width, int height)
    {
        if (width <= 0 || height <= 0 || width > MaximumDimension || height > MaximumDimension)
            throw new InvalidDataException("La imagen supera el límite de 8.000 píxeles por lado.");
        if (checked((long)width * height) > MaximumPixelCount)
            throw new InvalidDataException("La imagen supera el límite de 16 millones de píxeles.");
    }

    private static int GetExifOrientation(BitmapFrame frame)
    {
        if (frame.Metadata is not BitmapMetadata metadata || !metadata.ContainsQuery(ExifOrientationQuery))
            return 1;

        object? value = metadata.GetQuery(ExifOrientationQuery);
        var orientation = value switch
        {
            ushort number => number,
            short number => number,
            uint number => checked((int)number),
            int number => number,
            _ => 0
        };
        if (orientation is < 1 or > 8)
            throw new InvalidDataException("La orientación EXIF del JPEG no es válida.");
        return orientation;
    }
}
