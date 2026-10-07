namespace SGPdf.App.Features.Labels;

public enum ZplQuantityMode
{
    FromFile,
    OneEach,
    Custom
}

public sealed class ZplQuantitySelection
{
    public const long MaxCustomQuantity = 99_999_999;

    public ZplQuantitySelection(ZplQuantityMode mode, long customQuantity = 1)
    {
        if (!Enum.IsDefined(mode))
            throw new ArgumentOutOfRangeException(nameof(mode));
        if (customQuantity < 1 || customQuantity > MaxCustomQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(customQuantity),
                $"Custom quantity must be between 1 and {MaxCustomQuantity}.");
        }

        Mode = mode;
        CustomQuantity = customQuantity;
    }

    public ZplQuantityMode Mode { get; }

    public long CustomQuantity { get; }

    public long GetQuantity(ZplDesign design)
    {
        ArgumentNullException.ThrowIfNull(design);

        return Mode switch
        {
            ZplQuantityMode.FromFile => design.QuantityFromFile,
            ZplQuantityMode.OneEach => 1,
            ZplQuantityMode.Custom => CustomQuantity,
            _ => throw new InvalidOperationException("Unsupported ZPL quantity mode.")
        };
    }

    public long GetTotalQuantity(ZplDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        long total = 0;
        checked
        {
            foreach (var design in document.Designs)
                total += GetQuantity(design);
        }

        return total;
    }
}
