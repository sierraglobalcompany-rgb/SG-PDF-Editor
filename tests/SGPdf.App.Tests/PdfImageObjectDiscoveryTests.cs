using System.Collections;
using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfImageObjectDiscoveryTests
{
    [Fact]
    public void ImagePageObjects_AreReturned_AndVectorNeighborIsIgnored()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateImageWithVectorNeighbor();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var method = typeof(PdfDocumentSession).GetMethod(
            "GetImageObjects",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var result = Assert.IsAssignableFrom<IEnumerable>(
            method!.Invoke(session, new object?[] { 0, CancellationToken.None }));
        var items = result.Cast<object>().ToArray();

        var image = Assert.Single(items);
        Assert.Equal(0, Read<int>(image, "PageIndex"));
        Assert.True(Read<int>(image, "PageObjectIndex") >= 0);

        var matrix = Read<object>(image, "Matrix");
        foreach (var name in new[] { "A", "B", "C", "D", "E", "F" })
            Assert.True(double.IsFinite(Convert.ToDouble(Read<object>(matrix, name))));

        var bounds = Read<object>(image, "Bounds");
        var left = Convert.ToDouble(Read<object>(bounds, "Left"));
        var bottom = Convert.ToDouble(Read<object>(bounds, "Bottom"));
        var right = Convert.ToDouble(Read<object>(bounds, "Right"));
        var top = Convert.ToDouble(Read<object>(bounds, "Top"));
        Assert.True(double.IsFinite(left) && double.IsFinite(bottom) && double.IsFinite(right) && double.IsFinite(top));
        Assert.True(right > left);
        Assert.True(top > bottom);

        var metadata = Read<object>(image, "Metadata");
        Assert.Equal((uint)16, Read<uint>(metadata, "PixelWidth"));
        Assert.Equal((uint)10, Read<uint>(metadata, "PixelHeight"));
        Assert.True(Read<uint>(metadata, "BitsPerPixel") > 0);
    }

    private static T Read<T>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsType<T>(property!.GetValue(target));
    }
}
