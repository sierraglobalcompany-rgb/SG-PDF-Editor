using System.Printing;
using System.Xml.Linq;
using SGPdf.App.Features.Labels;
using SGPdf.App.Printing;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class WindowsThermalPrintPreflightTests
{
    [Fact]
    public void CreateCandidateTicket_SetsExactRequestedMediaInWpfUnits()
    {
        var dialogTicket = new PrintTicket();

        var candidate = WindowsThermalPrintPreflight.CreateCandidateTicket(
            dialogTicket,
            requestedWidthMm: 102,
            requestedHeightMm: 152,
            matchingResolution: null);

        Assert.NotNull(candidate.PageMediaSize);
        Assert.NotNull(candidate.PageMediaSize.Width);
        Assert.NotNull(candidate.PageMediaSize.Height);
        Assert.Equal(102d * 96d / 25.4d, candidate.PageMediaSize.Width!.Value, 6);
        Assert.Equal(152d * 96d / 25.4d, candidate.PageMediaSize.Height!.Value, 6);
    }

    [Fact]
    public void CreateCandidateTicket_ForcesCopyCountOne()
    {
        var dialogTicket = new PrintTicket { CopyCount = 7 };

        var candidate = WindowsThermalPrintPreflight.CreateCandidateTicket(
            dialogTicket,
            102,
            152,
            matchingResolution: null);

        Assert.Equal(1, candidate.CopyCount);
        Assert.Equal(7, dialogTicket.CopyCount);
    }

    [Fact]
    public void CreateCandidateTicket_PreservesSelectedMatchingResolution()
    {
        var dialogTicket = new PrintTicket();
        var matching = new PageResolution(203, 203);

        var candidate = WindowsThermalPrintPreflight.CreateCandidateTicket(
            dialogTicket,
            102,
            152,
            matching);

        Assert.NotNull(candidate.PageResolution);
        Assert.Equal(203, candidate.PageResolution.X);
        Assert.Equal(203, candidate.PageResolution.Y);
    }

    [Fact]
    public void ConvertImageableArea_ToMillimeters_PreservesOriginAndExtent()
    {
        var converted = WindowsThermalPrintPreflight.ConvertImageableAreaToMillimeters(
            originWidth: 96,
            originHeight: 48,
            extentWidth: 192,
            extentHeight: 96);

        Assert.Equal(25.4, converted.OriginX, 6);
        Assert.Equal(12.7, converted.OriginY, 6);
        Assert.Equal(50.8, converted.ExtentWidth, 6);
        Assert.Equal(25.4, converted.ExtentHeight, 6);
    }

    [Fact]
    public void BuildSummary_IncludesPrinterSizeResolutionQuantityAndNoScaling()
    {
        var plan = CreateThermalPlan(totalQuantity: 25, widthMm: 102, heightMm: 152);

        var summary = WindowsThermalPrintWorkflow.BuildSummary(
            printerName: "Zebra ZD421",
            plan,
            validatedDpi: 203,
            warnings: Array.Empty<string>());

        Assert.Contains("Zebra ZD421", summary, StringComparison.Ordinal);
        Assert.Contains("102", summary, StringComparison.Ordinal);
        Assert.Contains("152", summary, StringComparison.Ordinal);
        Assert.Contains("203", summary, StringComparison.Ordinal);
        Assert.Contains("25", summary, StringComparison.Ordinal);
        Assert.Contains("Copias de Windows: 1", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Escalado SG PDF Editor: ninguno", summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RuntimeProject_DoesNotAddSystemPrintingNuGetPackage()
    {
        var projectPath = FindRepositoryFile("src", "SGPdf.App", "SGPdf.App.csproj");
        var document = XDocument.Load(projectPath);
        var packages = document
            .Descendants("PackageReference")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        Assert.DoesNotContain(packages, name =>
            name!.Contains("System.Printing", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("ReachFramework", StringComparison.OrdinalIgnoreCase));
    }

    private static LabelLayoutPlan CreateThermalPlan(long totalQuantity, double widthMm, double heightMm)
    {
        if (totalQuantity > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(totalQuantity));

        var design = new ZplDesign(0, 0, "^XA^XZ", checked((int)totalQuantity));
        var document = new ZplDocument("labels.zpl", "synthetic", "synthetic", new[] { design });
        var sequence = new LabelOutputSequence(document, new ZplQuantitySelection(ZplQuantityMode.FromFile));
        return LabelLayoutPlanner.CreatePlan(
            sequence,
            new LabelLayoutSettings(LabelMediaKind.Thermal),
            widthMm,
            heightMm);
    }

    private static string FindRepositoryFile(params string[] relativeParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("No se encontró la raíz del repositorio desde el directorio de tests.");
    }
}
