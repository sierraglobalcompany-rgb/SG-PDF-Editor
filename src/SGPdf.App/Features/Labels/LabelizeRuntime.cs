using System.IO;

namespace SGPdf.App.Features.Labels;

public static class LabelizeRuntime
{
    public const string Version = "1.7.0";
    public const string ExecutableDirectoryName = "labelize";
    public const string ExecutableFileName = "labelize.exe";

    public static string ResolveBundledExecutablePath(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        var executablePath = Path.GetFullPath(Path.Combine(
            baseDirectory,
            ExecutableDirectoryName,
            ExecutableFileName));

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException(
                $"Labelize {Version} no está disponible en el runtime local esperado.",
                executablePath);
        }

        return executablePath;
    }
}
