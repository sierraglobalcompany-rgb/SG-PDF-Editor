namespace SGPdf.App.Features.Sign;

internal sealed class SignaturePhotoSource
{
    private readonly byte[] _bgraPixels;

    internal SignaturePhotoSource(
        int pixelWidth,
        int pixelHeight,
        int stride,
        byte[] bgraPixels,
        string? sourceName = null)
    {
        ArgumentNullException.ThrowIfNull(bgraPixels);

        if (pixelWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(pixelWidth));
        if (pixelHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(pixelHeight));
        if (stride < checked(pixelWidth * 4))
            throw new ArgumentOutOfRangeException(nameof(stride));

        var requiredLength = checked(stride * pixelHeight);
        if (bgraPixels.Length < requiredLength)
            throw new ArgumentException("The BGRA buffer is smaller than the declared image geometry.", nameof(bgraPixels));

        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        Stride = stride;
        _bgraPixels = bgraPixels[..requiredLength].ToArray();
        SourceName = sourceName;
    }

    internal int PixelWidth { get; }
    internal int PixelHeight { get; }
    internal int Stride { get; }
    internal ReadOnlyMemory<byte> BgraPixels => _bgraPixels;
    internal string? SourceName { get; }
}
