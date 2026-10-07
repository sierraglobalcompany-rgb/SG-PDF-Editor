using System.Text;
using SGPdf.App.Features.Labels;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ZplFileLoaderTests
{
    [Theory]
    [InlineData(".zpl")]
    [InlineData(".TXT")]
    [InlineData(".PrN")]
    public void Load_SupportedExtensions_AreAcceptedCaseInsensitively(string extension)
    {
        var path = NewTempPath(extension);
        try
        {
            File.WriteAllText(path, "^XA^FO10,10^FDOK^FS^XZ", new UTF8Encoding(false));

            var document = ZplFileLoader.Load(path);

            Assert.Equal(Path.GetFullPath(path), document.SourcePath);
            Assert.Single(document.Designs);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void Load_Utf8SpanishText_PreservesOriginalSource()
    {
        var path = NewTempPath(".zpl");
        const string source = "^XA^CI28^FO10,10^FDMedellín Ñandú^FS^XZ";
        try
        {
            File.WriteAllText(path, source, new UTF8Encoding(false));

            var document = ZplFileLoader.Load(path);

            Assert.Equal(source, document.OriginalSource);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void Load_Utf8Bom_IsAcceptedWithoutBecomingSourceText()
    {
        var path = NewTempPath(".zpl");
        const string source = "^XA^FO10,10^FDBOM OK^FS^XZ";
        try
        {
            var payload = Encoding.UTF8.GetBytes(source);
            File.WriteAllBytes(path, [0xEF, 0xBB, 0xBF, .. payload]);

            var document = ZplFileLoader.Load(path);

            Assert.Equal(source, document.OriginalSource);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".bin")]
    public void Load_UnsupportedExtension_ThrowsNotSupportedException(string extension)
    {
        var path = NewTempPath(extension);
        try
        {
            File.WriteAllText(path, "^XA^XZ", new UTF8Encoding(false));

            Assert.Throws<NotSupportedException>(() => ZplFileLoader.Load(path));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void Load_NonexistentPath_ThrowsFileNotFoundException()
    {
        var path = NewTempPath(".zpl");

        Assert.Throws<FileNotFoundException>(() => ZplFileLoader.Load(path));
    }

    [Fact]
    public void Load_InvalidUtf8_ThrowsDecoderFallbackException()
    {
        var path = NewTempPath(".zpl");
        try
        {
            File.WriteAllBytes(path, [0x5E, 0x58, 0x41, 0xC3, 0x28, 0x5E, 0x58, 0x5A]);

            Assert.Throws<DecoderFallbackException>(() => ZplFileLoader.Load(path));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private static string NewTempPath(string extension)
        => Path.Combine(Path.GetTempPath(), $"sgpdf-zpl-{Guid.NewGuid():N}{extension}");

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
