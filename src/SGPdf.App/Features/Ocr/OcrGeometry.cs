namespace SGPdf.App.Features.Ocr;

internal readonly record struct OcrPixelRect
{
    internal OcrPixelRect(int left, int top, int width, int height)
    {
        if (left < 0)
            throw new ArgumentOutOfRangeException(nameof(left));
        if (top < 0)
            throw new ArgumentOutOfRangeException(nameof(top));
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    public int Left { get; }
    public int Top { get; }
    public int Width { get; }
    public int Height { get; }
}

internal sealed record OcrWord(
    int Page,
    int Block,
    int Paragraph,
    int Line,
    int Word,
    double Confidence,
    string Text,
    OcrPixelRect Bounds);
