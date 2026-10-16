using System.Xml.Linq;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class AppIconContractTests
{
    private const string IconProjectPath = @"Assets\SGPdf.ico";

    [Fact]
    public void OfficialAppIcon_IsWiredToExecutableAndWpfResources()
    {
        var projectPath = FindRepoFile("src", "SGPdf.App", "SGPdf.App.csproj");
        var project = XDocument.Load(projectPath);

        var applicationIcon = project
            .Descendants("ApplicationIcon")
            .SingleOrDefault();

        Assert.NotNull(applicationIcon);
        Assert.Equal(IconProjectPath, applicationIcon!.Value);

        var iconResource = project
            .Descendants("Resource")
            .SingleOrDefault(element => string.Equals(
                NormalizePath((string?)element.Attribute("Include")),
                NormalizePath(IconProjectPath),
                StringComparison.Ordinal));

        Assert.NotNull(iconResource);

        var mainWindowPath = FindRepoFile("src", "SGPdf.App", "MainWindow.xaml");
        var mainWindow = XDocument.Load(mainWindowPath).Root;

        Assert.NotNull(mainWindow);
        var windowIcon = NormalizePath((string?)mainWindow!.Attribute("Icon"));
        Assert.True(
            string.IsNullOrEmpty(windowIcon) ||
            string.Equals(windowIcon, NormalizePath(IconProjectPath), StringComparison.Ordinal),
            $"MainWindow must not override the executable icon with a different asset: {windowIcon}");
    }

    [Fact]
    public void OfficialAppIcon_IsMultiSizeWindowsIco()
    {
        var projectPath = FindRepoFile("src", "SGPdf.App", "SGPdf.App.csproj");
        var appDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException("No se encontró la carpeta de SGPdf.App.");
        var iconPath = Path.Combine(appDirectory, "Assets", "SGPdf.ico");

        Assert.True(File.Exists(iconPath), $"Official app icon not found: {iconPath}");

        using var stream = File.OpenRead(iconPath);
        using var reader = new BinaryReader(stream);

        Assert.Equal((ushort)0, reader.ReadUInt16());
        Assert.Equal((ushort)1, reader.ReadUInt16());

        var imageCount = reader.ReadUInt16();
        Assert.True(imageCount > 1, "The app icon must contain multiple Windows icon sizes.");

        var sizes = new HashSet<int>();
        for (var index = 0; index < imageCount; index++)
        {
            var widthByte = reader.ReadByte();
            var heightByte = reader.ReadByte();
            reader.ReadByte(); // color count
            reader.ReadByte(); // reserved
            reader.ReadUInt16(); // planes
            reader.ReadUInt16(); // bit count
            reader.ReadUInt32(); // bytes in resource
            reader.ReadUInt32(); // image offset

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
