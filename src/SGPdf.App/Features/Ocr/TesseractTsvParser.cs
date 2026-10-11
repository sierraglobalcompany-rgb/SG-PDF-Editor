using System.Globalization;
using System.IO;
using System.Text;

namespace SGPdf.App.Features.Ocr;

internal static class TesseractTsvParser
{
    private const string ExpectedHeader = "level\tpage_num\tblock_num\tpar_num\tline_num\tword_num\tleft\ttop\twidth\theight\tconf\ttext";
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    internal static IReadOnlyList<OcrWord> ParseFile(string tsvPath)
    {
        if (string.IsNullOrWhiteSpace(tsvPath))
            throw new ArgumentException("La ruta TSV es obligatoria.", nameof(tsvPath));

        var text = File.ReadAllText(tsvPath, StrictUtf8);
        return ParseText(text);
    }

    internal static IReadOnlyList<OcrWord> ParseText(string tsv)
    {
        ArgumentNullException.ThrowIfNull(tsv);

        var lines = tsv.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

        if (lines.Length == 0 || !string.Equals(lines[0], ExpectedHeader, StringComparison.Ordinal))
            throw new InvalidDataException("El encabezado TSV de Tesseract no coincide con el formato esperado.");

        var words = new List<OcrWord>();
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            if (line.Length == 0)
                continue;

            var columns = line.Split('\t');
            if (columns.Length != 12)
                throw InvalidRow(lineIndex, "la fila no contiene exactamente 12 columnas");

            var level = ParseInt(columns[0], lineIndex, "level");
            if (level is >= 1 and <= 4)
                continue;
            if (level != 5)
                throw InvalidRow(lineIndex, "nivel TSV no soportado");

            var page = ParseInt(columns[1], lineIndex, "page_num");
            var block = ParseInt(columns[2], lineIndex, "block_num");
            var paragraph = ParseInt(columns[3], lineIndex, "par_num");
            var textLine = ParseInt(columns[4], lineIndex, "line_num");
            var word = ParseInt(columns[5], lineIndex, "word_num");
            var left = ParseInt(columns[6], lineIndex, "left");
            var top = ParseInt(columns[7], lineIndex, "top");
            var width = ParseInt(columns[8], lineIndex, "width");
            var height = ParseInt(columns[9], lineIndex, "height");
            var confidence = ParseDouble(columns[10], lineIndex, "conf");
            var text = columns[11];

            if (page <= 0 || block < 0 || paragraph < 0 || textLine < 0 || word <= 0)
                throw InvalidRow(lineIndex, "identificadores TSV inválidos");
            if (left < 0 || top < 0 || width <= 0 || height <= 0)
                throw InvalidRow(lineIndex, "geometría TSV inválida");
            if (!double.IsFinite(confidence))
                throw InvalidRow(lineIndex, "confianza TSV no finita");
            if (string.IsNullOrWhiteSpace(text))
                throw InvalidRow(lineIndex, "texto OCR vacío");

            words.Add(new OcrWord(
                page,
                block,
                paragraph,
                textLine,
                word,
                confidence,
                text,
                new OcrPixelRect(left, top, width, height)));
        }

        return words;
    }

    private static int ParseInt(string value, int lineIndex, string column)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            throw InvalidRow(lineIndex, $"{column} no es un entero válido");
        return result;
    }

    private static double ParseDouble(string value, int lineIndex, string column)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            throw InvalidRow(lineIndex, $"{column} no es un número válido");
        return result;
    }

    private static InvalidDataException InvalidRow(int lineIndex, string reason)
    {
        return new InvalidDataException($"TSV de Tesseract inválido en la fila {lineIndex + 1}: {reason}.");
    }
}
