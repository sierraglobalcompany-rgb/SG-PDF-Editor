namespace SGPdf.App.Features.Sign;

internal static class SignatureImageLimits
{
    internal const long MaxDecodedPixels = 20_000_000;

    internal static void ValidatePixelCount(int width, int height)
    {
        if (width <= 0)
            throw new InvalidDataException("El ancho de la imagen no es válido.");
        if (height <= 0)
            throw new InvalidDataException("El alto de la imagen no es válido.");

        var pixels = checked((long)width * height);
        if (pixels > MaxDecodedPixels)
        {
            throw new InvalidDataException(
                $"La imagen supera el límite de {MaxDecodedPixels:N0} píxeles decodificados.");
        }
    }
}
