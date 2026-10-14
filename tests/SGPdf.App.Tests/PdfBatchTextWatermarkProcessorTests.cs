using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfBatchTextWatermarkProcessorTests
{
    [Fact]
    public void ProcessOne_TextWatermark_CreatesCopyAndPreservesOriginal()
    {
        using var temp = new TempDirectory();
        string inputPath = Path.Combine(temp.Path, "entrada.pdf");
        string outputDirectory = Path.Combine(temp.Path, "salida");
        CreateSinglePagePdf(inputPath);
        byte[] originalBefore = SHA256.HashData(File.ReadAllBytes(inputPath));

        Assembly appAssembly = typeof(PdfDocumentSession).Assembly;
        Type? processorType = appAssembly.GetType(
            "SGPdf.App.Features.Batch.PdfBatchTextWatermarkProcessor",
            throwOnError: false);
        Assert.NotNull(processorType);

        object processor = Activator.CreateInstance(processorType!, nonPublic: true)!;
        object watermarkRequest = CreateWatermarkRequest(appAssembly);
        MethodInfo? processOne = processorType!.GetMethod(
            "ProcessOne",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(processOne);

        object? result = processOne!.Invoke(
            processor,
            [inputPath, outputDirectory, watermarkRequest, CancellationToken.None]);
        Assert.NotNull(result);

        Type resultType = result!.GetType();
        bool success = (bool)resultType.GetProperty("Success")!.GetValue(result)!;
        string outputPath = (string)resultType.GetProperty("OutputPath")!.GetValue(result)!;
        string? error = (string?)resultType.GetProperty("ErrorMessage")!.GetValue(result);

        Assert.True(success, error);
        Assert.Equal(Path.Combine(outputDirectory, "entrada-marca-agua.pdf"), outputPath);
        Assert.True(File.Exists(outputPath));
        Assert.Equal(originalBefore, SHA256.HashData(File.ReadAllBytes(inputPath)));
        Assert.NotEqual(originalBefore, SHA256.HashData(File.ReadAllBytes(outputPath)));
    }

    private static object CreateWatermarkRequest(Assembly appAssembly)
    {
        Type requestType = appAssembly.GetType(
            "SGPdf.App.Features.Utilities.WatermarkUtilityRequest",
            throwOnError: true)!;
        return Activator.CreateInstance(
            requestType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args:
            [
                "LOTE",
                24d,
                0.5d,
                0d,
                0d,
                0d,
                1,
                1,
                false,
                false,
                "Center",
                (byte)128,
                (byte)128,
                (byte)128
            ],
            culture: null)!;
    }

    private static void CreateSinglePagePdf(string path)
    {
        const string content = "BT /F1 12 Tf 72 720 Td (BASE) Tj ET";
        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        ];

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true)
        {
            NewLine = "\n"
        };
        writer.WriteLine("%PDF-1.4");
        writer.Flush();

        var offsets = new List<long> { 0 };
        for (int index = 0; index < objects.Length; index++)
        {
            offsets.Add(stream.Position);
            writer.WriteLine($"{index + 1} 0 obj");
            writer.WriteLine(objects[index]);
            writer.WriteLine("endobj");
            writer.Flush();
        }

        long xrefOffset = stream.Position;
        writer.WriteLine("xref");
        writer.WriteLine($"0 {objects.Length + 1}");
        writer.WriteLine("0000000000 65535 f ");
        for (int index = 1; index < offsets.Count; index++)
            writer.WriteLine($"{offsets[index]:D10} 00000 n ");
        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Length + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefOffset);
        writer.WriteLine("%%EOF");
        writer.Flush();

        File.WriteAllBytes(path, stream.ToArray());
    }

    private sealed class TempDirectory : IDisposable
    {
        internal TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "sgpdf-batch-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        internal string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }
    }
}
