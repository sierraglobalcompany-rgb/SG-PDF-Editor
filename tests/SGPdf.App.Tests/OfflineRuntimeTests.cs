using System.Reflection;
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
        var project = File.ReadAllText(projectPath);

        Assert.Contains("bblanchon.PDFium.Win32", project, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net.Http", project, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RestSharp", project, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RuntimeSource_HasNoNetworkClientOrHttpEndpoint()
    {
        var sourceRoot = Path.GetDirectoryName(FindRepositoryFile("src", "SGPdf.App", "SGPdf.App.csproj"))!;
        var forbiddenTokens = new[]
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

        foreach (var path in Directory.EnumerateFiles(sourceRoot, "*.*", SearchOption.AllDirectories)
                     .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                                    path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var source = File.ReadAllText(path);
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
