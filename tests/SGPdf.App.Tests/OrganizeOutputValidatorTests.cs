using System.IO;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OrganizeOutputValidatorTests
{
    [Fact]
    public void Validate_ExactCountSizesRotations_RendersEveryPageAt36DpiSequentially()
    {
        using var fixture = Task4PdfFixture.CreateMarkerSource();
        var rendered = new List<(int PageIndex, double Dpi)>();
        var validator = new OrganizeOutputValidator((pageIndex, dpi) => rendered.Add((pageIndex, dpi)));
        var expected = new[]
        {
            new OrganizeExpectedPage(new PdfPageSize(200d, 300d), 0),
            new OrganizeExpectedPage(new PdfPageSize(300d, 200d), 1),
            new OrganizeExpectedPage(new PdfPageSize(400d, 250d), 2)
        };

        validator.Validate(fixture.Path, expected);

        Assert.Equal(
            new[] { (0, 36d), (1, 36d), (2, 36d) },
            rendered);
    }

    [Fact]
    public void Validate_WrongPageCount_Throws()
    {
        using var fixture = Task4PdfFixture.CreateMarkerSource();
        var validator = new OrganizeOutputValidator();

        Assert.Throws<InvalidDataException>((Action)(() => validator.Validate(
            fixture.Path,
            new[] { new OrganizeExpectedPage(new PdfPageSize(200d, 300d), 0) })));
    }

    [Fact]
    public void Validate_WrongPageSize_Throws()
    {
        using var fixture = Task4PdfFixture.CreateMarkerSource();
        var validator = new OrganizeOutputValidator();
        var expected = new[]
        {
            new OrganizeExpectedPage(new PdfPageSize(201d, 300d), 0),
            new OrganizeExpectedPage(new PdfPageSize(300d, 200d), 1),
            new OrganizeExpectedPage(new PdfPageSize(400d, 250d), 2)
        };

        Assert.Throws<InvalidDataException>((Action)(() => validator.Validate(fixture.Path, expected)));
    }

    [Fact]
    public void Validate_WrongRotation_Throws()
    {
        using var fixture = Task4PdfFixture.CreateMarkerSource();
        var validator = new OrganizeOutputValidator();
        var expected = new[]
        {
            new OrganizeExpectedPage(new PdfPageSize(200d, 300d), 0),
            new OrganizeExpectedPage(new PdfPageSize(300d, 200d), 0),
            new OrganizeExpectedPage(new PdfPageSize(400d, 250d), 2)
        };

        Assert.Throws<InvalidDataException>((Action)(() => validator.Validate(fixture.Path, expected)));
    }

    [Fact]
    public void Validate_PreCanceled_Aborts()
    {
        using var fixture = Task4PdfFixture.CreateMarkerSource();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var validator = new OrganizeOutputValidator();
        var expected = new[]
        {
            new OrganizeExpectedPage(new PdfPageSize(200d, 300d), 0),
            new OrganizeExpectedPage(new PdfPageSize(300d, 200d), 1),
            new OrganizeExpectedPage(new PdfPageSize(400d, 250d), 2)
        };

        Assert.Throws<OperationCanceledException>((Action)(() => validator.Validate(fixture.Path, expected, cancellation.Token)));
    }
}
