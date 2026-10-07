using System.IO;

namespace SGPdf.App.Features.Labels;

public static class ZplDocumentParser
{
    private const string StartCommand = "^XA";
    private const string EndCommand = "^XZ";

    public static ZplDocument Parse(string sourcePath, string source)
    {
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(source);

        var designs = new List<ZplDesign>();
        var cursor = 0;
        var sourceBlockIndex = 0;
        var foundBlock = false;

        while (cursor < source.Length)
        {
            var nextStart = source.IndexOf(StartCommand, cursor, StringComparison.OrdinalIgnoreCase);
            var nextEnd = source.IndexOf(EndCommand, cursor, StringComparison.OrdinalIgnoreCase);

            if (nextStart < 0 && nextEnd < 0)
                break;

            if (nextEnd >= 0 && (nextStart < 0 || nextEnd < nextStart))
                throw new InvalidDataException("ZPL contains ^XZ without a matching ^XA.");

            if (nextStart < 0)
                break;

            var end = source.IndexOf(EndCommand, nextStart + StartCommand.Length, StringComparison.OrdinalIgnoreCase);
            if (end < 0)
                throw new InvalidDataException("ZPL contains ^XA without a matching ^XZ.");

            var nestedStart = source.IndexOf(StartCommand, nextStart + StartCommand.Length, StringComparison.OrdinalIgnoreCase);
            if (nestedStart >= 0 && nestedStart < end)
                throw new InvalidDataException("ZPL contains a nested ^XA before the current block closes.");

            foundBlock = true;
            var blockEnd = end + EndCommand.Length;
            var block = source[nextStart..blockEnd];

            if (!ContainsCommand(block, "^DF"))
            {
                designs.Add(new ZplDesign(
                    designs.Count,
                    sourceBlockIndex,
                    block,
                    1));
            }

            sourceBlockIndex++;
            cursor = blockEnd;
        }

        if (!foundBlock)
            throw new InvalidDataException("The file does not contain any complete ^XA...^XZ ZPL blocks.");

        if (designs.Count == 0)
            throw new InvalidDataException("The file does not contain any printable ZPL designs.");

        return new ZplDocument(sourcePath, source, source, designs);
    }

    private static bool ContainsCommand(string block, string command)
        => block.IndexOf(command, StringComparison.OrdinalIgnoreCase) >= 0;
}
