using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SGPdf.App.Features.Edit.Images;

internal static class ImageReplacementAssetLoader
{
    private static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    internal static ImageReplacementAsset Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("La ruta de la imagen es obligatoria.", nameof(path));

        var format = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => ImageReplacementFormat.Png,
            ".jpg" or ".jpeg" => ImageReplacementFormat.Jpeg,
            _ => throw new NotSupportedException("Solo se admiten imágenes PNG, JPG o JPEG.")
        };

        var encodedBytes = File.ReadAllBytes(path);
        var detectedFormat = DetectFormat(encodedBytes);
        if (detectedFormat is null || detectedFormat != format)
            throw new InvalidDataException("El contenido de la imagen no coincide con un PNG o JPEG válido.");

        try
        {
            using var stream = new MemoryStream(encodedBytes, writable: false);
            var decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count == 0)
                throw new InvalidDataException("La imagen no contiene ningún cuadro decodificable.");

            var frame = decoder.Frames[0];
            BitmapSource bgra = frame.Format == PixelFormats.Bgra32
                ? frame
                : new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0d);

            var width = bgra.PixelWidth;
            var height = bgra.PixelHeight;
            if (width <= 0 || height <= 0)
                throw new InvalidDataException("La imagen tiene dimensiones inválidas.");

            var stride = checked(width * 4);
            var pixels = new byte[checked(stride * height)];
            bgra.CopyPixels(pixels, stride, 0);

            var hasAlpha = false;
            for (var index = 3; index < pixels.Length; index += 4)
            {
                if (pixels[index] < byte.MaxValue)
                {
                    hasAlpha = true;
                    break;
                }
            }

            return new ImageReplacementAsset(
                format,
                encodedBytes,
                width,
                height,
                stride,
                pixels,
                hasAlpha);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex) when (ex is FormatException or NotSupportedException or ArgumentException or InvalidOperationException)
        {
            throw new InvalidDataException("No se pudo decodificar la imagen seleccionada.", ex);
        }
    }

    private static ImageReplacementFormat? DetectFormat(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= PngSignature.Length && bytes[..PngSignature.Length].SequenceEqual(PngSignature))
            return ImageReplacementFormat.Png;

        if (bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff)
            return ImageReplacementFormat.Jpeg;

        return null;
    }
}
