using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ImageEditBehaviorCharacterizationTests
{
    [Fact]
    public void GeometryFixtures_ExposeExpectedImageObjectsAndOptionalGeometryRoutes()
    {
        using var multiple = ImageEditPdfFixtureFactory.CreateMultipleImages();
        var multipleTypes = ImageEditNativeCharacterizationHarness.Inspect(
            multiple.Path,
            context => context.GetObjects().Select(item => item.Type).ToArray());
        Assert.Equal(2, multipleTypes.Count(type => type == 3));

        using var rotated = ImageEditPdfFixtureFactory.CreateRotatedImage();
        var rotatedEvidence = ImageEditNativeCharacterizationHarness.Inspect(
            rotated.Path,
            context =>
            {
                var image = context.GetFirstImage();
                return (context.GetMatrix(image.Handle), context.GetRotatedBounds(image.Handle), context.GetRenderedBitmapSize(image.Handle));
            });
        Assert.True(Math.Abs(rotatedEvidence.Item1.B) > 0.01d || Math.Abs(rotatedEvidence.Item1.C) > 0.01d);
        Assert.All(new[]
        {
            rotatedEvidence.Item2.X1, rotatedEvidence.Item2.Y1,
            rotatedEvidence.Item2.X2, rotatedEvidence.Item2.Y2,
            rotatedEvidence.Item2.X3, rotatedEvidence.Item2.Y3,
            rotatedEvidence.Item2.X4, rotatedEvidence.Item2.Y4
        }, value => Assert.True(double.IsFinite(value)));
        Assert.True(rotatedEvidence.Item3.Width > 0);
        Assert.True(rotatedEvidence.Item3.Height > 0);

        using var overlap = ImageEditPdfFixtureFactory.CreateOverlappingImagesAndVector();
        var overlapTypes = ImageEditNativeCharacterizationHarness.Inspect(
            overlap.Path,
            context => context.GetObjects().Select(item => item.Type).ToArray());
        Assert.Equal(2, overlapTypes.Count(type => type == 3));
        Assert.Contains(overlapTypes, type => type != 3);

        using var nearEdge = ImageEditPdfFixtureFactory.CreateNearEdgeImage();
        var edgeImages = ImageEditNativeCharacterizationHarness.Inspect(
            nearEdge.Path,
            context => context.GetObjects().Count(item => item.Type == 3));
        Assert.Equal(1, edgeImages);
    }

    [Fact]
    public void CoreMatrixMutation_SaveReopen_PersistsAndRenders()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateImageWithVectorNeighbor();
        var before = ImageEditNativeCharacterizationHarness.Inspect(
            fixture.Path,
            context =>
            {
                var image = context.GetFirstImage();
                return context.GetMatrix(image.Handle);
            });

        var output = Path.Combine(fixture.DirectoryPath, "matrix-output.pdf");
        ImageEditNativeCharacterizationHarness.MutateAndSave(
            fixture.Path,
            output,
            context =>
            {
                var image = context.GetFirstImage();
                var current = context.GetMatrix(image.Handle);
                context.SetMatrix(image.Handle, current with { E = current.E + 20d, F = current.F + 10d });
            });

        var after = ImageEditNativeCharacterizationHarness.Inspect(
            output,
            context => context.GetMatrix(context.GetFirstImage().Handle));
        Assert.Equal(before.E + 20d, after.E, precision: 3);
        Assert.Equal(before.F + 10d, after.F, precision: 3);

        using var session = PdfDocumentSession.Open(output);
        var rendered = session.RenderPage(0, 36d);
        Assert.True(rendered.PixelWidth > 0);
        Assert.True(rendered.PixelHeight > 0);
    }

    [Fact]
    public void Opacity_SaveReopenRender_CharacterizationReport()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateImageWithVectorNeighbor();
        var samples = new List<string>();
        foreach (var alpha in new byte[] { 255, 128, 0 })
        {
            var output = Path.Combine(fixture.DirectoryPath, $"opacity-{alpha}.pdf");
            ImageEditNativeCharacterizationHarness.MutateAndSave(
                fixture.Path,
                output,
                context => context.SetOpacity(context.GetFirstImage().Handle, alpha));
            using var session = PdfDocumentSession.Open(output);
            var pixel = PixelAt(session.RenderPage(0, 72d), 100, 95);
            samples.Add($"{alpha}=R{pixel.R},G{pixel.G},B{pixel.B},A{pixel.A}");
        }

        Assert.Fail("F6 opacity characterization: " + string.Join("; ", samples));
    }

    [Fact]
    public void ZOrder_SaveReopenRender_CharacterizationReport()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateOverlappingImagesAndVector();
        Rgba before;
        using (var source = PdfDocumentSession.Open(fixture.Path))
            before = PixelAt(source.RenderPage(0, 72d), 160, 180);

        var beforeTypes = ImageEditNativeCharacterizationHarness.Inspect(
            fixture.Path,
            context => context.GetObjects().Select(item => item.Type).ToArray());

        var output = Path.Combine(fixture.DirectoryPath, "zorder-output.pdf");
        ImageEditNativeCharacterizationHarness.MutateAndSave(
            fixture.Path,
            output,
            context =>
            {
                var objects = context.GetObjects();
                var firstImage = objects.First(item => item.Type == 3);
                context.MoveObjectToIndex(firstImage.Handle, objects.Count - 1);
            });

        Rgba after;
        using (var saved = PdfDocumentSession.Open(output))
            after = PixelAt(saved.RenderPage(0, 72d), 160, 180);
        var afterTypes = ImageEditNativeCharacterizationHarness.Inspect(
            output,
            context => context.GetObjects().Select(item => item.Type).ToArray());

        Assert.Fail(
            $"F6 z-order characterization: before={before}; after={after}; " +
            $"beforeTypes=[{string.Join(',', beforeTypes)}]; afterTypes=[{string.Join(',', afterTypes)}]");
    }

    private static Rgba PixelAt(PdfRenderedPage page, int x, int y)
    {
        Assert.InRange(x, 0, page.PixelWidth - 1);
        Assert.InRange(y, 0, page.PixelHeight - 1);
        var offset = checked((y * page.Stride) + (x * 4));
        return new Rgba(
            page.Pixels[offset + 2],
            page.Pixels[offset + 1],
            page.Pixels[offset],
            page.Pixels[offset + 3]);
    }

    private readonly record struct Rgba(byte R, byte G, byte B, byte A);
}
