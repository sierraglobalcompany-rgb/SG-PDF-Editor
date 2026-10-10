using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfTextObjectDiscoveryTests
{
    [Fact]
    public void GetTextObjects_PreservesExactUnicodeAndManagedSnapshot()
    {
        using var fixture = TextObjectDiscoveryFixtureFactory.CreateUnicodeRotatedText();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var item = Assert.Single(GetTextObjects(session, 0));

        Assert.Equal("NIÑO áé", Property<string>(item, "Text"));
        var key = Property<object>(item, "Key");
        Assert.Equal(0, Convert.ToInt32(Property<object>(key, "PageIndex")));
        Assert.Equal(0, Convert.ToInt32(Property<object>(key, "PageObjectIndex")));
        Assert.Equal("Helvetica", Property<string>(item, "FontName"));
        Assert.Equal(18d, Convert.ToDouble(Property<object>(item, "FontSize")), 3);
        Assert.Equal(0, Convert.ToInt32(Property<object>(item, "TextRenderMode")));

        var fill = Property<object>(item, "FillColor");
        Assert.Equal(255u, Convert.ToUInt32(Property<object>(fill, "Red")));
        Assert.Equal(0u, Convert.ToUInt32(Property<object>(fill, "Green")));
        Assert.Equal(0u, Convert.ToUInt32(Property<object>(fill, "Blue")));
        Assert.Equal(255u, Convert.ToUInt32(Property<object>(fill, "Alpha")));

        AssertFiniteMembers(Property<object>(item, "Matrix"), "A", "B", "C", "D", "E", "F");
        AssertFiniteMembers(Property<object>(item, "Bounds"), "Left", "Bottom", "Right", "Top");
        AssertFiniteMembers(Property<object>(item, "Quad"), "X1", "Y1", "X2", "Y2", "X3", "Y3", "X4", "Y4");
    }

    [Fact]
    public void GetTextObjects_KeepsContiguousTopLevelObjectsSeparate()
    {
        using var fixture = TextObjectDiscoveryFixtureFactory.CreateContiguousTextObjects();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var items = GetTextObjects(session, 0);

        Assert.Equal(new[] { "UNO", "DOS" }, items.Select(item => Property<string>(item, "Text")));
        Assert.Equal(new[] { 0, 1 }, items.Select(item =>
            Convert.ToInt32(Property<object>(Property<object>(item, "Key"), "PageObjectIndex"))));
    }

    [Fact]
    public void GetTextObjects_SkipsTextNestedInsideFormXObject()
    {
        using var fixture = TextObjectDiscoveryFixtureFactory.CreateTopLevelTextPlusFormText();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var item = Assert.Single(GetTextObjects(session, 0));

        Assert.Equal("TOP", Property<string>(item, "Text"));
    }

    [Fact]
    public void GetTextObjects_PreservesUnsupportedRenderModeForLaterReadOnlyPolicy()
    {
        using var fixture = TextObjectDiscoveryFixtureFactory.CreateStrokeText();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var item = Assert.Single(GetTextObjects(session, 0));

        Assert.Equal("STROKE", Property<string>(item, "Text"));
        Assert.Equal(1, Convert.ToInt32(Property<object>(item, "TextRenderMode")));
    }

    [Fact]
    public void GetTextObjects_ReadsOnlyRequestedPage()
    {
        using var fixture = TextObjectDiscoveryFixtureFactory.CreateTwoPages();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var firstPage = Assert.Single(GetTextObjects(session, 0));
        var secondPage = Assert.Single(GetTextObjects(session, 1));

        Assert.Equal("PAGE0", Property<string>(firstPage, "Text"));
        Assert.Equal("PAGE1", Property<string>(secondPage, "Text"));
        Assert.Equal(0, Convert.ToInt32(Property<object>(Property<object>(firstPage, "Key"), "PageIndex")));
        Assert.Equal(1, Convert.ToInt32(Property<object>(Property<object>(secondPage, "Key"), "PageIndex")));
    }

    [Fact]
    public void GetTextObjects_HonorsCancellation()
    {
        using var fixture = TextObjectDiscoveryFixtureFactory.CreateContiguousTextObjects();
        using var session = PdfDocumentSession.Open(fixture.Path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => GetTextObjects(session, 0, cancellation.Token));
    }

    private static IReadOnlyList<object> GetTextObjects(
        PdfDocumentSession session,
        int pageIndex,
        CancellationToken cancellationToken = default)
    {
        var method = typeof(PdfDocumentSession).GetMethod(
            "GetTextObjects",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(int), typeof(CancellationToken) },
            modifiers: null);
        Assert.NotNull(method);

        try
        {
            var result = method!.Invoke(session, new object[] { pageIndex, cancellationToken });
            var enumerable = Assert.IsAssignableFrom<IEnumerable>(result);
            return enumerable.Cast<object>().ToArray();
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static T Property<T>(object instance, string name)
    {
        var property = instance.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsAssignableFrom<T>(property!.GetValue(instance));
    }

    private static void AssertFiniteMembers(object value, params string[] names)
    {
        foreach (var name in names)
            Assert.True(double.IsFinite(Convert.ToDouble(Property<object>(value, name))), $"{name} must be finite.");
    }
}

internal sealed class TextObjectDiscoveryFixture : IDisposable
{
    internal TextObjectDiscoveryFixture(string directoryPath, string path)
    {
        DirectoryPath = directoryPath;
        Path = path;
    }

    internal string DirectoryPath { get; }
    internal string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }
}

internal static class TextObjectDiscoveryFixtureFactory
{
    internal static TextObjectDiscoveryFixture CreateUnicodeRotatedText()
    {
        const string content = "BT\n/F1 18 Tf\n1 0 0 rg\n0 1 -1 0 160 220 Tm\n(NI\\321O \\341\\351) Tj\nET\n";
        return Write(
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            HelveticaFont,
            Stream(content));
    }

    internal static TextObjectDiscoveryFixture CreateContiguousTextObjects()
    {
        const string content =
            "BT\n/F1 18 Tf\n0 0 0 rg\n1 0 0 1 72 300 Tm\n(UNO) Tj\nET\n" +
            "BT\n/F1 18 Tf\n0 0 0 rg\n1 0 0 1 72 260 Tm\n(DOS) Tj\nET\n";
        return Write(
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            HelveticaFont,
            Stream(content));
    }

    internal static TextObjectDiscoveryFixture CreateTopLevelTextPlusFormText()
    {
        const string pageContent =
            "BT\n/F1 18 Tf\n0 0 0 rg\n1 0 0 1 72 300 Tm\n(TOP) Tj\nET\n" +
            "q\n1 0 0 1 40 100 cm\n/Fm1 Do\nQ\n";
        const string formContent =
            "BT\n/F1 18 Tf\n0 0 0 rg\n1 0 0 1 10 40 Tm\n(FORM) Tj\nET\n";
        return Write(
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Resources << /Font << /F1 4 0 R >> /XObject << /Fm1 6 0 R >> >> /Contents 5 0 R >>",
            HelveticaFont,
            Stream(pageContent),
            Stream(formContent, "/Type /XObject /Subtype /Form /BBox [0 0 100 100] /Resources << /Font << /F1 4 0 R >> >>"));
    }

    internal static TextObjectDiscoveryFixture CreateStrokeText()
    {
        const string content = "BT\n/F1 18 Tf\n1 Tr\n0 0 0 rg\n1 0 0 1 72 300 Tm\n(STROKE) Tj\nET\n";
        return Write(
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            HelveticaFont,
            Stream(content));
    }

    internal static TextObjectDiscoveryFixture CreateTwoPages()
    {
        const string page0 = "BT\n/F1 18 Tf\n0 0 0 rg\n1 0 0 1 72 300 Tm\n(PAGE0) Tj\nET\n";
        const string page1 = "BT\n/F1 18 Tf\n0 0 0 rg\n1 0 0 1 72 300 Tm\n(PAGE1) Tj\nET\n";
        return Write(
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 6 0 R] /Count 2 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            HelveticaFont,
            Stream(page0),
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Resources << /Font << /F1 4 0 R >> >> /Contents 7 0 R >>",
            Stream(page1));
    }

    private const string HelveticaFont =
        "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>";

    private static string Stream(string content, string dictionaryPrefix = "")
    {
        var prefix = string.IsNullOrWhiteSpace(dictionaryPrefix) ? string.Empty : dictionaryPrefix + " ";
        return $"<< {prefix}/Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream";
    }

    private static TextObjectDiscoveryFixture Write(params string[] objects)
    {
        var directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"sgpdf-f7-text-discovery-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = System.IO.Path.Combine(directory, "source.pdf");

        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true)
        {
            NewLine = "\n"
        };
        writer.Write("%PDF-1.4\n");
        writer.Flush();

        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(stream.Position);
            writer.Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
            writer.Flush();
        }

        var xrefOffset = stream.Position;
        writer.Write($"xref\n0 {objects.Length + 1}\n");
        writer.Write("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            writer.Write($"{offset:0000000000} 00000 n \n");
        writer.Write($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        writer.Flush();

        return new TextObjectDiscoveryFixture(directory, path);
    }
}
