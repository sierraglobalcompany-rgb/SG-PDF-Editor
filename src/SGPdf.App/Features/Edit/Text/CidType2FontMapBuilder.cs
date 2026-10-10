using System.Collections.ObjectModel;
using System.Text;

namespace SGPdf.App.Features.Edit.Text;

internal sealed record CidType2FontMapPlan(
    string ToUnicodeCMap,
    byte[] CidToGidMap,
    IReadOnlyDictionary<Rune, ushort> RuneToCid);

internal static class CidType2FontMapBuilder
{
    private const int MaxBfCharBlockSize = 100;

    internal static CidType2FontMapPlan Build(
        IEnumerable<string> texts,
        Func<Rune, ushort>? glyphResolver = null)
    {
        ArgumentNullException.ThrowIfNull(texts);
        glyphResolver ??= FallbackFontAsset.GetGlyphId;

        var uniqueRunes = new HashSet<Rune>();
        foreach (var text in texts)
        {
            ArgumentNullException.ThrowIfNull(text);
            foreach (var rune in text.EnumerateRunes())
                uniqueRunes.Add(rune);
        }

        var orderedRunes = uniqueRunes
            .OrderBy(rune => rune.Value)
            .ToArray();
        if (orderedRunes.Length == 0)
            throw new ArgumentException("At least one Unicode rune is required to build a CID Type2 map.", nameof(texts));
        if (orderedRunes.Length > ushort.MaxValue)
            throw new NotSupportedException("A CID Type2 map cannot contain more than 65535 unique runes.");

        var runeToCid = new Dictionary<Rune, ushort>(orderedRunes.Length);
        var cidToGidMap = new byte[checked((orderedRunes.Length + 1) * 2)];

        for (var index = 0; index < orderedRunes.Length; index++)
        {
            var rune = orderedRunes[index];
            var cid = checked((ushort)(index + 1));
            var glyphId = glyphResolver(rune);
            if (glyphId == 0)
                throw new NotSupportedException($"The fallback font does not support U+{rune.Value:X4}.");

            runeToCid.Add(rune, cid);
            var offset = checked(cid * 2);
            cidToGidMap[offset] = checked((byte)(glyphId >> 8));
            cidToGidMap[offset + 1] = checked((byte)(glyphId & 0xFF));
        }

        return new CidType2FontMapPlan(
            BuildToUnicodeCMap(runeToCid),
            cidToGidMap,
            new ReadOnlyDictionary<Rune, ushort>(runeToCid));
    }

    private static string BuildToUnicodeCMap(IReadOnlyDictionary<Rune, ushort> runeToCid)
    {
        var builder = new StringBuilder();
        builder.AppendLine("/CIDInit /ProcSet findresource begin");
        builder.AppendLine("12 dict begin");
        builder.AppendLine("begincmap");
        builder.AppendLine("/CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >> def");
        builder.AppendLine("/CMapName /Adobe-Identity-H def");
        builder.AppendLine("/CMapType 2 def");
        builder.AppendLine("1 begincodespacerange");
        builder.AppendLine("<0000> <FFFF>");
        builder.AppendLine("endcodespacerange");

        var entries = runeToCid.ToArray();
        for (var offset = 0; offset < entries.Length; offset += MaxBfCharBlockSize)
        {
            var count = Math.Min(MaxBfCharBlockSize, entries.Length - offset);
            builder.Append(count).AppendLine(" beginbfchar");
            for (var index = offset; index < offset + count; index++)
            {
                var pair = entries[index];
                builder.Append('<')
                    .Append(pair.Value.ToString("X4"))
                    .Append("> <")
                    .Append(ToUtf16BeHex(pair.Key))
                    .AppendLine(">");
            }
            builder.AppendLine("endbfchar");
        }

        builder.AppendLine("endcmap");
        builder.AppendLine("CMapName currentdict /CMap defineresource pop");
        builder.AppendLine("end");
        builder.AppendLine("end");
        return builder.ToString();
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
