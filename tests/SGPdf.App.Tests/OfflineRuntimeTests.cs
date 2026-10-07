using System.Reflection;
using System.Xml.Linq;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OfflineRuntimeTests
{
    private static readonly string[] ForbiddenAssemblyPrefixes =
    {
        "System.Net.Http",
        "System.Net.Requests",
        "System.Net.Sockets",
        "System.Net.WebClient"
    };

    [Fact]
    public void AppAssembly_HasNoDirectNetworkAssemblyDependency()
    {
        var references = typeof(PdfDocumentSession).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        foreach (var forbidden in ForbiddenAssemblyPrefixes)
            Assert.DoesNotContain(references, name => name.StartsWith(forbidden, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RuntimeProject_HasOnlyApprovedPackageDependency()
    {
        var projectPath = FindRepositoryFile("src", "SGPdf.App", "SGPdf.App.csproj");
        var document = XDocument.Load(projectPath);
        var packages = document
            .Descendants("PackageReference")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(new[] { "bblanchon.PDFium.Win32" }, packages);
    }

    [Fact]
    public void RuntimeSource_HasNoNetworkClientOrRemoteNavigation()
    {
        var sourceRoot = Path.GetDirectoryName(FindRepositoryFile("src", "SGPdf.App", "SGPdf.App.csproj"))!;
        var forbiddenCodeTokens = new[]
        {
            "HttpClient",
            "WebClient",
            "HttpWebRequest",
            "TcpClient",
            "UdpClient",
            "System.Net.Sockets",
            "http://",
            "https://"
        };
        var forbiddenXamlTokens = new[]
        {
            "<WebBrowser",
            "NavigateUri=\"http://",
            "NavigateUri=\"https://",
            "Source=\"http://",
            "Source=\"https://"
        };

        foreach (var path in Directory.EnumerateFiles(sourceRoot, "*.*", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var source = File.ReadAllText(path);
            var forbiddenTokens = path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                ? forbiddenCodeTokens
                : path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
                    ? forbiddenXamlTokens
                    : Array.Empty<string>();

            foreach (var forbidden in forbiddenTokens)
                Assert.DoesNotContain(forbidden, source, StringComparison.OrdinalIgnoreCase);
        }
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
