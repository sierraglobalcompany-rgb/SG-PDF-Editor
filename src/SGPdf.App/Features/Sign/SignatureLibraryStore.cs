using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SGPdf.App.Features.Sign;

internal sealed class SignatureLibraryUnavailableException : IOException
{
    internal SignatureLibraryUnavailableException(string message)
        : base(message)
    {
    }

    internal SignatureLibraryUnavailableException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

internal sealed class SignatureLibraryStore
{
    private const string ManifestFileName = "library.json";
    private const int CurrentVersion = 1;
    private readonly SignatureLibraryFileOps _fileOps;

    internal SignatureLibraryStore(
        string? rootDirectory = null,
        SignatureLibraryFileOps? fileOps = null)
    {
        RootDirectory = rootDirectory is null
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SG PDF Editor",
                "Signatures")
            : Path.GetFullPath(rootDirectory);
        _fileOps = fileOps ?? new SignatureLibraryFileOps();
    }

    internal string RootDirectory { get; }

    internal IReadOnlyList<SignatureLibraryItem> Load()
    {
        if (!Directory.Exists(RootDirectory))
            return Array.Empty<SignatureLibraryItem>();

        var manifestPath = Path.Combine(RootDirectory, ManifestFileName);
        if (!File.Exists(manifestPath))
            return Array.Empty<SignatureLibraryItem>();

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw InvalidManifest("El manifiesto de firmas no tiene un objeto raíz válido.");

            if (!root.TryGetProperty("version", out var versionElement) ||
                versionElement.ValueKind != JsonValueKind.Number ||
                !versionElement.TryGetInt32(out var version) ||
                version != CurrentVersion)
            {
                throw InvalidManifest("La versión del manifiesto de firmas no es compatible.");
            }

            if (!root.TryGetProperty("items", out var itemsElement) ||
                itemsElement.ValueKind != JsonValueKind.Array)
            {
                throw InvalidManifest("El manifiesto de firmas no contiene una lista de elementos válida.");
            }

            var items = new List<SignatureLibraryItem>();
            var ids = new HashSet<Guid>();

            foreach (var itemElement in itemsElement.EnumerateArray())
            {
                if (itemElement.ValueKind != JsonValueKind.Object)
                    throw InvalidManifest("El manifiesto contiene un elemento inválido.");

                if (!itemElement.TryGetProperty("id", out var idElement) ||
                    idElement.ValueKind != JsonValueKind.String ||
                    !idElement.TryGetGuid(out var id) ||
                    id == Guid.Empty)
                {
                    throw InvalidManifest("El manifiesto contiene un identificador de firma inválido.");
                }

                if (!ids.Add(id))
                    throw InvalidManifest("El manifiesto contiene identificadores de firma duplicados.");

                if (!itemElement.TryGetProperty("displayName", out var displayNameElement) ||
                    displayNameElement.ValueKind != JsonValueKind.String)
                {
                    throw InvalidManifest("El manifiesto contiene un nombre de firma inválido.");
                }

                var displayName = displayNameElement.GetString();
                if (displayName is null)
                    throw InvalidManifest("El manifiesto contiene un nombre de firma inválido.");

                if (!itemElement.TryGetProperty("fileName", out var fileNameElement) ||
                    fileNameElement.ValueKind != JsonValueKind.String)
                {
                    throw InvalidManifest("El manifiesto contiene un nombre de archivo inválido.");
                }

                var fileName = fileNameElement.GetString();
                if (!IsSafePngFileName(fileName))
                    throw InvalidManifest("El manifiesto contiene una ruta de imagen no segura.");

                items.Add(new SignatureLibraryItem(id, displayName, fileName!));
            }

            return items;
        }
        catch (SignatureLibraryUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new SignatureLibraryUnavailableException(
                "No se pudo leer la biblioteca local de firmas sin riesgo de perder información.",
                ex);
        }
    }

    internal SignatureAsset LoadAsset(Guid id)
    {
        var item = Load().FirstOrDefault(candidate => candidate.Id == id)
            ?? throw new KeyNotFoundException("No se encontró la firma solicitada en la biblioteca local.");

        return SignaturePngLoader.Load(Path.Combine(RootDirectory, item.FileName));
    }

    internal SignatureLibraryItem Add(string displayName, SignatureAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        var currentItems = Load();
        var normalizedName = NormalizeDisplayName(displayName, currentItems, excludedId: null);
        var id = Guid.NewGuid();
        var item = new SignatureLibraryItem(id, normalizedName, $"{id:N}.png");

        Directory.CreateDirectory(RootDirectory);
        var finalPngPath = Path.Combine(RootDirectory, item.FileName);
        var tempPngPath = Path.Combine(RootDirectory, $".{id:N}.{Guid.NewGuid():N}.png.tmp");

        try
        {
            WritePng(tempPngPath, asset);
            _fileOps.PublishNewFile(tempPngPath, finalPngPath);
        }
        catch
        {
            TryDeleteTemporary(tempPngPath);
            throw;
        }

        try
        {
            var nextItems = currentItems.Concat(new[] { item }).ToArray();
            PublishManifest(nextItems);
            return item;
        }
        catch
        {
            try
            {
                _fileOps.DeleteFile(finalPngPath);
            }
            catch
            {
                // A failed rollback leaves a harmless orphan because library.json remains authoritative.
            }

            throw;
        }
    }

    internal SignatureLibraryItem Rename(Guid id, string displayName)
    {
        var currentItems = Load();
        var index = IndexOf(currentItems, id);
        if (index < 0)
            throw new KeyNotFoundException("No se encontró la firma solicitada en la biblioteca local.");

        var normalizedName = NormalizeDisplayName(displayName, currentItems, id);
        var renamed = currentItems[index] with { DisplayName = normalizedName };
        var nextItems = currentItems.ToArray();
        nextItems[index] = renamed;
        PublishManifest(nextItems);
        return renamed;
    }

    internal SignatureLibraryDeleteResult Delete(Guid id)
    {
        var currentItems = Load();
        var index = IndexOf(currentItems, id);
        if (index < 0)
            throw new KeyNotFoundException("No se encontró la firma solicitada en la biblioteca local.");

        var removed = currentItems[index];
        var nextItems = currentItems.Where(item => item.Id != id).ToArray();
        PublishManifest(nextItems);

        try
        {
            _fileOps.DeleteFile(Path.Combine(RootDirectory, removed.FileName));
            return new SignatureLibraryDeleteResult(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new SignatureLibraryDeleteResult(
                false,
                "La firma se eliminó de la biblioteca, pero su archivo PNG no pudo limpiarse. " + ex.Message);
        }
    }

    private void PublishManifest(IReadOnlyList<SignatureLibraryItem> items)
    {
        Directory.CreateDirectory(RootDirectory);
        var manifestPath = Path.Combine(RootDirectory, ManifestFileName);
        var tempPath = Path.Combine(RootDirectory, $".library.{Guid.NewGuid():N}.tmp");
        var manifest = new
        {
            version = CurrentVersion,
            items = items.Select(item => new
            {
                id = item.Id,
                displayName = item.DisplayName,
                fileName = item.FileName
            }).ToArray()
        };

        try
        {
            using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, manifest);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(manifestPath))
                _fileOps.ReplaceFile(tempPath, manifestPath);
            else
                _fileOps.PublishNewFile(tempPath, manifestPath);
        }
        catch
        {
            TryDeleteTemporary(tempPath);
            throw;
        }
    }

    private static void WritePng(string path, SignatureAsset asset)
    {
        var bitmap = BitmapSource.Create(
            asset.PixelWidth,
            asset.PixelHeight,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            asset.BgraPixels.ToArray(),
            asset.Stride);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            4096,
            FileOptions.WriteThrough);
        encoder.Save(stream);
        stream.Flush(flushToDisk: true);
    }

    private static string NormalizeDisplayName(
        string displayName,
        IReadOnlyList<SignatureLibraryItem> currentItems,
        Guid? excludedId)
    {
        if (displayName is null)
            throw new ArgumentNullException(nameof(displayName));

        var normalized = displayName.Trim();
        if (normalized.Length == 0)
            throw new ArgumentException("El nombre de la firma no puede estar vacío.", nameof(displayName));

        if (currentItems.Any(item =>
                item.Id != excludedId &&
                string.Equals(item.DisplayName, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Ya existe una firma con ese nombre.", nameof(displayName));
        }

        return normalized;
    }

    private static int IndexOf(IReadOnlyList<SignatureLibraryItem> items, Guid id)
    {
        for (var index = 0; index < items.Count; index++)
        {
            if (items[index].Id == id)
                return index;
        }

        return -1;
    }

    private static void TryDeleteTemporary(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Temporary cleanup is best effort and must never replace the primary failure.
        }
    }

    private static SignatureLibraryUnavailableException InvalidManifest(string message)
        => new(message);

    private static bool IsSafePngFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || Path.IsPathRooted(fileName))
            return false;

        if (!string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
            return false;

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return false;

        if (fileName.Contains('/') || fileName.Contains('\\'))
            return false;

        return string.Equals(Path.GetExtension(fileName), ".png", StringComparison.OrdinalIgnoreCase);
    }
}