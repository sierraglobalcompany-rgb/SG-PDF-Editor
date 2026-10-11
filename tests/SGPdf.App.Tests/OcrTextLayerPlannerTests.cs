using System.Collections;
using System.Reflection;
using SGPdf.App.Features.Ocr;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OcrTextLayerPlannerTests
{
    [Fact]
    public void Build_GroupsSameTsvLineWithSingleSpaces_AndKeepsPerWordGeometry()
    {
        var rendered = CreateRenderedPage(
            pageIndex: 4,
            originX: 0d,
            originY: 100d,
            xAxisX: 1d,
            xAxisY: 0d,
            yAxisX: 0d,
            yAxisY: -1d);
        var words = CreateWords(
            Word(1, 1, 1, 1, 1, 95d, "Factura", 10, 10, 20, 10),
            Word(1, 1, 1, 1, 2, 90d, "número", 35, 10, 20, 10),
            Word(1, 1, 1, 1, 3, 88d, "123", 60, 10, 15, 10));

        var placements = InvokeBuild(words, rendered);

        var placement = Assert.Single(placements);
        Assert.Equal(4, Get<int>(placement, "PageIndex"));
        Assert.Equal("Factura número 123", Get<string>(placement, "Text"));

        var placedWords = GetEnumerable(placement, "Words");
        Assert.Equal(3, placedWords.Length);
        Assert.Equal(new[] { "Factura", "número", "123" }, placedWords.Select(word => Get<string>(word, "Text")));
        Assert.All(placedWords, word => AssertValidRect(Get<object>(word, "Bounds"), 0d, 100d, 0d, 100d));
    }

    [Fact]
    public void Build_PreservesFirstAppearanceOrderAcrossDistinctLines()
    {
        var rendered = CreateRenderedPage(
            pageIndex: 0,
            originX: 0d,
            originY: 100d,
            xAxisX: 1d,
            xAxisY: 0d,
            yAxisX: 0d,
            yAxisY: -1d);
        var words = CreateWords(
            Word(1, 2, 4, 8, 1, 91d, "Primera", 10, 10, 20, 10),
            Word(1, 2, 4, 8, 2, 92d, "línea", 35, 10, 15, 10),
            Word(1, 1, 1, 3, 1, 93d, "Segunda", 10, 30, 20, 10));

        var placements = InvokeBuild(words, rendered);

        Assert.Equal(new[] { "Primera línea", "Segunda" }, placements.Select(item => Get<string>(item, "Text")));
        Assert.Equal(new[] { 0, 1 }, placements.Select(item => Get<int>(item, "SourceOrder")));
    }

    [Fact]
    public void Build_PreservesUnicodeAndConfidenceZeroWords()
    {
        var rendered = CreateRenderedPage(
            pageIndex: 1,
            originX: 0d,
            originY: 100d,
            xAxisX: 1d,
            xAxisY: 0d,
            yAxisX: 0d,
            yAxisY: -1d);
        var words = CreateWords(
            Word(1, 1, 1, 1, 1, 0d, "NIÑO", 10, 20, 20, 10),
            Word(1, 1, 1, 1, 2, 87.5d, "Bogotá", 35, 20, 20, 10));

        var placement = Assert.Single(InvokeBuild(words, rendered));

        Assert.Equal("NIÑO Bogotá", Get<string>(placement, "Text"));
        var placedWords = GetEnumerable(placement, "Words");
        Assert.Equal(2, placedWords.Length);
        Assert.Equal(0d, Get<double>(placedWords[0], "Confidence"));
        Assert.Equal("NIÑO", Get<string>(placedWords[0], "Text"));
    }

    [Fact]
    public void Build_RotatedMappedBoundsStayInsidePdfPageGeometry()
    {
        var rendered = CreateRenderedPage(
            pageIndex: 2,
            originX: 0d,
            originY: 0d,
            xAxisX: 0d,
            xAxisY: 1d,
            yAxisX: 1d,
            yAxisY: 0d);
        var words = CreateWords(
            Word(1, 1, 1, 1, 1, 80d, "Rotada", 10, 20, 30, 15),
            Word(1, 1, 1, 1, 2, 75d, "bien", 45, 20, 20, 15));

        var placement = Assert.Single(InvokeBuild(words, rendered));

        AssertValidRect(Get<object>(placement, "Bounds"), 0d, 100d, 0d, 100d);
        Assert.All(
            GetEnumerable(placement, "Words"),
            word => AssertValidRect(Get<object>(word, "Bounds"), 0d, 100d, 0d, 100d));
    }

    private static object[] InvokeBuild(object words, PdfRenderedPage renderedPage)
    {
        var plannerType = typeof(OcrRequest).Assembly.GetType("SGPdf.App.Features.Ocr.OcrTextLayerPlanner");
        Assert.NotNull(plannerType);

        var method = plannerType!.GetMethod("Build", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method!.Invoke(null, new[] { words, renderedPage });
        Assert.NotNull(result);
        return Assert.IsAssignableFrom<IEnumerable>(result).Cast<object>().ToArray();
    }

    private static object CreateWords(params WordSpec[] specs)
    {
        var assembly = typeof(OcrRequest).Assembly;
        var wordType = assembly.GetType("SGPdf.App.Features.Ocr.OcrWord");
        var pixelRectType = assembly.GetType("SGPdf.App.Features.Ocr.OcrPixelRect");
        Assert.NotNull(wordType);
        Assert.NotNull(pixelRectType);

        var listType = typeof(List<>).MakeGenericType(wordType!);
        var list = Assert.IsAssignableFrom<IList>(Activator.CreateInstance(listType));

        foreach (var spec in specs)
        {
            var bounds = Activator.CreateInstance(
                pixelRectType!,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { spec.Left, spec.Top, spec.Width, spec.Height },
                culture: null);
            Assert.NotNull(bounds);

            var word = Activator.CreateInstance(
                wordType!,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[]
                {
                    spec.Page,
                    spec.Block,
                    spec.Paragraph,
                    spec.Line,
                    spec.Word,
                    spec.Confidence,
                    spec.Text,
                    bounds!
                },
                culture: null);
            Assert.NotNull(word);
            list.Add(word);
        }

        return list;
    }

    private static PdfRenderedPage CreateRenderedPage(
        int pageIndex,
        double originX,
        double originY,
        double xAxisX,
        double xAxisY,
        double yAxisX,
        double yAxisY)
    {
        const int deviceWidth = 100;
        const int deviceHeight = 100;
        var rendered = new PdfRenderedPage(
            pageIndex,
            deviceWidth,
            deviceHeight,
            deviceWidth * 4,
            300d,
            new byte[deviceWidth * deviceHeight * 4]);

        var transformProperty = typeof(PdfRenderedPage).GetProperty(
            "DeviceTransform",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(transformProperty);

        var transformType = transformProperty!.PropertyType.GetGenericArguments().Single();
        var transform = Activator.CreateInstance(
            transformType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[]
            {
                originX,
                originY,
                xAxisX,
                xAxisY,
                yAxisX,
                yAxisY,
                deviceWidth,
                deviceHeight
            },
            culture: null);
        Assert.NotNull(transform);
        transformProperty.SetValue(rendered, transform);
        return rendered;
    }

    private static object[] GetEnumerable(object target, string propertyName)
    {
        var value = Get<object>(target, propertyName);
        return Assert.IsAssignableFrom<IEnumerable>(value).Cast<object>().ToArray();
    }

    private static T Get<T>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        var value = property!.GetValue(target);
        Assert.NotNull(value);
        return Assert.IsType<T>(value);
    }

    private static void AssertValidRect(
        object rect,
        double minX,
        double maxX,
        double minY,
        double maxY)
    {
        var left = Get<double>(rect, "Left");
        var bottom = Get<double>(rect, "Bottom");
        var right = Get<double>(rect, "Right");
        var top = Get<double>(rect, "Top");

        Assert.InRange(left, minX, maxX);
        Assert.InRange(right, minX, maxX);
        Assert.InRange(bottom, minY, maxY);
        Assert.InRange(top, minY, maxY);
        Assert.True(right > left);
        Assert.True(top > bottom);
    }

    private static WordSpec Word(
        int page,
        int block,
        int paragraph,
        int line,
        int word,
        double confidence,
        string text,
        int left,
        int top,
        int width,
        int height) =>
        new(page, block, paragraph, line, word, confidence, text, left, top, width, height);

    private sealed record WordSpec(
        int Page,
        int Block,
        int Paragraph,
        int Line,
        int Word,
        double Confidence,
        string Text,
        int Left,
        int Top,
        int Width,
        int Height);
}
