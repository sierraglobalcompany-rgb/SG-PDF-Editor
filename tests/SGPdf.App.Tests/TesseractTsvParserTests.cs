using System.Collections;
using System.Reflection;
using System.Text;
using SGPdf.App.Features.Ocr;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class TesseractTsvParserTests
{
    private const string Header = "level\tpage_num\tblock_num\tpar_num\tline_num\tword_num\tleft\ttop\twidth\theight\tconf\ttext";

    [Fact]
    public void ParseText_ReturnsOnlyLevel5WordsAndPreservesUnicodeGeometry()
    {
        var tsv = string.Join("\n",
            Header,
            "1\t1\t0\t0\t0\t0\t0\t0\t100\t50\t-1\t",
            "4\t1\t1\t1\t1\t0\t10\t20\t80\t12\t-1\t",
            "5\t1\t1\t1\t1\t1\t10\t20\t30\t12\t96.5\tNIÑO");

        var words = ParseText(tsv);

        var word = Assert.Single(words);
        Assert.Equal(1, Get<int>(word, "Page"));
        Assert.Equal(1, Get<int>(word, "Block"));
        Assert.Equal(1, Get<int>(word, "Paragraph"));
        Assert.Equal(1, Get<int>(word, "Line"));
        Assert.Equal(1, Get<int>(word, "Word"));
        Assert.Equal(96.5d, Get<double>(word, "Confidence"), 6);
        Assert.Equal("NIÑO", Get<string>(word, "Text"));

        var bounds = GetValue(word, "Bounds");
        Assert.Equal(10, Get<int>(bounds, "Left"));
        Assert.Equal(20, Get<int>(bounds, "Top"));
        Assert.Equal(30, Get<int>(bounds, "Width"));
        Assert.Equal(12, Get<int>(bounds, "Height"));
    }

    [Fact]
    public void ParseText_RejectsNonStandardHeader()
    {
        var error = Assert.Throws<TargetInvocationException>(() => ParseText(
            "level\tpage\tblock\tpar\tline\tword\tleft\ttop\twidth\theight\tconf\ttext\n" +
            "5\t1\t1\t1\t1\t1\t10\t20\t30\t12\t90\ttexto"));

        Assert.IsType<InvalidDataException>(error.InnerException);
    }

    [Theory]
    [InlineData("5\t1\t1\t1\t1\t1\t-1\t20\t30\t12\t90\ttexto")]
    [InlineData("5\t1\t1\t1\t1\t1\t10\t20\t0\t12\t90\ttexto")]
    [InlineData("5\t1\t1\t1\t1\t1\t10\t20\t30\t-2\t90\ttexto")]
    public void ParseText_RejectsInvalidLevel5Geometry(string row)
    {
        var error = Assert.Throws<TargetInvocationException>(() => ParseText($"{Header}\n{row}"));
        Assert.IsType<InvalidDataException>(error.InnerException);
    }

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    public void ParseText_RejectsNonFiniteConfidence(string confidence)
    {
        var row = $"5\t1\t1\t1\t1\t1\t10\t20\t30\t12\t{confidence}\ttexto";
        var error = Assert.Throws<TargetInvocationException>(() => ParseText($"{Header}\n{row}"));
        Assert.IsType<InvalidDataException>(error.InnerException);
    }

    [Fact]
    public void ParseText_RejectsEmptyLevel5Text()
    {
        var row = "5\t1\t1\t1\t1\t1\t10\t20\t30\t12\t90\t";
        var error = Assert.Throws<TargetInvocationException>(() => ParseText($"{Header}\n{row}"));
        Assert.IsType<InvalidDataException>(error.InnerException);
    }

    [Fact]
    public void ParseFile_RejectsInvalidUtf8()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-ocr-tsv-{Guid.NewGuid():N}.tsv");
        try
        {
            var prefix = Encoding.ASCII.GetBytes(Header + "\n5\t1\t1\t1\t1\t1\t10\t20\t30\t12\t90\t");
            File.WriteAllBytes(path, prefix.Concat(new byte[] { 0xC3, 0x28 }).ToArray());

            var error = Assert.Throws<TargetInvocationException>(() => ParseFile(path));
            Assert.IsType<DecoderFallbackException>(error.InnerException);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static IReadOnlyList<object> ParseText(string tsv)
    {
        var parserType = GetParserType();
        var method = parserType.GetMethod(
            "ParseText",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);
        Assert.NotNull(method);

        var result = method!.Invoke(null, new object[] { tsv });
        return Assert.IsAssignableFrom<IEnumerable>(result).Cast<object>().ToArray();
    }

    private static void ParseFile(string path)
    {
        var parserType = GetParserType();
        var method = parserType.GetMethod(
            "ParseFile",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);
        Assert.NotNull(method);
        _ = method!.Invoke(null, new object[] { path });
    }

    private static Type GetParserType()
    {
        var type = typeof(OcrRequest).Assembly.GetType("SGPdf.App.Features.Ocr.TesseractTsvParser");
        Assert.NotNull(type);
        return type!;
    }

    private static object GetValue(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        var value = property!.GetValue(target);
        Assert.NotNull(value);
        return value!;
    }

    private static T Get<T>(object target, string propertyName)
    {
        return Assert.IsType<T>(GetValue(target, propertyName));
    }
}
