using System.IO;
using System.Text;
using System.Windows.Media;

namespace SGPdf.App.Features.Edit.Text;

internal static class FallbackFontAsset
{
    internal const string FontFileName = "DejaVuSans.ttf";
    internal const long MaxFontBytes = 2L * 1024L * 1024L;

    private static readonly Lazy<GlyphTypeface> Typeface = new(
        static () => CreateValidatedTypeface(ResolveFontPath()),
        LazyThreadSafetyMode.ExecutionAndPublication);

    internal static byte[] LoadBytes() => ReadValidatedBytes(ResolveFontPath());

    internal static bool SupportsRune(Rune rune) =>
        Typeface.Value.CharacterToGlyphMap.TryGetValue(rune.Value, out var glyphId) && glyphId != 0;

    internal static ushort GetGlyphId(Rune rune)
    {
        if (!Typeface.Value.CharacterToGlyphMap.TryGetValue(rune.Value, out var glyphId) || glyphId == 0)
            throw new NotSupportedException($"The fallback font does not support U+{rune.Value:X4}.");

        return glyphId;
    }

    internal static byte[] ReadValidatedBytes(string path)
    {
        ValidateFontFile(path);
        return File.ReadAllBytes(path);
    }

    private static GlyphTypeface CreateValidatedTypeface(string path)
    {
        ValidateFontFile(path);
        return new GlyphTypeface(new Uri(path, UriKind.Absolute));
    }

    private static void ValidateFontFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var info = new FileInfo(path);
        if (!info.Exists)
            throw new FileNotFoundException("The pinned fallback font was not found.", path);
        if (info.Length <= 0 || info.Length > MaxFontBytes)
            throw new NotSupportedException(
                $"Fallback font size must be between 1 and {MaxFontBytes} bytes; actual: {info.Length}.");
    }

    private static string ResolveFontPath() =>
        Path.Combine(AppContext.BaseDirectory, "fonts", FontFileName);
}
