using SGPdf.App.Features.Labels;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class LabelizeRuntimeTests
{
    [Fact]
    public void ResolveBundledExecutablePath_ReturnsPinnedSubdirectoryExecutable()
    {
        var root = CreateTempDirectory();
        try
        {
            var expected = Path.Combine(root, "labelize", "labelize.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(expected)!);
            File.WriteAllText(expected, "fake");

            var actual = LabelizeRuntime.ResolveBundledExecutablePath(root);

            Assert.Equal(Path.GetFullPath(expected), actual);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolveBundledExecutablePath_WhenMissing_ThrowsControlledFileNotFound()
    {
        var root = CreateTempDirectory();
        try
        {
            var expected = Path.Combine(root, "labelize", "labelize.exe");

            var error = Assert.Throws<FileNotFoundException>(
                () => LabelizeRuntime.ResolveBundledExecutablePath(root));

            Assert.Equal(Path.GetFullPath(expected), error.FileName);
            Assert.Contains("Labelize 1.7.0", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "sgpdf-labelize-runtime-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
