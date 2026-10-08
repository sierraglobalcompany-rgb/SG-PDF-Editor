using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SGPdf.App.Features.Sign;

internal static class SignaturePhotoLoader
{
    internal static SignaturePhotoSource Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("La ruta de la foto de firma es obligatoria.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("No se encontró la imagen de firma.", path);

        var extension = Path.GetExtension(path);
        if (!extension.Equals(".png", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("La preparación de firma admite archivos PNG, JPG o JPEG.");
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);
        if (decoder.Frames.Count == 0)
            throw new InvalidDataException("La imagen no contiene un fotograma válido.");

        var frame = decoder.Frames[0];
        SignatureImageLimits.ValidatePixelCount(frame.PixelWidth, frame.PixelHeight);

        BitmapSource source = frame;
        if (source.Format != PixelFormats.Bgra32)
        {
            var converted = new FormatConvertedBitmap(
                source,
                PixelFormats.Bgra32,
                null,
                0d);
            converted.Freeze();
            source = converted;
        }

        var stride = checked(frame.PixelWidth * 4);
        var pixels = new byte[checked(stride * frame.PixelHeight)];
        source.CopyPixels(pixels, stride, 0);

        return new SignaturePhotoSource(
            frame.PixelWidth,
            frame.PixelHeight,
            stride,
            pixels,
            Path.GetFileName(path));
    }
}
