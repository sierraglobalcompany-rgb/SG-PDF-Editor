using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Labels;
using SGPdf.App.Printing;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class LabelPrintPaginatorTests
{
    [Fact]
    public void Constructor_RejectsMultiUpPlan()
    {
        var document = Document((0, 2));
        var plan = Plan(document, LabelMediaKind.A4, 50, 30, labelsPerPage: 2);
        var rendered = new[] { Rendered(0, 50, 30, SolidPng(Colors.Black)) };

        Assert.Throws<ArgumentException>(() => new LabelPrintPaginator(plan, rendered));
    }

    [Fact]
    public void Constructor_RejectsRenderedDesignCountMismatch()
    {
        var document = Document((0, 1), (1, 1));
        var plan = Plan(document, LabelMediaKind.Thermal, 50, 30);
        var rendered = new[] { Rendered(0, 50, 30, SolidPng(Colors.Black)) };

        Assert.Throws<ArgumentException>(() => new LabelPrintPaginator(plan, rendered));
    }

    [Fact]
    public void PageSize_UsesExactPlanMillimetersWithoutPrintableAreaFit()
    {
        RunInSta(() =>
        {
            var document = Document((0, 1));
            var plan = Plan(document, LabelMediaKind.Thermal, 102, 152);
            var paginator = new LabelPrintPaginator(
                plan,
                new[] { Rendered(0, 102, 152, SolidPng(Colors.Black)) });

            Assert.Equal(102d * 96d / 25.4d, paginator.PageSize.Width, 6);
            Assert.Equal(152d * 96d / 25.4d, paginator.PageSize.Height, 6);
            Assert.Equal(paginator.PageSize, paginator.GetPage(0).Size);
        });
    }

    [Fact]
    public void RepeatedQuantities_ResolveCorrectRenderedDesignPerPage()
    {
        RunInSta(() =>
        {
            var document = Document((0, 2), (1, 1));
            var plan = Plan(document, LabelMediaKind.Thermal, 20, 20);
            var paginator = new LabelPrintPaginator(
                plan,
                new[]
                {
                    Rendered(0, 20, 20, SolidPng(Colors.Red)),
                    Rendered(1, 20, 20, SolidPng(Colors.Blue))
                });

            Assert.Equal(3, paginator.PageCount);
            AssertColorClose(Colors.Red, RenderPixel(paginator.GetPage(0), 0.5, 0.5));
            AssertColorClose(Colors.Red, RenderPixel(paginator.GetPage(1), 0.5, 0.5));
            AssertColorClose(Colors.Blue, RenderPixel(paginator.GetPage(2), 0.5, 0.5));
        });
    }

    [Fact]
    public void RotatedThermalPlan_UsesAlreadyRotatedF2_4PageDimensionsExactlyOnce()
    {
        RunInSta(() =>
        {
            var document = Document((0, 1));
            var sequence = new LabelOutputSequence(document, new ZplQuantitySelection(ZplQuantityMode.FromFile));
            var plan = LabelLayoutPlanner.CreatePlan(
                sequence,
                new LabelLayoutSettings(LabelMediaKind.Thermal, rotation: LabelRotation.Degrees90),
                labelWidthMm: 20,
                labelHeightMm: 10);
            var paginator = new LabelPrintPaginator(
                plan,
                new[] { Rendered(0, 20, 10, HorizontalRedBluePng()) });

            Assert.Equal(10d * 96d / 25.4d, paginator.PageSize.Width, 6);
            Assert.Equal(20d * 96d / 25.4d, paginator.PageSize.Height, 6);

            var page = paginator.GetPage(0);
            var top = RenderPixel(page, 0.5, 0.25);
            var bottom = RenderPixel(page, 0.5, 0.75);
            Assert.NotEqual((top.R, top.G, top.B), (bottom.R, bottom.G, bottom.B));
        });
    }

    [Fact]
    public void PageCount_AllowsInt32MaxValue_AndRejectsOneAboveWithoutMaterializingPages()
    {
        RunInSta(() =>
        {
            var maxDocument = Document((0, int.MaxValue));
            var maxPlan = Plan(maxDocument, LabelMediaKind.Thermal, 20, 20);
            var maxPaginator = new LabelPrintPaginator(
                maxPlan,
                new[] { Rendered(0, 20, 20, SolidPng(Colors.Black)) });

            Assert.Equal(int.MaxValue, maxPaginator.PageCount);

            var overflowDocument = Document((0, int.MaxValue), (1, 1));
            var overflowPlan = Plan(overflowDocument, LabelMediaKind.Thermal, 20, 20);
            Assert.Equal((long)int.MaxValue + 1L, overflowPlan.PageCount);

            Assert.Throws<InvalidOperationException>(() => new LabelPrintPaginator(
                overflowPlan,
                new[]
                {
                    Rendered(0, 20, 20, SolidPng(Colors.Black)),
                    Rendered(1, 20, 20, SolidPng(Colors.White))
                }));
        });
    }

    private static ZplDocument Document(params (int Index, int Quantity)[] designs)
        => new(
            "labels.zpl",
            "synthetic",
            "synthetic",
            designs.Select(item => new ZplDesign(item.Index, item.Index, "^XA^XZ", item.Quantity)).ToArray());

    private static LabelLayoutPlan Plan(
        ZplDocument document,
        LabelMediaKind media,
        double widthMm,
        double heightMm,
        int labelsPerPage = 1)
    {
        var sequence = new LabelOutputSequence(document, new ZplQuantitySelection(ZplQuantityMode.FromFile));
        return LabelLayoutPlanner.CreatePlan(
            sequence,
            new LabelLayoutSettings(media, labelsPerPage),
            widthMm,
            heightMm);
    }

    private static ZplRenderedLabel Rendered(
        int designIndex,
        double widthMm,
        double heightMm,
        byte[] png)
        => new(designIndex, widthMm, heightMm, 8, png);

    private static byte[] SolidPng(Color color)
        => EncodePng(1, 1, [color]);

    private static byte[] HorizontalRedBluePng()
        => EncodePng(2, 1, [Colors.Red, Colors.Blue]);

    private static byte[] EncodePng(int width, int height, IReadOnlyList<Color> colors)
    {
        var pixels = new byte[checked(width * height * 4)];
        for (var index = 0; index < colors.Count; index++)
        {
            var color = colors[index];
            var offset = index * 4;
            pixels[offset] = color.B;
            pixels[offset + 1] = color.G;
            pixels[offset + 2] = color.R;
            pixels[offset + 3] = color.A;
        }

        var bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            width * 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static Color RenderPixel(DocumentPage page, double normalizedX, double normalizedY)
    {
        var width = Math.Max(1, (int)Math.Ceiling(page.Size.Width));
        var height = Math.Max(1, (int)Math.Ceiling(page.Size.Height));
        var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        target.Render(page.Visual);

        var x = Math.Clamp((int)(width * normalizedX), 0, width - 1);
        var y = Math.Clamp((int)(height * normalizedY), 0, height - 1);
        var pixel = new byte[4];
        target.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
        return Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
    }

    private static void AssertColorClose(Color expected, Color actual)
    {
        Assert.InRange(actual.R, Math.Max(0, expected.R - 2), Math.Min(255, expected.R + 2));
        Assert.InRange(actual.G, Math.Max(0, expected.G - 2), Math.Min(255, expected.G + 2));
        Assert.InRange(actual.B, Math.Max(0, expected.B - 2), Math.Min(255, expected.B + 2));
    }

    private static void RunInSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
