using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SGPdf.App.Features.Labels;

public static partial class ZplDocumentParser
{
    private const string StartCommand = "^XA";
    private const string EndCommand = "^XZ";
    private const int MaximumPrintQuantity = 99_999_999;

    [GeneratedRegex(@"\^PQ(?<body>[^\^~]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PrintQuantityRegex();

    public static ZplDocument Parse(string sourcePath, string source)
    {
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(source);

        var designs = new List<ZplDesign>();
        var normalized = new StringBuilder(source.Length);
        var cursor = 0;
        var sourceBlockIndex = 0;
        var foundBlock = false;

        while (cursor < source.Length)
        {
            var nextStart = source.IndexOf(StartCommand, cursor, StringComparison.OrdinalIgnoreCase);
            var nextEnd = source.IndexOf(EndCommand, cursor, StringComparison.OrdinalIgnoreCase);

            if (nextStart < 0 && nextEnd < 0)
            {
                normalized.Append(source, cursor, source.Length - cursor);
                cursor = source.Length;
                break;
            }

            if (nextEnd >= 0 && (nextStart < 0 || nextEnd < nextStart))
                throw new InvalidDataException("ZPL contains ^XZ without a matching ^XA.");

            if (nextStart < 0)
                break;

            normalized.Append(source, cursor, nextStart - cursor);

            var end = source.IndexOf(EndCommand, nextStart + StartCommand.Length, StringComparison.OrdinalIgnoreCase);
            if (end < 0)
                throw new InvalidDataException("ZPL contains ^XA without a matching ^XZ.");

            var nestedStart = source.IndexOf(StartCommand, nextStart + StartCommand.Length, StringComparison.OrdinalIgnoreCase);
            if (nestedStart >= 0 && nestedStart < end)
                throw new InvalidDataException("ZPL contains a nested ^XA before the current block closes.");

            foundBlock = true;
            var blockEnd = end + EndCommand.Length;
            var block = source[nextStart..blockEnd];
            var isStoredFormatDefinition = ContainsCommand(block, "^DF");
            var quantityMatches = PrintQuantityRegex().Matches(block);

            if (isStoredFormatDefinition && quantityMatches.Count > 0)
            {
                throw new InvalidDataException(
                    "^PQ inside a stored format definition is not supported because stored format quantity semantics cannot be assigned safely in F2.1.");
            }

            var quantity = 1;
            if (!isStoredFormatDefinition)
            {
                foreach (Match match in quantityMatches)
                    quantity = ParseQuantity(match.Groups["body"].Value);

                designs.Add(new ZplDesign(
                    designs.Count,
                    sourceBlockIndex,
                    block,
                    quantity));
            }

            normalized.Append(PrintQuantityRegex().Replace(block, string.Empty));
            sourceBlockIndex++;
            cursor = blockEnd;
        }

        if (!foundBlock)
            throw new InvalidDataException("The file does not contain any complete ^XA...^XZ ZPL blocks.");

        if (designs.Count == 0)
            throw new InvalidDataException("The file does not contain any printable ZPL designs.");

        return new ZplDocument(sourcePath, source, normalized.ToString(), designs);
    }

    private static int ParseQuantity(string body)
    {
        var firstParameter = body.Split(',', 2)[0].Trim();
        if (firstParameter.Length == 0)
            return 1;

        if (!int.TryParse(firstParameter, NumberStyles.None, CultureInfo.InvariantCulture, out var quantity))
            throw new InvalidDataException($"Invalid ^PQ quantity '{firstParameter}'.");

        if (quantity == 0)
            return 1;

        if (quantity < 0 || quantity > MaximumPrintQuantity)
        {
            throw new InvalidDataException(
                $"^PQ quantity must be between 0 and {MaximumPrintQuantity.ToString(CultureInfo.InvariantCulture)}.");
        }

        return quantity;
    }

    private static bool ContainsCommand(string block, string command)
        => block.IndexOf(command, StringComparison.OrdinalIgnoreCase) >= 0;
}
