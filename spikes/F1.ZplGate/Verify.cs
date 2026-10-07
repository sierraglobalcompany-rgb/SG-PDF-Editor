using System.Diagnostics;
using System.Globalization;
using System.Text;
using BinaryKits.Zpl.Viewer;
using BinaryKits.Zpl.Viewer.ElementDrawers;
using SkiaSharp;
using ZXing;
using ZXing.Common;

const double WidthMm = 102.0;
const double HeightMm = 152.0;
const int Dpmm = 8;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: Verify <labelize.exe> <spike-output-dir>");
    return 2;
}

var labelizeExe = Path.GetFullPath(args[0]);
var root = Path.GetFullPath(args[1]);
var binaryDir = Path.Combine(root, "binarykits");
var labelizeDir = Path.Combine(root, "labelize");
var sb = new StringBuilder();

sb.AppendLine("# F1 ZPL Gate — corrected alpha-aware verification");
sb.AppendLine();
sb.AppendLine("BinaryKits PNGs use transparent background with black RGB. This pass composites both engines over white before pixel comparison and barcode decoding.");
sb.AppendLine();
sb.AppendLine("| Case | Mismatch | Ink IoU |");
sb.AppendLine("|---|---:|---:|");

foreach (var leftPath in Directory.EnumerateFiles(binaryDir, "*.png").OrderBy(Path.GetFileName))
{
    var name = Path.GetFileName(leftPath);
    var rightPath = Path.Combine(labelizeDir, name);
    if (!File.Exists(rightPath))
        continue;

    var diff = Compare(leftPath, rightPath);
    sb.AppendLine($"| {Path.GetFileNameWithoutExtension(name)} | {diff.Mismatch:F3}% | {diff.Iou:F2}% |");
}

sb.AppendLine();
sb.AppendLine("## Barcode decode after white compositing");
sb.AppendLine();
AppendDecode(sb, "Code128 BinaryKits", Path.Combine(binaryDir, "06-code128.png"), BarcodeFormat.CODE_128, "123456789012");
AppendDecode(sb, "Code128 Labelize", Path.Combine(labelizeDir, "06-code128.png"), BarcodeFormat.CODE_128, "123456789012");
AppendDecode(sb, "QR BinaryKits", Path.Combine(binaryDir, "07-qr.png"), BarcodeFormat.QR_CODE, "SG-PDF-QR-12345");
AppendDecode(sb, "QR Labelize", Path.Combine(labelizeDir, "07-qr.png"), BarcodeFormat.QR_CODE, "SG-PDF-QR-12345");

var ftZpl = "^XA^FT200,1100^BQN,2,10^FDMA,TestTest^FS^XZ";
var ftInput = Path.Combine(root, "corpus", "11-qr-ft-known-risk.zpl");
var ftBinary = Path.Combine(binaryDir, "11-qr-ft-known-risk.png");
var ftLabelize = Path.Combine(labelizeDir, "11-qr-ft-known-risk.png");
File.WriteAllText(ftInput, ftZpl, new UTF8Encoding(false));
RenderBinary(ftZpl, ftBinary);
RenderLabelize(labelizeExe, ftInput, ftLabelize);

var ftDiff = Compare(ftBinary, ftLabelize);
var ftBinaryBounds = GetInkBounds(ftBinary);
var ftLabelizeBounds = GetInkBounds(ftLabelize);
var ftBinaryDecode = Decode(ftBinary, BarcodeFormat.QR_CODE);
var ftLabelizeDecode = Decode(ftLabelize, BarcodeFormat.QR_CODE);

sb.AppendLine();
sb.AppendLine("## Known-risk probe: `^FT + ^BQ`");
sb.AppendLine();
sb.AppendLine($"- Cross-engine mismatch: **{ftDiff.Mismatch:F3}%**; ink IoU: **{ftDiff.Iou:F2}%**.");
sb.AppendLine($"- BinaryKits ink bounds: `{ftBinaryBounds}`.");
sb.AppendLine($"- Labelize ink bounds: `{ftLabelizeBounds}`.");
sb.AppendLine($"- BinaryKits QR decode: **{FormatDecode(ftBinaryDecode, "TestTest")}**.");
sb.AppendLine($"- Labelize QR decode: **{FormatDecode(ftLabelizeDecode, "TestTest")}**.");

var reportPath = Path.Combine(root, "corrected-report.md");
File.WriteAllText(reportPath, sb.ToString());
Console.WriteLine(sb.ToString());
return 0;

static void AppendDecode(StringBuilder sb, string label, string path, BarcodeFormat format, string expected)
{
    var result = Decode(path, format);
    sb.AppendLine($"- {label}: **{FormatDecode(result, expected)}**");
}

static string FormatDecode(Result? result, string expected)
    => result is not null && string.Equals(result.Text, expected, StringComparison.Ordinal)
        ? $"PASS ({result.Text})"
        : $"FAIL ({result?.Text ?? "no result"})";

static void RenderBinary(string zpl, string outputPath)
{
    IPrinterStorage storage = new PrinterStorage();
    var analyzer = new ZplAnalyzer(storage);
    var drawer = new ZplElementDrawer(storage);
    var analyze = analyzer.Analyze(zpl);
    if (analyze.Errors.Length > 0 || analyze.UnknownCommands.Length > 0 || analyze.LabelInfos.Length == 0)
        throw new InvalidOperationException($"BinaryKits analyze failed. Errors={string.Join(" | ", analyze.Errors)} Unknown={string.Join(" | ", analyze.UnknownCommands)}");
    File.WriteAllBytes(outputPath, drawer.Draw(analyze.LabelInfos[^1].ZplElements, WidthMm, HeightMm, Dpmm));
}

static void RenderLabelize(string executable, string inputPath, string outputPath)
{
    var psi = new ProcessStartInfo
    {
        FileName = executable,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };
    foreach (var arg in new[]
    {
        "convert", inputPath, "-o", outputPath,
        "--width", WidthMm.ToString(CultureInfo.InvariantCulture),
        "--height", HeightMm.ToString(CultureInfo.InvariantCulture),
        "--dpmm", Dpmm.ToString(CultureInfo.InvariantCulture)
    })
        psi.ArgumentList.Add(arg);

    using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start Labelize.");
    var stdout = process.StandardOutput.ReadToEndAsync();
    var stderr = process.StandardError.ReadToEndAsync();
    process.WaitForExit();
    Task.WaitAll(stdout, stderr);
    if (process.ExitCode != 0 || !File.Exists(outputPath))
        throw new InvalidOperationException($"Labelize failed: exit={process.ExitCode}; {stderr.Result}");
}

static (double Mismatch, double Iou) Compare(string leftPath, string rightPath)
{
    using var left = SKBitmap.Decode(leftPath) ?? throw new InvalidOperationException("Could not decode BinaryKits PNG.");
    using var right = SKBitmap.Decode(rightPath) ?? throw new InvalidOperationException("Could not decode Labelize PNG.");
    if (left.Width != right.Width || left.Height != right.Height)
        throw new InvalidOperationException($"Dimension mismatch: {left.Width}x{left.Height} vs {right.Width}x{right.Height}.");

    long mismatch = 0, union = 0, intersection = 0;
    var total = (long)left.Width * left.Height;
    for (var y = 0; y < left.Height; y++)
    for (var x = 0; x < left.Width; x++)
    {
        var a = IsBlackOnWhite(left.GetPixel(x, y));
        var b = IsBlackOnWhite(right.GetPixel(x, y));
        if (a != b) mismatch++;
        if (a || b) union++;
        if (a && b) intersection++;
    }

    return (mismatch * 100d / total, union == 0 ? 100d : intersection * 100d / union);
}

static string GetInkBounds(string path)
{
    using var bitmap = SKBitmap.Decode(path) ?? throw new InvalidOperationException("PNG decode failed.");
    var minX = bitmap.Width;
    var minY = bitmap.Height;
    var maxX = -1;
    var maxY = -1;
    for (var y = 0; y < bitmap.Height; y++)
    for (var x = 0; x < bitmap.Width; x++)
    {
        if (!IsBlackOnWhite(bitmap.GetPixel(x, y))) continue;
        minX = Math.Min(minX, x);
        minY = Math.Min(minY, y);
        maxX = Math.Max(maxX, x);
        maxY = Math.Max(maxY, y);
    }
    return maxX < 0 ? "empty" : $"x={minX}..{maxX}, y={minY}..{maxY}";
}

static bool IsBlackOnWhite(SKColor color)
{
    var alpha = color.Alpha;
    var red = (color.Red * alpha + 255 * (255 - alpha)) / 255;
    var green = (color.Green * alpha + 255 * (255 - alpha)) / 255;
    var blue = (color.Blue * alpha + 255 * (255 - alpha)) / 255;
    var luminance = (red * 299 + green * 587 + blue * 114) / 1000;
    return luminance < 128;
}

static Result? Decode(string path, BarcodeFormat expectedFormat)
{
    using var bitmap = SKBitmap.Decode(path) ?? throw new InvalidOperationException("PNG decode failed.");
    var rgb = new byte[checked(bitmap.Width * bitmap.Height * 3)];
    var offset = 0;
    for (var y = 0; y < bitmap.Height; y++)
    for (var x = 0; x < bitmap.Width; x++)
    {
        var color = bitmap.GetPixel(x, y);
        var alpha = color.Alpha;
        rgb[offset++] = (byte)((color.Red * alpha + 255 * (255 - alpha)) / 255);
        rgb[offset++] = (byte)((color.Green * alpha + 255 * (255 - alpha)) / 255);
        rgb[offset++] = (byte)((color.Blue * alpha + 255 * (255 - alpha)) / 255);
    }

    var reader = new BarcodeReaderGeneric
    {
        Options = new DecodingOptions { TryHarder = true, PossibleFormats = [expectedFormat] }
    };
    return reader.Decode(rgb, bitmap.Width, bitmap.Height, RGBLuminanceSource.BitmapFormat.RGB24);
}
