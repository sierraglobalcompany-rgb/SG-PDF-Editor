using System.IO;
using System.Text.Json;

namespace SGPdf.App.Features.Sign;

internal sealed class SignatureLibraryUnavailableException : InvalidDataException
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

    internal SignatureLibraryStore(string? rootDirectory = null)
    {
        RootDirectory = rootDirectory is null
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SG PDF Editor",
                "Signatures")
            : Path.GetFullPath(rootDirectory);
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
