using System.Reflection;
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
        var type = ResolveAssetType();

        Assert.NotNull(type.GetMethod("LoadBytes", BindingFlags.Static | BindingFlags.NonPublic));
        Assert.NotNull(type.GetMethod("SupportsRune", BindingFlags.Static | BindingFlags.NonPublic));
        Assert.NotNull(type.GetMethod("GetGlyphId", BindingFlags.Static | BindingFlags.NonPublic));
        Assert.NotNull(type.GetMethod("ReadValidatedBytes", BindingFlags.Static | BindingFlags.NonPublic));
    }

    [Fact]
    public void FallbackFontAsset_RejectsFileAboveTwoMiBBeforeRead()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f7-font-guard-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "oversized.ttf");
        try
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                stream.SetLength((2L * 1024L * 1024L) + 1L);

            var method = ResolveAssetType().GetMethod(
                "ReadValidatedBytes",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var error = Assert.Throws<TargetInvocationException>(() => method!.Invoke(null, new object[] { path }));
            Assert.IsType<NotSupportedException>(error.InnerException);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static Type ResolveAssetType()
    {
        var assembly = typeof(SGPdf.App.Pdf.PdfDocumentSession).Assembly;
        return assembly.GetType("SGPdf.App.Features.Edit.Text.FallbackFontAsset")
            ?? throw new Xunit.Sdk.XunitException("FallbackFontAsset production type is missing.");
    }

    private static string ResolveOutputFontPath() =>
        Path.Combine(AppContext.BaseDirectory, "fonts", "DejaVuSans.ttf");
}
