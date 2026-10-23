using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using SGPdf.App.Features.Labels;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Utilities;

internal sealed record PdfImageWatermarkResult(bool Succeeded, string? Error)
{
    internal static PdfImageWatermarkResult Success { get; } = new(true, string.Empty);
    internal static PdfImageWatermarkResult Failure(string error) => new(false, error);
}

internal sealed class PdfImageWatermarkCoordinator
{
    private const double ValidationDpi = 144d;
    private const long MaximumValidationPixels = 12_000_000L;
    private const double BoxTolerance = 0.5d;
    private static readonly TimeSpan QpdfTimeout = TimeSpan.FromSeconds(15);
    private static readonly HashSet<string> SupportedCatalogKeys = new(StringComparer.Ordinal)
    {
        "/Type", "/Pages", "/Version", "/Metadata"
    };
    private static readonly HashSet<string> SupportedPageKeys = new(StringComparer.Ordinal)
    {
        "/Type", "/Parent", "/Resources", "/MediaBox", "/CropBox", "/Rotate",
        "/Contents", "/BleedBox", "/TrimBox", "/ArtBox", "/UserUnit", "/Group"
    };

    private readonly string _qpdfPath;
    private readonly Func<ProcessStartInfo, TimeSpan, CancellationToken, Task<ChildProcessResult>> _runProcessAsync;

    internal PdfImageWatermarkCoordinator()
        : this(
            Path.Combine(AppContext.BaseDirectory, "qpdf", "qpdf.exe"),
            (startInfo, timeout, token) => ChildProcessRunner.RunAsync(startInfo, timeout, token))
    {
    }

    internal PdfImageWatermarkCoordinator(
        string qpdfPath,
        Func<ProcessStartInfo, TimeSpan, CancellationToken, Task<ChildProcessResult>> runProcessAsync)
    {
        _qpdfPath = qpdfPath ?? throw new ArgumentNullException(nameof(qpdfPath));
        _runProcessAsync = runProcessAsync ?? throw new ArgumentNullException(nameof(runProcessAsync));
    }

    internal async Task<PdfImageWatermarkResult> CreateCopyAsync(
        string sourcePath,
        string imagePath,
        string destinationPath,
        UtilityPageRange range,
        double widthPercent,
        double opacityPercent,
        WatermarkAnchor anchor,
        CancellationToken cancellationToken)
    {
        string? temporaryPath = null;
        try
        {
            ArgumentNullException.ThrowIfNull(range);
            cancellationToken.ThrowIfCancellationRequested();
            var source = Path.GetFullPath(sourcePath);
            var image = Path.GetFullPath(imagePath);
            var destination = Path.GetFullPath(destinationPath);
            ValidatePaths(source, image, destination);
            if (!File.Exists(_qpdfPath))
                return PdfImageWatermarkResult.Failure("No está disponible el verificador local requerido para esta operación.");

            var sourceHash = await ComputeSha256Async(source, cancellationToken).ConfigureAwait(false);
            var encryptionCheck = await RunQpdfAsync(source, cancellationToken).ConfigureAwait(false);
            if (encryptionCheck.ExitCode != 2)
                return PdfImageWatermarkResult.Failure("El PDF está cifrado o no se pudo demostrar que no lo esté.");

            cancellationToken.ThrowIfCancellationRequested();
            using var sourceSession = PdfDocumentSession.Open(source);
            if (sourceSession.OpenedWithPassword)
                return PdfImageWatermarkResult.Failure("Esta versión no admite PDF abierto con contraseña.");
            var signatureCount = sourceSession.GetCryptographicSignatureCount();
            if (signatureCount != 0)
                return PdfImageWatermarkResult.Failure("El PDF está firmado o no se pudo verificar que esté libre de firmas.");

            var sourceSnapshot = await Task.Run(
                () => CaptureAndValidateSource(source, sourceSession, range, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            var sourceImageCounts = await Task.Run(
                () => Enumerable.Range(0, sourceSession.PageCount)
                    .Select(page => sourceSession.GetImageObjects(page, cancellationToken).Count)
                    .ToArray(),
                cancellationToken).ConfigureAwait(false);
            var pageSizes = await Task.Run(
                () => sourceSession.GetPageSizes(cancellationToken),
                cancellationToken).ConfigureAwait(false);
            var asset = await Task.Run(
                () => PdfImageWatermarkAssetLoader.Load(image, opacityPercent, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            var placements = PdfImageWatermarkPlanner.Plan(
                asset.PixelWidth,
                asset.PixelHeight,
                widthPercent,
                range,
                pageSizes,
                anchor);

            temporaryPath = Path.Combine(
                Path.GetDirectoryName(destination)!,
                $".sgpdf-image-watermark-{Guid.NewGuid():N}.pdf");
            await Task.Run(
                () => PdfImageWatermarkWriter.WriteTemporaryCopy(
                    source,
                    temporaryPath,
                    asset,
                    placements,
                    cancellationToken),
                cancellationToken).ConfigureAwait(false);

            await Task.Run(
                () => ValidateOutput(
                    source,
                    temporaryPath,
                    sourceSession,
                    sourceSnapshot,
                    sourceImageCounts,
                    placements,
                    asset,
                    cancellationToken),
                cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            var currentSourceHash = await ComputeSha256Async(source, cancellationToken).ConfigureAwait(false);
            if (!CryptographicOperations.FixedTimeEquals(sourceHash, currentSourceHash))
                return PdfImageWatermarkResult.Failure("El PDF original cambió durante el proceso; no se publicó ninguna copia.");
            var currentImageHash = await ComputeSha256Async(image, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(asset.Sha256, Convert.ToHexString(currentImageHash), StringComparison.Ordinal))
                return PdfImageWatermarkResult.Failure("La imagen seleccionada cambió durante el proceso; no se publicó ninguna copia.");
            if (File.Exists(destination) || Directory.Exists(destination))
                return PdfImageWatermarkResult.Failure("El destino ya existe; no se sobrescribió.");

            File.Move(temporaryPath, destination, overwrite: false);
            temporaryPath = null;
            return PdfImageWatermarkResult.Success;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or InvalidDataException or
                NotSupportedException or IOException or UnauthorizedAccessException or TimeoutException)
        {
            return PdfImageWatermarkResult.Failure(exception.Message);
        }
        catch (Exception)
        {
            return PdfImageWatermarkResult.Failure("No se pudo validar la copia con marca de agua; no se publicó el resultado.");
        }
        finally
        {
            if (temporaryPath is not null)
                TryDeleteOwnedTemporary(temporaryPath);
        }
    }

    private async Task<ChildProcessResult> RunQpdfAsync(string sourcePath, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(_qpdfPath);
        startInfo.ArgumentList.Add("--is-encrypted");
        startInfo.ArgumentList.Add(sourcePath);
        return await _runProcessAsync(startInfo, QpdfTimeout, cancellationToken).ConfigureAwait(false);
    }

    private static PdfOperationPreservationSnapshot CaptureAndValidateSource(
        string sourcePath,
        PdfDocumentSession sourceSession,
        UtilityPageRange range,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (sourceSession.PageCount <= 0)
            throw new InvalidOperationException("El PDF no contiene páginas legibles.");
        if (sourceSession.GetOrganizeFormType(cancellationToken) != 0 ||
            sourceSession.GetBookmarks(cancellationToken).Count != 0 ||
            sourceSession.HasOrganizeNamedDestinations(cancellationToken) ||
            sourceSession.IsOrganizeTagged(cancellationToken) ||
            sourceSession.HasOrganizePageLabels(cancellationToken) ||
            sourceSession.HasOrganizeAttachments(cancellationToken))
        {
            throw new InvalidOperationException("El PDF contiene formularios, marcadores, destinos, etiquetas, adjuntos o estructura etiquetada que esta versión no puede acreditar.");
        }

        var selectedPages = range.Resolve(sourceSession.PageCount);
        if (selectedPages.Count > 150)
            throw new InvalidOperationException("Se pueden seleccionar hasta 150 páginas por operación.");
        foreach (var pageIndex in selectedPages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (sourceSession.GetPageRotation(pageIndex, cancellationToken) != 0)
                throw new InvalidOperationException($"La página {pageIndex + 1} tiene rotación PDF; esta versión solo admite rotación 0.");
            if (sourceSession.GetPageAnnotationCount(pageIndex, cancellationToken) != 0 ||
                sourceSession.GetPageLinks(pageIndex, cancellationToken).Count != 0)
            {
                throw new InvalidOperationException($"La página {pageIndex + 1} contiene anotaciones o enlaces que esta versión no puede acreditar.");
            }
        }

        var structure = CapturePdfSharpState(sourcePath, selectedPages, sourceSession, cancellationToken);
        _ = structure;
        return PdfOperationPreservationSnapshot.Capture(sourceSession, cancellationToken);
    }

    private static string CapturePdfSharpState(
        string path,
        IReadOnlyList<int> selectedPages,
        PdfDocumentSession session,
        CancellationToken cancellationToken)
    {
        using var document = PdfReader.Open(path, PdfDocumentOpenMode.Modify);
        if (document.PageCount != session.PageCount ||
            document.Internals.Catalog.Elements.Keys.Any(key => !SupportedCatalogKeys.Contains(key)) ||
            Enumerable.Range(0, document.PageCount)
                .Any(index => document.Pages[index].Elements.Keys.Any(key => !SupportedPageKeys.Contains(key))))
        {
            throw new InvalidOperationException("El PDF contiene una estructura de catálogo o página que esta versión no puede acreditar.");
        }

        var state = new List<string>();
        state.Add("xmp=" + GetXmpHash(document));
        for (var index = 0; index < document.PageCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = document.Pages[index];
            var media = page.MediaBoxReadOnly;
            var crop = page.EffectiveCropBoxReadOnly;
            ValidateRectangle(media, "MediaBox", index);
            ValidateRectangle(crop, "CropBox", index);
            var size = session.GetPageSize(index, cancellationToken);
            if (selectedPages.Contains(index))
            {
                if (!Near(media.X1, 0d) || !Near(media.Y1, 0d) ||
                    !Near(crop.X1, 0d) || !Near(crop.Y1, 0d) ||
                    !Near(media.X2, crop.X2) || !Near(media.Y2, crop.Y2) ||
                    !Near(media.X2, size.Width) || !Near(media.Y2, size.Height))
                {
                    throw new InvalidOperationException($"La página {index + 1} tiene un origen o CropBox ambiguo; no se aplicó la marca de agua.");
                }
            }

            state.Add($"page[{index}].media={media.X1:R},{media.Y1:R},{media.X2:R},{media.Y2:R}");
            state.Add($"page[{index}].crop={crop.X1:R},{crop.Y1:R},{crop.X2:R},{crop.Y2:R}");
        }
        return string.Join("\n", state);
    }

    private static void ValidateOutput(
        string sourcePath,
        string outputPath,
        PdfDocumentSession sourceSession,
        PdfOperationPreservationSnapshot sourceSnapshot,
        IReadOnlyList<int> sourceImageCounts,
        IReadOnlyList<PdfImageWatermarkPlacement> placements,
        PdfImageWatermarkAsset asset,
        CancellationToken cancellationToken)
    {
        using var output = PdfDocumentSession.Open(outputPath);
        if (output.OpenedWithPassword || output.PageCount != sourceSnapshot.PageCount)
            throw new InvalidOperationException("La copia guardada no preservó el número de páginas o no se pudo abrir sin contraseña.");
        var outputSnapshot = PdfOperationPreservationSnapshot.Capture(output, cancellationToken);
        if (!sourceSnapshot.HasSameContentAndStructuresAs(outputSnapshot, includeModificationDate: true))
            throw new InvalidOperationException("La copia no preservó el texto, metadatos y estructuras comprobables del original.");
        if (output.GetCryptographicSignatureCount() != 0)
            throw new InvalidOperationException("La salida contiene una firma inesperada o no se pudo verificar.");

        var selected = placements.Select(item => item.PageIndex).ToHashSet();
        for (var pageIndex = 0; pageIndex < output.PageCount; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var expectedImageCount = sourceImageCounts[pageIndex] + (selected.Contains(pageIndex) ? 1 : 0);
            if (output.GetImageObjects(pageIndex, cancellationToken).Count != expectedImageCount)
                throw new InvalidOperationException($"La copia no contiene exactamente una marca de agua en la página {pageIndex + 1} seleccionada.");

            var sourceSize = sourceSession.GetPageSize(pageIndex, cancellationToken);
            var outputSize = output.GetPageSize(pageIndex, cancellationToken);
            if (!Near(sourceSize.Width, outputSize.Width) || !Near(sourceSize.Height, outputSize.Height))
                throw new InvalidOperationException($"La copia cambió las dimensiones de la página {pageIndex + 1}.");
        }

        var sourceState = CapturePdfSharpState(sourcePath, Array.Empty<int>(), sourceSession, cancellationToken);
        var outputState = CapturePdfSharpState(outputPath, Array.Empty<int>(), output, cancellationToken);
        if (!string.Equals(sourceState, outputState, StringComparison.Ordinal))
            throw new InvalidOperationException("La copia cambió el XMP o las cajas de página del PDF original.");

        foreach (var placement in placements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            VerifySavedRender(sourceSession, output, placement, asset, cancellationToken);
        }
    }

    private static void VerifySavedRender(
        PdfDocumentSession source,
        PdfDocumentSession output,
        PdfImageWatermarkPlacement placement,
        PdfImageWatermarkAsset asset,
        CancellationToken cancellationToken)
    {
        var size = source.GetPageSize(placement.PageIndex, cancellationToken);
        var widthPixels = Math.Max(1L, (long)Math.Ceiling(size.Width * ValidationDpi / 72d));
        var heightPixels = Math.Max(1L, (long)Math.Ceiling(size.Height * ValidationDpi / 72d));
        if (widthPixels * heightPixels > MaximumValidationPixels)
            throw new InvalidOperationException("La página es demasiado grande para comprobar de forma segura la mezcla visual de la marca.");

        var before = source.RenderPage(placement.PageIndex, ValidationDpi, cancellationToken);
        var after = output.RenderPage(placement.PageIndex, ValidationDpi, cancellationToken);
        var matrix = placement.Matrix;
        var pixelBytes = asset.BgraPixels.Span;
        var bestScore = 0;
        var bestSample = default(BlendSample);
        var hasTransparentSample = false;
        BlendSample transparentSample = default;

        for (var gridY = 0; gridY <= 32; gridY++)
        for (var gridX = 0; gridX <= 32; gridX++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var imageX = Math.Min(asset.PixelWidth - 1, (int)((long)gridX * asset.PixelWidth / 32));
            var imageY = Math.Min(asset.PixelHeight - 1, (int)((long)gridY * asset.PixelHeight / 32));
            var imageOffset = checked(imageY * asset.Stride + imageX * 4);
            var alpha = pixelBytes[imageOffset + 3];
            var xPoints = matrix.E + ((imageX + 0.5d) / asset.PixelWidth * matrix.A);
            var yPoints = matrix.F + ((imageY + 0.5d) / asset.PixelHeight * matrix.D);
            var sample = ReadBlendSample(before, after, size.Height, xPoints, yPoints, pixelBytes, imageOffset, alpha);
            if (alpha == 0 && !hasTransparentSample)
            {
                transparentSample = sample;
                hasTransparentSample = true;
            }

            if (alpha == 0)
                continue;
            var score = Math.Max(
                Math.Abs(sample.Expected.R - sample.Source.R),
                Math.Max(
                    Math.Abs(sample.Expected.G - sample.Source.G),
                    Math.Abs(sample.Expected.B - sample.Source.B)));
            if (score > bestScore)
            {
                bestScore = score;
                bestSample = sample;
            }
        }

        if (bestScore < 8)
            throw new InvalidOperationException("ALPHA_FAIL: el render guardado no ofrece un píxel con mezcla visual demostrable.");
        if (!ColorNear(bestSample.Expected, bestSample.Output, tolerance: 35))
            throw new InvalidOperationException("ALPHA_FAIL: la mezcla de transparencia no coincide en el PDF guardado y reabierto.");
        if (hasTransparentSample && !ColorNear(transparentSample.Source, transparentSample.Output, tolerance: 2))
            throw new InvalidOperationException("ALPHA_FAIL: una zona completamente transparente alteró el contenido original.");
    }

    private static BlendSample ReadBlendSample(
        PdfRenderedPage before,
        PdfRenderedPage after,
        double pageHeight,
        double xPoints,
        double yPoints,
        ReadOnlySpan<byte> pixels,
        int imageOffset,
        byte alpha)
    {
        var x = Math.Clamp((int)Math.Round(xPoints * before.Dpi / 72d), 0, before.PixelWidth - 1);
        var y = Math.Clamp((int)Math.Round((pageHeight - yPoints) * before.Dpi / 72d), 0, before.PixelHeight - 1);
        var offset = checked(y * before.Stride + x * 4);
        var source = ReadColor(before.Pixels, offset);
        var actual = ReadColor(after.Pixels, checked(y * after.Stride + x * 4));
        var opacity = alpha / 255d;
        var expected = new PixelColor(
            Blend(source.R, pixels[imageOffset + 2], opacity),
            Blend(source.G, pixels[imageOffset + 1], opacity),
            Blend(source.B, pixels[imageOffset], opacity));
        return new BlendSample(source, expected, actual);
    }

    private static PixelColor ReadColor(byte[] pixels, int offset)
        => new(pixels[offset + 2], pixels[offset + 1], pixels[offset]);

    private static byte Blend(byte background, byte foreground, double alpha)
        => checked((byte)Math.Clamp((int)Math.Round(foreground * alpha + background * (1d - alpha)), 0, 255));

    private static bool ColorNear(PixelColor expected, PixelColor actual, int tolerance)
        => Math.Abs(expected.R - actual.R) <= tolerance &&
           Math.Abs(expected.G - actual.G) <= tolerance &&
           Math.Abs(expected.B - actual.B) <= tolerance;

    private static string GetXmpHash(PdfDocument document)
    {
        if (!document.Internals.Catalog.Elements.ContainsKey("/Metadata"))
            return "none";
        PdfItem? item = document.Internals.Catalog.Elements["/Metadata"];
        if (item is PdfReference reference)
            item = reference.Value;
        if (item is not PdfDictionary { Stream: not null } metadata)
            throw new InvalidOperationException("El XMP del PDF no se pudo validar.");
        var bytes = metadata.Stream.UnfilteredValue;
        if (bytes is null)
            throw new InvalidOperationException("El XMP del PDF no se pudo validar.");
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static void ValidateRectangle(PdfRectangle rectangle, string name, int pageIndex)
    {
        if (!double.IsFinite(rectangle.X1) || !double.IsFinite(rectangle.Y1) ||
            !double.IsFinite(rectangle.X2) || !double.IsFinite(rectangle.Y2) ||
            rectangle.X2 <= rectangle.X1 || rectangle.Y2 <= rectangle.Y1)
        {
            throw new InvalidOperationException($"La {name} de la página {pageIndex + 1} no es válida.");
        }
    }

    private static bool Near(double left, double right)
        => Math.Abs(left - right) <= BoxTolerance;

    private static void ValidatePaths(string source, string image, string destination)
    {
        if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El destino debe ser distinto del PDF original.");
        if (!string.Equals(Path.GetExtension(destination), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El destino debe tener extensión .pdf.");
        if (!File.Exists(source))
            throw new FileNotFoundException("No se encontró el PDF original.", source);
        if (!File.Exists(image))
            throw new FileNotFoundException("No se encontró la imagen seleccionada.", image);
        if (File.Exists(destination) || Directory.Exists(destination))
            throw new IOException("Ese destino ya existe. Elige otro nombre para conservarlo.");
        var directory = Path.GetDirectoryName(destination);
        if (directory is null || !Directory.Exists(directory))
            throw new DirectoryNotFoundException("No existe la carpeta de destino.");
    }

    private static async Task<byte[]> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, useAsync: true);
        return await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private static void TryDeleteOwnedTemporary(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private readonly record struct PixelColor(byte R, byte G, byte B);
    private readonly record struct BlendSample(PixelColor Source, PixelColor Expected, PixelColor Output);
}
