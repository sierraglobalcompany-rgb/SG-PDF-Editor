using System.IO;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowQrTests
{
    [Fact]
    public void QrUi_ReusesUtilitiesMenuAndExistingQrGenerator()
    {
        string root = FindRepositoryRoot();
        string utilities = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Utilities.cs"));
        string qr = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Qr.cs"));

        Assert.Contains("QrCodeMenuItem", utilities, StringComparison.Ordinal);
        Assert.Contains("Generar código _QR...", utilities, StringComparison.Ordinal);
        Assert.Contains("QrCode_Click", utilities, StringComparison.Ordinal);
        Assert.Contains("new QrCodeDialog", qr, StringComparison.Ordinal);
        Assert.Contains("new QrCodeGenerator()", qr, StringComparison.Ordinal);
        Assert.Contains("GeneratePngAsync", qr, StringComparison.Ordinal);
    }

    [Fact]
    public void QrUi_SavesExplicitPngWithOverwritePromptAndStaysQrOnly()
    {
        string root = FindRepositoryRoot();
        string qr = File.ReadAllText(Path.Combine(root, "src", "SGPdf.App", "MainWindow.Qr.cs"));

        Assert.Contains("SaveFileDialog", qr, StringComparison.Ordinal);
        Assert.Contains("Archivo PNG (*.png)|*.png", qr, StringComparison.Ordinal);
        Assert.Contains("OverwritePrompt = true", qr, StringComparison.Ordinal);
        Assert.Contains("WriteAllBytesAsync", qr, StringComparison.Ordinal);
        Assert.DoesNotContain("Code128", qr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BarcodeGenerator", qr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Symbology", qr, StringComparison.OrdinalIgnoreCase);
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
