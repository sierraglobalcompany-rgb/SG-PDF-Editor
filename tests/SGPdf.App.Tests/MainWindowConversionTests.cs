using System.IO;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowConversionTests
{
    [Fact]
    public void PdfToPngUi_ReusesUtilitiesOwnerAndFocusedConverterOnly()
    {
        string root = FindRepositoryRoot();
        string utilities = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Utilities.cs"));
        string conversion = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Conversions.cs"));

        Assert.Contains("ConvertPdfToPngMenuItem", utilities, StringComparison.Ordinal);
        Assert.Contains("Convertir PDF a _PNG...", utilities, StringComparison.Ordinal);
        Assert.Contains("pdfToPng.Click += ConvertPdfToPng_Click", utilities, StringComparison.Ordinal);
        Assert.Contains("OpenFolderDialog", conversion, StringComparison.Ordinal);
        Assert.Contains("new PdfToPngConverter().Convert", conversion, StringComparison.Ordinal);
        Assert.Contains("CanRunUtility()", conversion, StringComparison.Ordinal);
        Assert.DoesNotContain("ConversionEngine", conversion, StringComparison.Ordinal);
        Assert.DoesNotContain("LibreOffice", conversion, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SGPdf.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("No se encontró la raíz del repositorio.");
    }
}
