using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BinaryKits.Zpl.Viewer;
using BinaryKits.Zpl.Viewer.ElementDrawers;
using SkiaSharp;
using ZXing;
using ZXing.Common;

const double LabelWidthMm = 102.0;
const double LabelHeightMm = 152.0;
const int Dpmm = 8;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: F1.ZplGate <labelize.exe> <output-dir>");
    return 2;
}

var labelizeExe = Path.GetFullPath(args[0]);
var outputRoot = Path.GetFullPath(args[1]);
if (!File.Exists(labelizeExe))
    throw new FileNotFoundException("Labelize executable not found.", labelizeExe);

Directory.CreateDirectory(outputRoot);
var corpusDir = Path.Combine(outputRoot, "corpus");
var binaryDir = Path.Combine(outputRoot, "binarykits");
var labelizeDir = Path.Combine(outputRoot, "labelize");
Directory.CreateDirectory(corpusDir);
Directory.CreateDirectory(binaryDir);
Directory.CreateDirectory(labelizeDir);

var corpus = BuildCorpus();
var cases = new List<CaseReport>();

Console.WriteLine($"Gate corpus: {corpus.Count} synthetic designs");
Console.WriteLine($"Labelize: {labelizeExe}");

foreach (var item in corpus)
{
    var normalized = NormalizeQuantity(item.Zpl, out var quantity);
    var inputPath = Path.Combine(corpusDir, item.Name + ".zpl");
    File.WriteAllText(inputPath, normalized, new UTF8Encoding(false));

    var binaryPath = Path.Combine(binaryDir, item.Name + ".png");
    var labelizePath = Path.Combine(labelizeDir, item.Name + ".png");

    var binary = RenderBinary(normalized, binaryPath);
    var labelize = RenderLabelize(labelizeExe, inputPath, labelizePath);
    var diff = binary.Success && labelize.Success
        ? CompareMonochrome(binaryPath, labelizePath)
        : ImageDiff.NotComparable("render failed");

    DecodeResult? binaryDecode = null;
    DecodeResult? labelizeDecode = null;
    if (item.ExpectedBarcode is not null)
    {
        binaryDecode = binary.Success
            ? DecodeBarcode(binaryPath, item.ExpectedBarcode.Format)
            : DecodeResult.Failed("render failed");
        labelizeDecode = labelize.Success
            ? DecodeBarcode(labelizePath, item.ExpectedBarcode.Format)
            : DecodeResult.Failed("render failed");
    }

    cases.Add(new CaseReport(
        item.Name,
        item.Purpose,
        quantity,
        binary,
        labelize,
        diff,
        item.ExpectedBarcode,
        binaryDecode,
        labelizeDecode));
}

var benchmarks = new List<BenchmarkReport>();
foreach (var count in new[] { 10, 100, 500 })
{
    benchmarks.Add(RunBenchmark(labelizeExe, outputRoot, corpus, count));
}

var report = new GateReport(
    DateTimeOffset.UtcNow,
    "BinaryKits.Zpl.Viewer 1.3.1",
    "Labelize 1.7.0 Windows x64 CLI",
    LabelWidthMm,
    LabelHeightMm,
    Dpmm,
    cases,
    benchmarks);

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
File.WriteAllText(Path.Combine(outputRoot, "report.json"), JsonSerializer.Serialize(report, jsonOptions));
File.WriteAllText(Path.Combine(outputRoot, "report.md"), BuildMarkdown(report));
Console.WriteLine(BuildMarkdown(report));
return 0;

static List<GateCase> BuildCorpus()
{
    return
    [
        new(
            "01-ci28-utf8",
            "^CI28 + tildes/ñ UTF-8",
            "^XA^CI28^FO40,60^A0N,42,42^FDMedellín Ñandú café acción^FS^XZ"),
        new(
            "02-fh-hex",
            "^FH escapes over UTF-8 bytes",
            "^XA^CI28^FO40,60^A0N,42,42^FH_^FDCaf_C3_A9 Ni_C3_B1o^FS^XZ"),
        new(
            "03-fb-long-address",
            "^FB long address wrapping",
            "^XA^CI28^FO40,40^A0N,30,30^FB700,4,6,L,0^FDCarrera 80 # 45-120, apartamento 301, Medellín, Antioquia. Referencia: portería torre norte.^FS^XZ"),
        new(
            "04-fr-reverse",
            "^FR reverse field over filled box",
            "^XA^FO30,30^GB740,150,150,B,0^FS^FO55,80^A0N,40,40^FR^FDTEXTO INVERTIDO^FS^XZ"),
        new(
            "05-gfa-logo",
            "^GFA inline graphic field",
            "^XA^FO80,80^GFA,32,32,2,FFFF80018001800180018001800180018001800180018001800180018001FFFF^FS^XZ"),
        new(
            "06-code128",
            "^BC Code 128 decodability",
            "^XA^BY2,2,100^FO60,80^BCN,100,Y,N,N^FD123456789012^FS^XZ",
            new ExpectedBarcode(BarcodeFormat.CODE_128, "123456789012")),
        new(
            "07-qr",
            "^BQ QR decodability",
            "^XA^FO80,80^BQN,2,6^FDLA,SG-PDF-QR-12345^FS^XZ",
            new ExpectedBarcode(BarcodeFormat.QR_CODE, "SG-PDF-QR-12345")),
        new(
            "08-pq-quantity",
            "^PQ quantity extracted before rendering",
            "^XA^FO40,50^A0N,36,36^FDCantidad separada del render^FS^PQ28^XZ"),
        new(
            "09-df-xf-template",
            "^DF/^XF stored format with ^FN substitution",
            "^XA^DFR:SGFORM.ZPL^FO50,60^A0N,36,36^FN1^FS^XZ^XA^XFR:SGFORM.ZPL^FS^FN1^FDPlantilla OK^FS^XZ"),
        new(
            "10-shipping-composite",
            "Composite shipping-style label",
            "^XA^CI28^FO25,25^GB760,520,3,B,0^FS^FO50,50^A0N,34,34^FDSierra Global - Medellín^FS^FO50,100^A0N,26,26^FB560,3,4,L,0^FDCliente: María Peña. Calle 45 # 80-20, Torre 3, Apto 501.^FS^BY2,2,85^FO50,220^BCN,85,N,N,N^FDSG202610070001^FS^FO540,190^BQN,2,5^FDLA,SG202610070001^FS^FO50,360^A0N,26,26^FDEntrega local - cada etiqueta se renderiza una sola vez.^FS^XZ")
    ];
}

static string NormalizeQuantity(string zpl, out int quantity)
{
    quantity = 1;
    var match = Regex.Match(zpl, @"\^PQ(?<qty>\d+)(?<rest>[^\^~]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    if (match.Success && int.TryParse(match.Groups["qty"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
        quantity = Math.Max(1, parsed);

    return Regex.Replace(zpl, @"\^PQ[^\^~]*", string.Empty, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}

static EngineRenderReport RenderBinary(string zpl, string outputPath)
{
    var sw = Stopwatch.StartNew();
    try
    {
        IPrinterStorage storage = new PrinterStorage();
        var analyzer = new ZplAnalyzer(storage);
        var drawer = new ZplElementDrawer(storage);
        var analyze = analyzer.Analyze(zpl);

        if (analyze.LabelInfos.Length == 0)
            throw new InvalidOperationException("BinaryKits returned zero rendered labels.");

        var bytes = drawer.Draw(analyze.LabelInfos[^1].ZplElements, LabelWidthMm, LabelHeightMm, Dpmm);
        File.WriteAllBytes(outputPath, bytes);
        sw.Stop();

        return new EngineRenderReport(
            true,
            sw.Elapsed.TotalMilliseconds,
            new FileInfo(outputPath).Length,
            analyze.LabelInfos.Length,
            analyze.UnknownCommands,
            analyze.Errors,
            null,
            Process.GetCurrentProcess().WorkingSet64);
    }
    catch (Exception ex)
    {
        sw.Stop();
        return new EngineRenderReport(false, sw.Elapsed.TotalMilliseconds, 0, 0, [], [], ex.ToString(), Process.GetCurrentProcess().WorkingSet64);
    }
}

static EngineRenderReport RenderLabelize(string executable, string inputPath, string outputPath)
{
    var psi = new ProcessStartInfo
    {
        FileName = executable,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };
    psi.ArgumentList.Add("convert");
    psi.ArgumentList.Add(inputPath);
    psi.ArgumentList.Add("-o");
    psi.ArgumentList.Add(outputPath);
    psi.ArgumentList.Add("--width");
    psi.ArgumentList.Add(LabelWidthMm.ToString(CultureInfo.InvariantCulture));
    psi.ArgumentList.Add("--height");
    psi.ArgumentList.Add(LabelHeightMm.ToString(CultureInfo.InvariantCulture));
    psi.ArgumentList.Add("--dpmm");
    psi.ArgumentList.Add(Dpmm.ToString(CultureInfo.InvariantCulture));

    var sw = Stopwatch.StartNew();
    using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start Labelize.");
    var stdoutTask = process.StandardOutput.ReadToEndAsync();
    var stderrTask = process.StandardError.ReadToEndAsync();
    process.WaitForExit();
    Task.WaitAll(stdoutTask, stderrTask);
    sw.Stop();

    var success = process.ExitCode == 0 && File.Exists(outputPath);
    var error = success ? null : $"Exit={process.ExitCode}; stdout={stdoutTask.Result}; stderr={stderrTask.Result}";
    long peak = 0;
    try { peak = process.PeakWorkingSet64; } catch { }

    return new EngineRenderReport(
        success,
        sw.Elapsed.TotalMilliseconds,
        success ? new FileInfo(outputPath).Length : 0,
        success ? 1 : 0,
        [],
        [],
        error,
        peak);
}

static ImageDiff CompareMonochrome(string leftPath, string rightPath)
{
    using var left = SKBitmap.Decode(leftPath);
    using var right = SKBitmap.Decode(rightPath);
    if (left is null || right is null)
        return ImageDiff.NotComparable("PNG decode failed");
    if (left.Width != right.Width || left.Height != right.Height)
        return ImageDiff.NotComparable($"dimension mismatch {left.Width}x{left.Height} vs {right.Width}x{right.Height}");

    long mismatched = 0;
    long unionInk = 0;
    long intersectionInk = 0;
    var total = (long)left.Width * left.Height;

    for (var y = 0; y < left.Height; y++)
    {
        for (var x = 0; x < left.Width; x++)
        {
            var a = IsBlack(left.GetPixel(x, y));
            var b = IsBlack(right.GetPixel(x, y));
            if (a != b)
                mismatched++;
            if (a || b)
                unionInk++;
            if (a && b)
                intersectionInk++;
        }
    }

    var mismatchPct = total == 0 ? 0d : mismatched * 100d / total;
    var iou = unionInk == 0 ? 1d : intersectionInk * 100d / unionInk;
    return new ImageDiff(true, left.Width, left.Height, mismatchPct, iou, null);
}

static bool IsBlack(SKColor color)
{
    var luminance = (color.Red * 299 + color.Green * 587 + color.Blue * 114) / 1000;
    return luminance < 128;
}

static DecodeResult DecodeBarcode(string path, BarcodeFormat expectedFormat)
{
    try
    {
        using var bitmap = SKBitmap.Decode(path);
        if (bitmap is null)
            return DecodeResult.Failed("PNG decode failed");

        var rgb = new byte[checked(bitmap.Width * bitmap.Height * 3)];
        var offset = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                rgb[offset++] = color.Red;
                rgb[offset++] = color.Green;
                rgb[offset++] = color.Blue;
            }
        }

        var reader = new BarcodeReaderGeneric
        {
            AutoRotate = false,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = [expectedFormat]
            }
        };
        var result = reader.Decode(rgb, bitmap.Width, bitmap.Height, RGBLuminanceSource.BitmapFormat.RGB24);
        return result is null
            ? DecodeResult.Failed("ZXing returned no result")
            : new DecodeResult(true, result.BarcodeFormat.ToString(), result.Text, null);
    }
    catch (Exception ex)
    {
        return DecodeResult.Failed(ex.Message);
    }
}

static BenchmarkReport RunBenchmark(string labelizeExe, string outputRoot, List<GateCase> corpus, int count)
{
    var normalized = corpus.Select(item => NormalizeQuantity(item.Zpl, out _)).ToArray();

    // Warm-up each engine outside the timed batch.
    _ = RenderBinaryBytes(normalized[0]);
    var benchDir = Path.Combine(outputRoot, "bench");
    Directory.CreateDirectory(benchDir);
    var labelizeInputs = new string[corpus.Count];
    for (var i = 0; i < corpus.Count; i++)
    {
        var path = Path.Combine(benchDir, $"input-{i}.zpl");
        File.WriteAllText(path, normalized[i], new UTF8Encoding(false));
        labelizeInputs[i] = path;
    }

    var labelizeWarmOutput = Path.Combine(benchDir, "warm.png");
    _ = RenderLabelize(labelizeExe, labelizeInputs[0], labelizeWarmOutput);

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    var process = Process.GetCurrentProcess();
    var binaryStartMemory = process.WorkingSet64;
    var swBinary = Stopwatch.StartNew();
    long binaryBytes = 0;
    for (var i = 0; i < count; i++)
        binaryBytes += RenderBinaryBytes(normalized[i % normalized.Length]).LongLength;
    swBinary.Stop();
    process.Refresh();
    var binaryEndMemory = process.WorkingSet64;

    var swLabelize = Stopwatch.StartNew();
    long labelizeBytes = 0;
    long labelizePeakWorkingSet = 0;
    var outputPath = Path.Combine(benchDir, "labelize-bench.png");
    for (var i = 0; i < count; i++)
    {
        if (File.Exists(outputPath))
            File.Delete(outputPath);
        var result = RenderLabelize(labelizeExe, labelizeInputs[i % labelizeInputs.Length], outputPath);
        if (!result.Success)
            throw new InvalidOperationException($"Labelize benchmark failed at {i}: {result.Error}");
        labelizeBytes += result.OutputBytes;
        labelizePeakWorkingSet = Math.Max(labelizePeakWorkingSet, result.WorkingSetBytes);
    }
    swLabelize.Stop();

    return new BenchmarkReport(
        count,
        swBinary.Elapsed.TotalMilliseconds,
        count / swBinary.Elapsed.TotalSeconds,
        binaryBytes,
        Math.Max(0, binaryEndMemory - binaryStartMemory),
        swLabelize.Elapsed.TotalMilliseconds,
        count / swLabelize.Elapsed.TotalSeconds,
        labelizeBytes,
        labelizePeakWorkingSet);
}

static byte[] RenderBinaryBytes(string zpl)
{
    IPrinterStorage storage = new PrinterStorage();
    var analyzer = new ZplAnalyzer(storage);
    var drawer = new ZplElementDrawer(storage);
    var analyze = analyzer.Analyze(zpl);
    if (analyze.Errors.Length > 0)
        throw new InvalidOperationException(string.Join(" | ", analyze.Errors));
    if (analyze.LabelInfos.Length == 0)
        throw new InvalidOperationException("BinaryKits returned zero labels.");
    return drawer.Draw(analyze.LabelInfos[^1].ZplElements, LabelWidthMm, LabelHeightMm, Dpmm);
}

static string BuildMarkdown(GateReport report)
{
    var sb = new StringBuilder();
    sb.AppendLine("# F1 ZPL Gate — synthetic probe results");
    sb.AppendLine();
    sb.AppendLine($"Generated: {report.GeneratedUtc:O}");
    sb.AppendLine();
    sb.AppendLine("## Per-case results");
    sb.AppendLine();
    sb.AppendLine("| Case | Qty | BinaryKits | Labelize | Mono mismatch | Ink IoU | BK unknown/errors | Barcode BK | Barcode Labelize |");
    sb.AppendLine("|---|---:|---|---|---:|---:|---|---|---|");
    foreach (var item in report.Cases)
    {
        var bkIssues = item.BinaryKits.UnknownCommands.Length + item.BinaryKits.Errors.Length;
        var mismatch = item.Diff.Comparable ? $"{item.Diff.MismatchPercent:F2}%" : "n/a";
        var iou = item.Diff.Comparable ? $"{item.Diff.InkIntersectionOverUnionPercent:F2}%" : "n/a";
        var bkDecode = FormatDecode(item.ExpectedBarcode, item.BinaryDecode);
        var lzDecode = FormatDecode(item.ExpectedBarcode, item.LabelizeDecode);
        sb.AppendLine($"| {item.Name} | {item.Quantity} | {(item.BinaryKits.Success ? "PASS" : "FAIL")} | {(item.Labelize.Success ? "PASS" : "FAIL")} | {mismatch} | {iou} | {bkIssues} | {bkDecode} | {lzDecode} |");
    }

    sb.AppendLine();
    sb.AppendLine("## Benchmarks");
    sb.AppendLine();
    sb.AppendLine("| Designs | BinaryKits ms | BinaryKits labels/s | Labelize CLI ms | Labelize CLI labels/s | Labelize peak WS MiB |");
    sb.AppendLine("|---:|---:|---:|---:|---:|---:|");
    foreach (var benchmark in report.Benchmarks)
    {
        sb.AppendLine($"| {benchmark.DesignCount} | {benchmark.BinaryKitsMilliseconds:F1} | {benchmark.BinaryKitsPerSecond:F1} | {benchmark.LabelizeMilliseconds:F1} | {benchmark.LabelizePerSecond:F1} | {benchmark.LabelizePeakWorkingSetBytes / 1048576d:F1} |");
    }

    sb.AppendLine();
    sb.AppendLine("Notes: ^PQ is extracted by the SG probe before either renderer because project semantics require quantity to remain metadata, not repeated renders. Pixel comparison thresholds both images to black/white before calculating mismatch and ink IoU.");
    return sb.ToString();
}

static string FormatDecode(ExpectedBarcode? expected, DecodeResult? actual)
{
    if (expected is null)
        return "—";
    if (actual is null || !actual.Success)
        return "FAIL";
    return string.Equals(expected.Text, actual.Text, StringComparison.Ordinal)
        ? "PASS"
        : $"FAIL ({actual.Text})";
}

sealed record GateCase(string Name, string Purpose, string Zpl, ExpectedBarcode? ExpectedBarcode = null);
sealed record ExpectedBarcode(BarcodeFormat Format, string Text);
sealed record EngineRenderReport(bool Success, double Milliseconds, long OutputBytes, int LabelCount, string[] UnknownCommands, string[] Errors, string? Error, long WorkingSetBytes);
sealed record ImageDiff(bool Comparable, int Width, int Height, double MismatchPercent, double InkIntersectionOverUnionPercent, string? Reason)
{
    public static ImageDiff NotComparable(string reason) => new(false, 0, 0, 0, 0, reason);
}
sealed record DecodeResult(bool Success, string? Format, string? Text, string? Error)
{
    public static DecodeResult Failed(string error) => new(false, null, null, error);
}
sealed record CaseReport(string Name, string Purpose, int Quantity, EngineRenderReport BinaryKits, EngineRenderReport Labelize, ImageDiff Diff, ExpectedBarcode? ExpectedBarcode, DecodeResult? BinaryDecode, DecodeResult? LabelizeDecode);
sealed record BenchmarkReport(int DesignCount, double BinaryKitsMilliseconds, double BinaryKitsPerSecond, long BinaryKitsOutputBytes, long BinaryKitsWorkingSetDeltaBytes, double LabelizeMilliseconds, double LabelizePerSecond, long LabelizeOutputBytes, long LabelizePeakWorkingSetBytes);
sealed record GateReport(DateTimeOffset GeneratedUtc, string BinaryKitsVersion, string LabelizeVersion, double WidthMm, double HeightMm, int Dpmm, List<CaseReport> Cases, List<BenchmarkReport> Benchmarks);
