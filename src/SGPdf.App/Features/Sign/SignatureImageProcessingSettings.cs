namespace SGPdf.App.Features.Sign;

internal enum SignatureInkStyle
{
    Original,
    Black,
    Blue
}

internal readonly record struct SignatureImageProcessingSettings(
    int BackgroundRemoval,
    int Brightness,
    int Contrast,
    SignatureInkStyle InkStyle,
    bool AutoCrop)
{
    internal static SignatureImageProcessingSettings Automatic { get; } =
        new(65, 0, 20, SignatureInkStyle.Original, true);

    internal void Validate()
    {
        if (BackgroundRemoval is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(BackgroundRemoval));
        if (Brightness is < -100 or > 100)
            throw new ArgumentOutOfRangeException(nameof(Brightness));
        if (Contrast is < -100 or > 100)
            throw new ArgumentOutOfRangeException(nameof(Contrast));
        if (!Enum.IsDefined(InkStyle))
            throw new ArgumentOutOfRangeException(nameof(InkStyle));
    }
}

internal readonly record struct SignaturePaperColor(byte R, byte G, byte B);
