namespace SGPdf.App.Features.Utilities;

internal sealed record PageNumberingSettings
{
    internal PageNumberingSettings(
        long startNumber = 1,
        string prefix = "",
        string suffix = "",
        int paddingWidth = 0)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(suffix);
        if (paddingWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(paddingWidth), "Padding width cannot be negative.");

        StartNumber = startNumber;
        Prefix = prefix;
        Suffix = suffix;
        PaddingWidth = paddingWidth;
    }

    internal long StartNumber { get; }

    internal string Prefix { get; }

    internal string Suffix { get; }

    internal int PaddingWidth { get; }

    internal long NumberAt(int selectedPageOrdinal) =>
        checked(StartNumber + selectedPageOrdinal);
}
