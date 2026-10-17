using System.Buffers.Binary;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class HomeToolIconContractTests
{
    private static readonly string[] ExpectedToolIds =
    [
        "merge-pdf",
        "split-pdf",
        "organize-pages",
        "edit-pdf",
        "ocr",
        "redact",
        "protect",
        "sign",
        "compare-pdf",
        "pdf-to-png",
        "image-to-pdf",
        "qr",
        "labels-zpl",
        "watermark",
        "page-numbering"
    ];

    [Fact]
    public void OfficialHomeToolIconAssets_AreComplete256PngSet()
    {
        var projectPath = FindRepoFile("src", "SGPdf.App", "SGPdf.App.csproj");
        var appDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException("No se encontró la carpeta de SGPdf.App.");
        var iconDirectory = Path.Combine(appDirectory, "Assets", "HomeTools");

        Assert.True(Directory.Exists(iconDirectory), $"Home tool icon directory not found: {iconDirectory}");

        var actualIds = Directory
            .EnumerateFiles(iconDirectory, "*.png", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var expectedIds = ExpectedToolIds.Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(expectedIds, actualIds);

        foreach (var toolId in ExpectedToolIds)
        {
            AssertPng256(Path.Combine(iconDirectory, $"{toolId}.png"));
        }
    }

    [Fact]
    public void HomeDashboard_UsesOfficialToolAndDocumentArtwork()
    {
        var dashboardPath = FindRepoFile("src", "SGPdf.App", "Shell", "HomeDashboard.xaml");
        var dashboard = File.ReadAllText(dashboardPath);

        Assert.Contains("Source=\"{Binding HomeIconPath}\"", dashboard, StringComparison.Ordinal);
        Assert.Contains("Assets/SGPdf.Document.png", dashboard, StringComparison.Ordinal);

        var descriptorPath = FindRepoFile("src", "SGPdf.App", "Shell", "ToolDescriptor.cs");
        var descriptor = File.ReadAllText(descriptorPath);
        Assert.Contains("HomeIconPath", descriptor, StringComparison.Ordinal);
        Assert.Contains("Assets/HomeTools/{Id}.png", descriptor, StringComparison.Ordinal);
    }

    private static void AssertPng256(string path)
    {
        Assert.True(File.Exists(path), $"Home tool icon not found: {path}");

        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length >= 24, $"PNG too short: {path}");

        ReadOnlySpan<byte> pngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
        Assert.True(bytes.AsSpan(0, 8).SequenceEqual(pngSignature), $"Invalid PNG signature: {path}");

        var width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));

        Assert.Equal(256, width);
        Assert.Equal(256, height);
    }

    private static string FindRepoFile(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = segments.Aggregate(directory.FullName, Path.Combine);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Repository file not found: {Path.Combine(segments)}");
    }
}
