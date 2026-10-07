using System.Text;

namespace SGPdf.App.Features.Labels;

public static class ZplFileLoader
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    public static ZplDocument Load(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var fullPath = Path.GetFullPath(filePath);
        var extension = Path.GetExtension(fullPath);
        if (!IsSupportedExtension(extension))
        {
            throw new NotSupportedException(
                $"Unsupported ZPL file extension '{extension}'. Expected .zpl, .txt, or .prn.");
        }

        var bytes = File.ReadAllBytes(fullPath);
        var offset = HasUtf8Bom(bytes) ? 3 : 0;
        var source = StrictUtf8.GetString(bytes, offset, bytes.Length - offset);
        return ZplDocumentParser.Parse(fullPath, source);
    }

    private static bool IsSupportedExtension(string extension)
        => extension.Equals(".zpl", StringComparison.OrdinalIgnoreCase) ||
           extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) ||
           extension.Equals(".prn", StringComparison.OrdinalIgnoreCase);

    private static bool HasUtf8Bom(byte[] bytes)
        => bytes.Length >= 3 &&
           bytes[0] == 0xEF &&
           bytes[1] == 0xBB &&
           bytes[2] == 0xBF;
}
