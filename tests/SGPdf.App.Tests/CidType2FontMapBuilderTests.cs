using System.Text;
using SGPdf.App.Features.Edit.Text;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class CidType2FontMapBuilderTests
{
    [Fact]
    public void Build_UsesDeterministicUniqueRuneCidsAndExactToUnicode()
    {
        static ushort Glyph(Rune rune) => checked((ushort)(0x0100 + rune.Value));

        var first = CidType2FontMapBuilder.Build(new[] { "NIÑO áé", "CASA" }, Glyph);
        var second = CidType2FontMapBuilder.Build(new[] { "CASA", "NIÑO áé" }, Glyph);

        Assert.Equal(first.ToUnicodeCMap, second.ToUnicodeCMap);
        Assert.Equal(first.CidToGidMap, second.CidToGidMap);

        var ordered = first.RuneToCid.Keys.OrderBy(rune => rune.Value).ToArray();
        Assert.Equal(ordered, first.RuneToCid.Keys.ToArray());
        Assert.Equal(Enumerable.Range(1, ordered.Length).Select(value => (ushort)value), first.RuneToCid.Values);

        foreach (var pair in first.RuneToCid)
        {
            var cidHex = pair.Value.ToString("X4");
            var unicodeHex = ToUtf16BeHex(pair.Key);
            Assert.Contains($"<{cidHex}> <{unicodeHex}>", first.ToUnicodeCMap, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Build_WritesCidToGidMapAsBigEndianGlyphIds()
    {
        static ushort Glyph(Rune rune) => rune.Value switch
        {
            0x0041 => 0x0123,
            0x00D1 => 0x4567,
            _ => 0x0001
        };

        var plan = CidType2FontMapBuilder.Build(new[] { "ÑA" }, Glyph);

        foreach (var pair in plan.RuneToCid)
        {
            var glyphId = Glyph(pair.Key);
            var offset = checked(pair.Value * 2);
            Assert.Equal((byte)(glyphId >> 8), plan.CidToGidMap[offset]);
            Assert.Equal((byte)(glyphId & 0xFF), plan.CidToGidMap[offset + 1]);
        }
    }

    [Fact]
    public void Build_RejectsMissingGlyphBeforeProducingMap()
    {
        var ex = Assert.Throws<NotSupportedException>(() =>
            CidType2FontMapBuilder.Build(new[] { "NIÑO" }, rune => rune.Value == 0x00D1 ? (ushort)0 : (ushort)1));

        Assert.Contains("U+00D1", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string ToUtf16BeHex(Rune rune)
    {
        Span<char> chars = stackalloc char[2];
        var count = rune.EncodeToUtf16(chars);
        var builder = new StringBuilder(count * 4);
        for (var index = 0; index < count; index++)
            builder.Append(((ushort)chars[index]).ToString("X4"));
        return builder.ToString();
    }
}
