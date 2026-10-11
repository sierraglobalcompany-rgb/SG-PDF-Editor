using SGPdf.App.Features.Edit.Text;

namespace SGPdf.App.Features.Utilities;

internal sealed record PageNumberingSettings
{
    internal PageNumberingSettings(
        long startNumber = 1,
        string prefix = "",
        string suffix = "",
        int paddingWidth = 0)
        : this(startNumber, prefix, suffix, paddingWidth, UtilityOverlayColors.OpaqueBlack)
    {
    }

    internal PageNumberingSettings(
        long startNumber,
        string prefix,
        string suffix,
        int paddingWidth,
        PdfTextFillColor fillColor)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(suffix);
        if (paddingWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(paddingWidth), "Padding width cannot be negative.");
        UtilityOverlayColors.Validate(fillColor, nameof(fillColor));

        StartNumber = startNumber;
        Prefix = prefix;
        Suffix = suffix;
        PaddingWidth = paddingWidth;
        FillColor = fillColor;
    }

    internal long StartNumber { get; }

    internal string Prefix { get; }

    internal string Suffix { get; }

    internal int PaddingWidth { get; }

    internal PdfTextFillColor FillColor { get; }

    internal long NumberAt(int selectedPageOrdinal) =>
        checked(StartNumber + selectedPageOrdinal);
}
