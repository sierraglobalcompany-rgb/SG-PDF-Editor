using System.Buffers.Binary;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SGPdf.App.Features.Sign;

internal static class SignaturePngLoader
{
    internal const long MaxDecodedPixels = 20_000_000;
    private static ReadOnlySpan<byte> PngSignature => new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

    internal static SignatureAsset Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("La ruta de la firma es obligatoria.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("No se encontró la imagen de firma.", path);

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var (headerWidth, headerHeight) = ReadAndValidateHeader(stream);
        stream.Position = 0;

        var decoder = new PngBitmapDecoder(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);
        if (decoder.Frames.Count == 0)
            throw new InvalidDataException("El PNG no contiene una imagen válida.");

        var frame = decoder.Frames[0];
        if (frame.PixelWidth <= 0 || frame.PixelHeight <= 0 ||
            frame.PixelWidth != headerWidth || frame.PixelHeight != headerHeight)
        {
            throw new InvalidDataException("Las dimensiones del PNG no son válidas.");
        }

        ValidatePixelCount(frame.PixelWidth, frame.PixelHeight);

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

        var hasTransparency = false;
        for (var offset = 3; offset < pixels.Length; offset += 4)
        {
            if (pixels[offset] < byte.MaxValue)
            {
                hasTransparency = true;
                break;
            }
        }

        if (!hasTransparency)
        {
            throw new InvalidDataException(
                "F3.1 requiere un PNG con transparencia. Usa la preparación de foto para quitar un fondo blanco.");
        }

        return new SignatureAsset(
            frame.PixelWidth,
            frame.PixelHeight,
            stride,
            pixels,
            Path.GetFileName(path));
    }

    private static (int Width, int Height) ReadAndValidateHeader(Stream stream)
    {
        Span<byte> header = stackalloc byte[24];
        var read = 0;
        while (read < header.Length)
        {
            var count = stream.Read(header[read..]);
            if (count == 0)
                break;
            read += count;
        }

        if (read < header.Length || !header[..8].SequenceEqual(PngSignature))
            throw new InvalidDataException("El archivo no es un PNG válido.");
        if (!header.Slice(12, 4).SequenceEqual("IHDR"u8))
            throw new InvalidDataException("El PNG no contiene un encabezado IHDR válido.");

        var widthRaw = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(16, 4));
        var heightRaw = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(20, 4));
        if (widthRaw == 0 || heightRaw == 0 || widthRaw > int.MaxValue || heightRaw > int.MaxValue)
            throw new InvalidDataException("Las dimensiones del PNG no son válidas.");

        var width = checked((int)widthRaw);
        var height = checked((int)heightRaw);
        ValidatePixelCount(width, height);
        return (width, height);
    }

    private static void ValidatePixelCount(int width, int height)
    {
        var pixels = checked((long)width * height);
        if (pixels > MaxDecodedPixels)
        {
            throw new InvalidDataException(
                $"La imagen supera el límite de {MaxDecodedPixels:N0} píxeles decodificados.");
        }
    }
}
