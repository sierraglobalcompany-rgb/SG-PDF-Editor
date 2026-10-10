namespace SGPdf.App.Features.Edit.Images;

internal enum ImageReplacementFormat
{
    Png,
    Jpeg
}

internal sealed record ImageReplacementAsset(
    ImageReplacementFormat Format,
    ReadOnlyMemory<byte> EncodedBytes,
    int PixelWidth,
    int PixelHeight,
    int Stride,
    ReadOnlyMemory<byte> BgraPixels,
    bool HasAlpha);
