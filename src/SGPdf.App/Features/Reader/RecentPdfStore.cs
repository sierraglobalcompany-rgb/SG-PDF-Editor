using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SGPdf.App.Features.Reader;

internal sealed record RecentPdfEntry(string FullPath, DateTimeOffset LastOpenedUtc);

internal sealed class RecentPdfStore
{
    private const int ManifestVersion = 1;
    private const int MaxItems = 10;
    private const string ManifestFileName = "recent-files.json";
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly RecentPdfFileOps _fileOps;

    internal RecentPdfStore(string? rootDirectory = null, RecentPdfFileOps? fileOps = null)
    {
        RootDirectory = rootDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SG PDF Editor");
        _fileOps = fileOps ?? new RecentPdfFileOps();
    }

    internal string RootDirectory { get; }

    private string ManifestPath => Path.Combine(RootDirectory, ManifestFileName);

    internal IReadOnlyList<RecentPdfEntry> Load()
        => TryLoad(out var entries) ? entries : Array.Empty<RecentPdfEntry>();

    internal IReadOnlyList<RecentPdfEntry> RecordSuccessfulOpen(string path, DateTimeOffset openedUtc)
    {
        var normalizedPath = NormalizePath(path);
        var current = TryLoad(out var loaded)
            ? loaded.ToList()
            : new List<RecentPdfEntry>();

        current.RemoveAll(item => PathComparer.Equals(item.FullPath, normalizedPath));
        current.Insert(0, new RecentPdfEntry(normalizedPath, openedUtc.ToUniversalTime()));
        if (current.Count > MaxItems)
            current.RemoveRange(MaxItems, current.Count - MaxItems);

        WriteAtomic(current);
        return current;
    }

    internal IReadOnlyList<RecentPdfEntry> Remove(string path)
    {
        var normalizedPath = NormalizePath(path);
        if (!TryLoad(out var loaded))
            return Array.Empty<RecentPdfEntry>();

        var updated = loaded
            .Where(item => !PathComparer.Equals(item.FullPath, normalizedPath))
            .ToList();
        if (updated.Count == loaded.Count)
            return loaded;

        WriteAtomic(updated);
        return updated;
    }

    internal void Clear()
    {
        if (!File.Exists(ManifestPath))
            return;

        _fileOps.DeleteFile(ManifestPath);
    }

    private bool TryLoad(out IReadOnlyList<RecentPdfEntry> entries)
    {
        entries = Array.Empty<RecentPdfEntry>();
        if (!Directory.Exists(RootDirectory) || !File.Exists(ManifestPath))
            return true;

        RecentManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<RecentManifest>(File.ReadAllText(ManifestPath), JsonOptions);
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        if (manifest is null || manifest.Version != ManifestVersion || manifest.Items is null)
            return false;

        var result = new List<RecentPdfEntry>(Math.Min(manifest.Items.Count, MaxItems));
        var seen = new HashSet<string>(PathComparer);
        foreach (var item in manifest.Items)
        {
            if (result.Count == MaxItems)
                break;
            if (item is null || string.IsNullOrWhiteSpace(item.FullPath))
                return false;

            string normalizedPath;
            try
            {
                normalizedPath = NormalizePath(item.FullPath);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return false;
            }

            if (!seen.Add(normalizedPath))
                continue;

            result.Add(new RecentPdfEntry(normalizedPath, item.LastOpenedUtc.ToUniversalTime()));
        }

        entries = result;
        return true;
    }

    private void WriteAtomic(IReadOnlyList<RecentPdfEntry> entries)
    {
        var rootExisted = Directory.Exists(RootDirectory);
        Directory.CreateDirectory(RootDirectory);
        var tempPath = Path.Combine(RootDirectory, $".recent-files.{Guid.NewGuid():N}.tmp");
        var manifest = new RecentManifest
        {
            Version = ManifestVersion,
            Items = entries.Select(item => (RecentManifestItem?)new RecentManifestItem
            {
                FullPath = item.FullPath,
                LastOpenedUtc = item.LastOpenedUtc.ToUniversalTime()
            }).ToList()
        };

        try
        {
            var json = JsonSerializer.Serialize(manifest, JsonOptions);
            File.WriteAllText(tempPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            if (File.Exists(ManifestPath))
                _fileOps.ReplaceFile(tempPath, ManifestPath);
            else
                _fileOps.PublishNewFile(tempPath, ManifestPath);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // Best effort only. The authoritative manifest is never the temp file.
            }

            if (!rootExisted)
            {
                try
                {
                    if (Directory.Exists(RootDirectory) && !Directory.EnumerateFileSystemEntries(RootDirectory).Any())
                        Directory.Delete(RootDirectory);
                }
                catch
                {
                    // Best effort cleanup after a failed first mutation.
                }
            }
        }
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("La ruta del PDF reciente no puede estar vacía.", nameof(path));
        return Path.GetFullPath(path);
    }

    private sealed class RecentManifest
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("items")]
        public List<RecentManifestItem?>? Items { get; set; }
    }

    private sealed class RecentManifestItem
    {
        [JsonPropertyName("fullPath")]
        public string FullPath { get; set; } = string.Empty;

        [JsonPropertyName("lastOpenedUtc")]
        public DateTimeOffset LastOpenedUtc { get; set; }
    }
}
