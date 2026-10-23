using System.IO;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Conversions;

public sealed class ImageToPdfExporter
{
    private const string ExifOrientationQuery = "/app1/ifd/{ushort=274}";
    private const double A4PortraitWidthPoints = 595.28d;
    private const double A4PortraitHeightPoints = 841.89d;
    private const double PageMarginPoints = 18d;
    private const long MaximumImagePixels = 40_000_000;
    private const long MaximumTotalPixels = 50_000_000;
    private const int MaximumImageDimension = 30_000;

    public void Export(
        IReadOnlyList<string> orderedPaths,
        string destinationPdfPath,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(orderedPaths);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPdfPath);
        token.ThrowIfCancellationRequested();

        if (orderedPaths.Count == 0)
            throw new ArgumentException("Selecciona al menos una imagen para crear el PDF.", nameof(orderedPaths));

        string destination = Path.GetFullPath(destinationPdfPath);
        if (!string.Equals(Path.GetExtension(destination), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El destino debe tener extensión .pdf.", nameof(destinationPdfPath));

        string directory = Path.GetDirectoryName(destination)
            ?? throw new ArgumentException("El destino no contiene una carpeta válida.", nameof(destinationPdfPath));
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"La carpeta de destino no existe: {directory}");
        if (File.Exists(destination) || Directory.Exists(destination))
            throw new IOException($"El archivo de salida ya existe: {destination}");

        string[] sources = NormalizeSourcePaths(orderedPaths, destination);
        string[] fingerprints = sources.Select(ComputeFingerprint).ToArray();
        var dimensions = new (int Width, int Height)[sources.Length];
        long totalPixels = 0;

        for (var index = 0; index < sources.Length; index++)
        {
            token.ThrowIfCancellationRequested();
            EnsureFingerprintUnchanged(sources[index], fingerprints[index]);
            BitmapSource image = LoadNormalizedImage(sources[index], token);
            dimensions[index] = (image.PixelWidth, image.PixelHeight);
            long pixels = checked((long)image.PixelWidth * image.PixelHeight);
            if (pixels > MaximumImagePixels)
                throw new InvalidDataException($"La imagen '{Path.GetFileName(sources[index])}' supera el tamaño máximo admitido.");
            totalPixels = checked(totalPixels + pixels);
            if (totalPixels > MaximumTotalPixels)
                throw new InvalidDataException("El conjunto de imágenes es demasiado grande para crear el PDF de forma segura.");
        }

        token.ThrowIfCancellationRequested();
        string temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(destination)}.{Guid.NewGuid():N}.sgpdf.tmp");
        bool ownsTemporary = false;

        try
        {
            using (var output = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.ReadWrite,
                       FileShare.None))
            {
                ownsTemporary = true;
                WritePdf(sources, dimensions, output, token);
                output.Flush(flushToDisk: true);
            }

            token.ThrowIfCancellationRequested();
            using (var reopened = PdfDocumentSession.Open(temporaryPath))
            {
                if (reopened.PageCount != sources.Length)
                {
                    throw new InvalidDataException(
                        $"El PDF resultante contiene {reopened.PageCount} páginas; se esperaban {sources.Length}.");
                }
            }

            for (var index = 0; index < sources.Length; index++)
                EnsureFingerprintUnchanged(sources[index], fingerprints[index]);
            token.ThrowIfCancellationRequested();

            File.Move(temporaryPath, destination, overwrite: false);
            ownsTemporary = false;
        }
        finally
        {
            if (ownsTemporary)
                TryDelete(temporaryPath);
        }
    }

    private static string[] NormalizeSourcePaths(IReadOnlyList<string> orderedPaths, string destination)
    {
        var sources = new string[orderedPaths.Count];
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < orderedPaths.Count; index++)
        {
            string path = orderedPaths[index];
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("La lista contiene una ruta de imagen vacía.", nameof(orderedPaths));

            string fullPath = Path.GetFullPath(path);
            string extension = Path.GetExtension(fullPath);
            if (!extension.Equals(".png", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException($"Solo se admiten imágenes PNG, JPG o JPEG: {Path.GetFileName(fullPath)}");
            }

            if (!seen.Add(fullPath))
                throw new ArgumentException("No se puede incluir dos veces la misma imagen.", nameof(orderedPaths));
            if (string.Equals(fullPath, destination, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("El PDF de salida no puede reemplazar una imagen de origen.", nameof(destination));
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"No se encontró la imagen '{Path.GetFileName(fullPath)}'.", fullPath);

            sources[index] = fullPath;
        }

        return sources;
    }

    private static void WritePdf(
        IReadOnlyList<string> sources,
        IReadOnlyList<(int Width, int Height)> dimensions,
        Stream output,
        CancellationToken token)
    {
        using var document = new PdfDocument();
        var loadedImages = new List<LoadedImage>(sources.Count);

        try
        {
            for (var index = 0; index < sources.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                BitmapSource bitmap = LoadNormalizedImage(sources[index], token);
                if (bitmap.PixelWidth != dimensions[index].Width || bitmap.PixelHeight != dimensions[index].Height)
                    throw new IOException($"La imagen '{Path.GetFileName(sources[index])}' cambió durante la conversión.");

                loadedImages.Add(LoadedImage.Create(bitmap));
                bool landscape = bitmap.PixelWidth > bitmap.PixelHeight;
                double pageWidth = landscape
                    ? A4PortraitHeightPoints
                    : A4PortraitWidthPoints;
                double pageHeight = landscape
                    ? A4PortraitWidthPoints
                    : A4PortraitHeightPoints;

                PdfPage page = document.AddPage();
                page.Width = XUnit.FromPoint(pageWidth);
                page.Height = XUnit.FromPoint(pageHeight);
                using XGraphics graphics = XGraphics.FromPdfPage(page);
                graphics.DrawRectangle(XBrushes.White, 0, 0, pageWidth, pageHeight);

                double availableWidth = pageWidth - (PageMarginPoints * 2d);
                double availableHeight = pageHeight - (PageMarginPoints * 2d);
                double scale = Math.Min(
                    availableWidth / bitmap.PixelWidth,
                    availableHeight / bitmap.PixelHeight);
                double drawWidth = bitmap.PixelWidth * scale;
                double drawHeight = bitmap.PixelHeight * scale;
                double x = (pageWidth - drawWidth) / 2d;
                double y = (pageHeight - drawHeight) / 2d;
                graphics.DrawImage(loadedImages[^1].Image, x, y, drawWidth, drawHeight);
            }

            token.ThrowIfCancellationRequested();
            document.Save(output, closeStream: false);
        }
        finally
        {
            foreach (LoadedImage image in loadedImages)
                image.Dispose();
        }
    }

    private static BitmapSource LoadNormalizedImage(string path, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        string extension = Path.GetExtension(path);

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Span<byte> signature = stackalloc byte[8];
            int read = stream.ReadAtLeast(signature, signature.Length, throwOnEndOfStream: false);
            bool isPng = read >= 8 && signature.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            bool isJpeg = read >= 3 && signature[0] == 0xff && signature[1] == 0xd8 && signature[2] == 0xff;
            if ((extension.Equals(".png", StringComparison.OrdinalIgnoreCase) && !isPng) ||
                ((extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)) && !isJpeg))
            {
                throw new InvalidDataException($"El contenido de '{Path.GetFileName(path)}' no coincide con su extensión.");
            }

            stream.Position = 0;
            BitmapDecoder decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count == 0)
                throw new InvalidDataException($"La imagen '{Path.GetFileName(path)}' no contiene cuadros decodificables.");

            BitmapFrame frame = decoder.Frames[0];
            int orientation = isJpeg ? GetExifOrientation(frame) : 1;
            int sourceWidth = frame.PixelWidth;
            int sourceHeight = frame.PixelHeight;
            ValidateDimensions(path, sourceWidth, sourceHeight);
            BitmapSource bgra = frame.Format == PixelFormats.Bgra32
                ? frame
                : new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0d);
            bgra.Freeze();

            int outputWidth = orientation is >= 5 and <= 8 ? sourceHeight : sourceWidth;
            int outputHeight = orientation is >= 5 and <= 8 ? sourceWidth : sourceHeight;
            int sourceStride = checked(sourceWidth * 4);
            int outputStride = checked(outputWidth * 4);
            var sourcePixels = new byte[checked(sourceStride * sourceHeight)];
            bgra.CopyPixels(sourcePixels, sourceStride, 0);
            var outputPixels = new byte[checked(outputStride * outputHeight)];

            for (var y = 0; y < outputHeight; y++)
            for (var x = 0; x < outputWidth; x++)
            {
                (int sourceX, int sourceY) = MapToSource(x, y, sourceWidth, sourceHeight, orientation);
                int sourceOffset = checked(sourceY * sourceStride + sourceX * 4);
                int outputOffset = checked(y * outputStride + x * 4);
                byte alpha = sourcePixels[sourceOffset + 3];
                outputPixels[outputOffset] = CompositeOnWhite(sourcePixels[sourceOffset], alpha);
                outputPixels[outputOffset + 1] = CompositeOnWhite(sourcePixels[sourceOffset + 1], alpha);
                outputPixels[outputOffset + 2] = CompositeOnWhite(sourcePixels[sourceOffset + 2], alpha);
                outputPixels[outputOffset + 3] = byte.MaxValue;
            }

            var normalized = BitmapSource.Create(
                outputWidth,
                outputHeight,
                96d,
                96d,
                PixelFormats.Bgra32,
                null,
                outputPixels,
                outputStride);
            normalized.Freeze();
            token.ThrowIfCancellationRequested();
            return normalized;
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is FileFormatException or NotSupportedException or ArgumentException or
                InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            throw new InvalidDataException($"No se pudo decodificar la imagen '{Path.GetFileName(path)}'.", exception);
        }
    }

    private static void ValidateDimensions(string path, int width, int height)
    {
        if (width <= 0 || height <= 0 || width > MaximumImageDimension || height > MaximumImageDimension)
            throw new InvalidDataException($"La imagen '{Path.GetFileName(path)}' tiene dimensiones no admitidas.");

        long pixelCount = checked((long)width * height);
        if (pixelCount > MaximumImagePixels)
            throw new InvalidDataException($"La imagen '{Path.GetFileName(path)}' supera el tamaño máximo admitido.");
    }

    private static int GetExifOrientation(BitmapFrame frame)
    {
        if (frame.Metadata is not BitmapMetadata metadata || !metadata.ContainsQuery(ExifOrientationQuery))
            return 1;

        object? value = metadata.GetQuery(ExifOrientationQuery);
        int orientation = value switch
        {
            ushort number => number,
            short number => number,
            uint number => checked((int)number),
            int number => number,
            _ => 0
        };

        if (orientation is < 1 or > 8)
            throw new InvalidDataException("La orientación EXIF de la imagen no es válida.");
        return orientation;
    }

    private static (int X, int Y) MapToSource(int x, int y, int width, int height, int orientation)
        => orientation switch
        {
            1 => (x, y),
            2 => (width - 1 - x, y),
            3 => (width - 1 - x, height - 1 - y),
            4 => (x, height - 1 - y),
            5 => (y, x),
            6 => (y, height - 1 - x),
            7 => (width - 1 - y, height - 1 - x),
            8 => (width - 1 - y, x),
            _ => throw new ArgumentOutOfRangeException(nameof(orientation))
        };

    private static byte CompositeOnWhite(byte channel, byte alpha)
        => (byte)((channel * alpha + 255 * (255 - alpha) + 127) / 255);

    private static string ComputeFingerprint(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static void EnsureFingerprintUnchanged(string path, string expected)
    {
        if (!string.Equals(ComputeFingerprint(path), expected, StringComparison.Ordinal))
            throw new IOException($"La imagen '{Path.GetFileName(path)}' cambió durante la conversión; no se guardó el PDF.");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class LoadedImage : IDisposable
    {
        private LoadedImage(MemoryStream stream, XImage image)
        {
            Stream = stream;
            Image = image;
        }

        public MemoryStream Stream { get; }
        public XImage Image { get; }

        public static LoadedImage Create(BitmapSource bitmap)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            var stream = new MemoryStream();
            try
            {
                encoder.Save(stream);
                stream.Position = 0;
                return new LoadedImage(stream, XImage.FromStream(stream));
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            Image.Dispose();
            Stream.Dispose();
        }
    }
}
