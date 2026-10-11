using System.Globalization;
using SGPdf.App.Features.Edit.Text;

namespace SGPdf.App.Features.Utilities;

internal static class PageNumberFormatter
{
    internal static string Format(long number, PageNumberingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        ValidateFontCoverage(settings.Prefix);
        ValidateFontCoverage(settings.Suffix);

        var formattedNumber = settings.PaddingWidth == 0
            ? number.ToString(CultureInfo.InvariantCulture)
            : number.ToString($"D{settings.PaddingWidth}", CultureInfo.InvariantCulture);

        return string.Concat(settings.Prefix, formattedNumber, settings.Suffix);
    }

    private static void ValidateFontCoverage(string value)
    {
        foreach (var rune in value.EnumerateRunes())
        {
            if (!FallbackFontAsset.SupportsRune(rune))
                throw new NotSupportedException($"The fallback font does not support U+{rune.Value:X4}.");
        }
    }
}
