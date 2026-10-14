using System.IO;
using System.Text;

namespace SGPdf.App.Features.Labels;

public sealed class QrCodeGenerator
{
    private const int MaxPayloadBytes = 512;
    private static readonly ZplRenderOptions RenderOptions = new(60d, 60d, 8);

    private readonly LabelizeProcessRenderer _renderer;

    public QrCodeGenerator()
        : this(new LabelizeProcessRenderer())
    {
    }

    internal QrCodeGenerator(LabelizeProcessRenderer renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public async Task<byte[]> GeneratePngAsync(
        string value,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("QR value cannot be empty.", nameof(value));

        cancellationToken.ThrowIfCancellationRequested();

        var payload = Encoding.UTF8.GetBytes(value);
        if (payload.Length > MaxPayloadBytes)
        {
            throw new ArgumentException(
                $"QR value cannot exceed {MaxPayloadBytes} UTF-8 bytes.",
                nameof(value));
        }

        var source = BuildZpl(payload);
        var document = ZplDocumentParser.Parse("generated-qr.zpl", source);
        var rendered = await _renderer
            .RenderAsync(document, RenderOptions, cancellationToken)
            .ConfigureAwait(false);

        if (rendered.Count != 1)
            throw new InvalidDataException("QR generation must produce exactly one PNG.");

        return rendered[0].PngBytes;
    }

    private static string BuildZpl(ReadOnlySpan<byte> payload)
    {
        var encoded = new StringBuilder(payload.Length * 3);
        foreach (var value in payload)
            encoded.Append('_').Append(value.ToString("X2"));

        return $"^XA^CI28^PW480^LL480^FO20,20^BQN,2,5^FH^FDLA,{encoded}^FS^XZ";
    }
}
