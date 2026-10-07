using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace SGPdf.App.Features.Labels;

public sealed class LabelizeProcessRenderer
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly string _executablePath;
    private readonly TimeSpan _timeout;
    private readonly string _tempRoot;

    public LabelizeProcessRenderer()
        : this(
            LabelizeRuntime.ResolveBundledExecutablePath(AppContext.BaseDirectory),
            TimeSpan.FromSeconds(15),
            Path.GetTempPath())
    {
    }

    internal LabelizeProcessRenderer(
        string executablePath,
        TimeSpan timeout,
        string tempRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempRoot);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Render timeout must be positive.");

        _executablePath = Path.GetFullPath(executablePath);
        _timeout = timeout;
        _tempRoot = Path.GetFullPath(tempRoot);
    }

    public async Task<IReadOnlyList<ZplRenderedLabel>> RenderAsync(
        ZplDocument document,
        ZplRenderOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(_executablePath))
        {
            throw new FileNotFoundException(
                $"Labelize {LabelizeRuntime.Version} no está disponible para renderizar etiquetas.",
                _executablePath);
        }

        Directory.CreateDirectory(_tempRoot);
        var requestDirectory = Path.Combine(_tempRoot, $"sgpdf-labelize-{Guid.NewGuid():N}");
        Directory.CreateDirectory(requestDirectory);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var inputPath = Path.Combine(requestDirectory, "input.zpl");
            var outputPath = Path.Combine(requestDirectory, "label.png");
            await File.WriteAllTextAsync(
                inputPath,
                document.NormalizedRenderSource,
                Utf8NoBom,
                cancellationToken).ConfigureAwait(false);

            var startInfo = BuildStartInfo(inputPath, outputPath, options, requestDirectory);
            var result = await ChildProcessRunner.RunAsync(
                startInfo,
                _timeout,
                cancellationToken).ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                throw new InvalidDataException(
                    $"Labelize terminó con código {result.ExitCode}: {CompactDiagnostic(result.StandardError)}");
            }

            var outputFiles = ResolveOutputFiles(requestDirectory, outputPath);
            if (outputFiles.Count != document.Designs.Count)
            {
                throw new InvalidDataException(
                    $"Labelize produjo {outputFiles.Count} PNG(s), pero el documento contiene {document.Designs.Count} diseño(s) imprimible(s).");
            }

            var rendered = new List<ZplRenderedLabel>(outputFiles.Count);
            for (var index = 0; index < outputFiles.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var pngBytes = await File.ReadAllBytesAsync(outputFiles[index], cancellationToken).ConfigureAwait(false);
                ValidatePng(pngBytes, outputFiles[index]);

                rendered.Add(new ZplRenderedLabel(
                    document.Designs[index].Index,
                    options.WidthMm,
                    options.HeightMm,
                    options.Dpmm,
                    pngBytes));
            }

            return rendered;
        }
        finally
        {
            TryDeleteDirectory(requestDirectory);
        }
    }

    private ProcessStartInfo BuildStartInfo(
        string inputPath,
        string outputPath,
        ZplRenderOptions options,
        string workingDirectory)
    {
        var startInfo = new ProcessStartInfo(_executablePath)
        {
            WorkingDirectory = workingDirectory
        };

        startInfo.ArgumentList.Add("convert");
        startInfo.ArgumentList.Add(inputPath);
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(outputPath);
        startInfo.ArgumentList.Add("--format");
        startInfo.ArgumentList.Add("zpl");
        startInfo.ArgumentList.Add("--type");
        startInfo.ArgumentList.Add("png");
        startInfo.ArgumentList.Add("--width");
        startInfo.ArgumentList.Add(options.WidthMm.ToString("0.###", CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("--height");
        startInfo.ArgumentList.Add(options.HeightMm.ToString("0.###", CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("--dpmm");
        startInfo.ArgumentList.Add(options.Dpmm.ToString(CultureInfo.InvariantCulture));
        return startInfo;
    }

    private static IReadOnlyList<string> ResolveOutputFiles(string requestDirectory, string singleOutputPath)
    {
        var numbered = new List<(int Number, string Path)>();
        foreach (var path in Directory.EnumerateFiles(requestDirectory, "label_*.png", SearchOption.TopDirectoryOnly))
        {
            var stem = Path.GetFileNameWithoutExtension(path);
            const string prefix = "label_";
            if (!stem.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var suffix = stem[prefix.Length..];
            if (int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0)
                numbered.Add((number, path));
        }

        if (numbered.Count > 0)
        {
            return numbered
                .OrderBy(item => item.Number)
                .Select(item => item.Path)
                .ToArray();
        }

        return File.Exists(singleOutputPath) ? [singleOutputPath] : [];
    }

    private static void ValidatePng(byte[] bytes, string outputPath)
    {
        if (bytes.Length <= PngSignature.Length ||
            !bytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
        {
            throw new InvalidDataException(
                $"Labelize no produjo un PNG válido para '{Path.GetFileName(outputPath)}'.");
        }
    }

    private static string CompactDiagnostic(string standardError)
    {
        if (string.IsNullOrWhiteSpace(standardError))
            return "sin diagnóstico adicional";

        var compact = string.Join(
            " ",
            standardError.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return compact.Length <= 500 ? compact : compact[..500];
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup. A later startup cleanup can handle an OS-held file if one remains.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup if the OS temporarily holds a generated file.
        }
    }
}
