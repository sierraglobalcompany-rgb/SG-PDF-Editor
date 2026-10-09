namespace SGPdf.App.Features.Organize;

internal readonly record struct OrganizeSourceFingerprint(
    long FileLength,
    long LastWriteTimeUtcTicks);

internal sealed record OrganizeSource
{
    internal OrganizeSource(
        Guid sourceId,
        string path,
        int originalPageCount,
        OrganizeSourceFingerprint fingerprint)
    {
        if (sourceId == Guid.Empty)
            throw new ArgumentException("El identificador de origen es obligatorio.", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("La ruta del PDF es obligatoria.", nameof(path));
        if (originalPageCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(originalPageCount), "El PDF debe tener al menos una página.");

        SourceId = sourceId;
        Path = System.IO.Path.GetFullPath(path);
        OriginalPageCount = originalPageCount;
        Fingerprint = fingerprint;
    }

    internal Guid SourceId { get; }
    internal string Path { get; }
    internal int OriginalPageCount { get; }
    internal OrganizeSourceFingerprint Fingerprint { get; }

    internal static OrganizeSource Capture(string path, int pageCount)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("La ruta del PDF es obligatoria.", nameof(path));
        if (pageCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageCount), "El PDF debe tener al menos una página.");

        var normalizedPath = System.IO.Path.GetFullPath(path);
        var info = new FileInfo(normalizedPath);
        if (!info.Exists)
            throw new FileNotFoundException("No se encontró el archivo PDF.", normalizedPath);

        return new OrganizeSource(
            Guid.NewGuid(),
            normalizedPath,
            pageCount,
            new OrganizeSourceFingerprint(info.Length, info.LastWriteTimeUtc.Ticks));
    }

    internal bool MatchesCurrentFile()
    {
        try
        {
            var info = new FileInfo(Path);
            if (!info.Exists)
                return false;

            return info.Length == Fingerprint.FileLength &&
                   info.LastWriteTimeUtc.Ticks == Fingerprint.LastWriteTimeUtcTicks;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
