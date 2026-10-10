using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class FallbackFontAssetTests
{
    private const string ExpectedSha256 = "7da195a74c55bef988d0d48f9508bd5d849425c1770dba5d7bfc6ce9ed848954";

    [Fact]
    public void PinnedDejaVuSans_HasExpectedSha256()
    {
        var path = ResolveOutputFontPath();
        Assert.True(File.Exists(path), $"Missing pinned fallback font: {path}");

        using var stream = File.OpenRead(path);
        var actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        Assert.Equal(ExpectedSha256, actual);
    }

    [Fact]
    public void PinnedDejaVuSans_CoversAsciiAndSpanishRequiredRunes()
    {
        var path = ResolveOutputFontPath();
        Assert.True(File.Exists(path), $"Missing pinned fallback font: {path}");

        var glyphTypeface = new GlyphTypeface(new Uri(path, UriKind.Absolute));
        var required = Enumerable.Range(0x20, 0x7f - 0x20)
            .Concat("áéíóúüñÁÉÍÓÚÜÑ¿¡".EnumerateRunes().Select(static rune => rune.Value))
            .Distinct()
            .ToArray();

        foreach (var codePoint in required)
        {
            Assert.True(
                glyphTypeface.CharacterToGlyphMap.TryGetValue(codePoint, out var glyphId) && glyphId != 0,
                $"DejaVu Sans 2.37 does not cover U+{codePoint:X4}.");
        }
    }

    [Fact]
    public void FallbackFontAsset_ProductionTypeAndMemoryGuardExist()
    {
        var assembly = typeof(SGPdf.App.Pdf.PdfDocumentSession).Assembly;
        var type = assembly.GetType("SGPdf.App.Features.Edit.Text.FallbackFontAsset");
        Assert.NotNull(type);

        Assert.NotNull(type!.GetMethod(
            "LoadBytes",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
        Assert.NotNull(type.GetMethod(
            "SupportsRune",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
        Assert.NotNull(type.GetMethod(
            "GetGlyphId",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
    }

    private static string ResolveOutputFontPath() =>
        Path.Combine(AppContext.BaseDirectory, "fonts", "DejaVuSans.ttf");
}
