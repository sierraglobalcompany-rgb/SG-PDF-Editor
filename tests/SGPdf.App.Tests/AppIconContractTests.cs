using System.Xml.Linq;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class AppIconContractTests
{
    private const string AppIconProjectPath = @"Assets\SGPdf.App.ico";
    private const string DocumentIconProjectPath = @"Assets\SGPdf.Document.ico";
    private const string DocumentPngProjectPath = @"Assets\SGPdf.Document.png";

    [Fact]
    public void AppAndDocumentIcons_AreWiredToDistinctRoles()
    {
        var projectPath = FindRepoFile("src", "SGPdf.App", "SGPdf.App.csproj");
        var project = XDocument.Load(projectPath);

        var applicationIcon = project
            .Descendants("ApplicationIcon")
            .SingleOrDefault();

        Assert.NotNull(applicationIcon);
        Assert.Equal(AppIconProjectPath, applicationIcon!.Value);

        AssertProjectResource(project, AppIconProjectPath);
        AssertProjectResource(project, DocumentIconProjectPath);
        AssertProjectResource(project, DocumentPngProjectPath);

        var appDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException("No se encontró la carpeta de SGPdf.App.");

        var appIconPath = Path.Combine(appDirectory, "Assets", "SGPdf.App.ico");
        var documentIconPath = Path.Combine(appDirectory, "Assets", "SGPdf.Document.ico");
        var documentPngPath = Path.Combine(appDirectory, "Assets", "SGPdf.Document.png");

        Assert.True(File.Exists(appIconPath), $"App icon not found: {appIconPath}");
        Assert.True(File.Exists(documentIconPath), $"Document icon not found: {documentIconPath}");
        Assert.True(File.Exists(documentPngPath), $"Document PNG not found: {documentPngPath}");

        Assert.NotEqual(
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(appIconPath))),
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(documentIconPath))));

        var mainWindowPath = FindRepoFile("src", "SGPdf.App", "MainWindow.xaml");
        var mainWindow = XDocument.Load(mainWindowPath).Root;

        Assert.NotNull(mainWindow);
        var windowIcon = NormalizePath((string?)mainWindow!.Attribute("Icon"));
        Assert.True(
            string.IsNullOrEmpty(windowIcon) ||
            string.Equals(windowIcon, NormalizePath(AppIconProjectPath), StringComparison.Ordinal),
            $"MainWindow must use the application icon role, not the document icon: {windowIcon}");
    }

    [Fact]
    public void AppAndDocumentIcons_AreMultiSizeWindowsIco()
    {
        var projectPath = FindRepoFile("src", "SGPdf.App", "SGPdf.App.csproj");
        var appDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException("No se encontró la carpeta de SGPdf.App.");

        AssertMultiSizeIcon(Path.Combine(appDirectory, "Assets", "SGPdf.App.ico"), "app");
        AssertMultiSizeIcon(Path.Combine(appDirectory, "Assets", "SGPdf.Document.ico"), "document");
    }

    private static void AssertProjectResource(XDocument project, string projectPath)
    {
        var resource = project
            .Descendants("Resource")
            .SingleOrDefault(element => string.Equals(
                NormalizePath((string?)element.Attribute("Include")),
                NormalizePath(projectPath),
                StringComparison.Ordinal));

        Assert.NotNull(resource);
    }

    private static void AssertMultiSizeIcon(string iconPath, string role)
    {
        Assert.True(File.Exists(iconPath), $"Official {role} icon not found: {iconPath}");

        using var stream = File.OpenRead(iconPath);
        using var reader = new BinaryReader(stream);

        Assert.Equal((ushort)0, reader.ReadUInt16());
        Assert.Equal((ushort)1, reader.ReadUInt16());

        var imageCount = reader.ReadUInt16();
        Assert.True(imageCount > 1, $"The {role} icon must contain multiple Windows icon sizes.");

        var sizes = new HashSet<int>();
        for (var index = 0; index < imageCount; index++)
        {
            var widthByte = reader.ReadByte();
            var heightByte = reader.ReadByte();
            reader.ReadByte();
            reader.ReadByte();
            reader.ReadUInt16();
            reader.ReadUInt16();
            reader.ReadUInt32();
            reader.ReadUInt32();

            var width = widthByte == 0 ? 256 : widthByte;
            var height = heightByte == 0 ? 256 : heightByte;
            Assert.Equal(width, height);
            sizes.Add(width);
        }

        Assert.Contains(16, sizes);
        Assert.Contains(32, sizes);
        Assert.Contains(48, sizes);
        Assert.Contains(256, sizes);
    }

    private static string NormalizePath(string? path) =>
        (path ?? string.Empty).Replace('\\', '/');

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
