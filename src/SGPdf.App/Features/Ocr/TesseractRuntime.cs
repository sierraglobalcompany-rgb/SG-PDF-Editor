using System.IO;

namespace SGPdf.App.Features.Ocr;

internal static class TesseractRuntime
{
    private const string RuntimeDirectoryName = "tesseract";
    private const string ExecutableName = "tesseract.exe";
    private const string TessdataDirectoryName = "tessdata";

    internal static string ResolveExecutablePath(string? appBaseDirectory = null)
    {
        var baseDirectory = ResolveBaseDirectory(appBaseDirectory);
        var executablePath = Path.GetFullPath(
            Path.Combine(baseDirectory, RuntimeDirectoryName, ExecutableName));

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException(
                "No se encontró el runtime privado de Tesseract incluido con SG PDF Editor.",
                executablePath);
        }

        return executablePath;
    }

    internal static string ResolveTessdataDirectory(string? appBaseDirectory = null)
    {
        var baseDirectory = ResolveBaseDirectory(appBaseDirectory);
        var tessdataDirectory = Path.GetFullPath(
            Path.Combine(baseDirectory, RuntimeDirectoryName, TessdataDirectoryName));

        RequireLanguageData(tessdataDirectory, "spa.traineddata");
        RequireLanguageData(tessdataDirectory, "eng.traineddata");
        return tessdataDirectory;
    }

    private static string ResolveBaseDirectory(string? appBaseDirectory)
    {
        var candidate = string.IsNullOrWhiteSpace(appBaseDirectory)
            ? AppContext.BaseDirectory
            : appBaseDirectory;
        return Path.GetFullPath(candidate);
    }

    private static void RequireLanguageData(string tessdataDirectory, string fileName)
    {
        var path = Path.Combine(tessdataDirectory, fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"No se encontró el archivo OCR requerido '{fileName}' en el runtime privado.",
                path);
        }
    }
}
