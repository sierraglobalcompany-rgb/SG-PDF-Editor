using System.IO;

namespace SGPdf.App.Features.Edit;

internal readonly record struct PdfEditSourceFingerprint(
    long FileLength,
    long LastWriteTimeUtcTicks)
{
    internal static PdfEditSourceFingerprint Capture(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("La ruta fuente es obligatoria.", nameof(path));

        var fullPath = Path.GetFullPath(path);
        var info = new FileInfo(fullPath);
        if (!info.Exists)
            throw new FileNotFoundException("No existe el PDF fuente para edición de imágenes.", fullPath);

        return new PdfEditSourceFingerprint(info.Length, info.LastWriteTimeUtc.Ticks);
    }

    internal bool MatchesCurrentFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var info = new FileInfo(Path.GetFullPath(path));
            return info.Exists
                && info.Length == FileLength
                && info.LastWriteTimeUtc.Ticks == LastWriteTimeUtcTicks;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
