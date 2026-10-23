using System.Reflection;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Labels;
using SGPdf.App.Features.Signing;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;
using SkiaSharp;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfImageWatermarkTests
{
    [Theory]
    [InlineData("Center", 105d, 177.5d)]
    [InlineData("TopLeft", 12d, 343d)]
    [InlineData("TopRight", 198d, 343d)]
    [InlineData("BottomLeft", 12d, 12d)]
    [InlineData("BottomRight", 198d, 12d)]
    public void Planner_UsesFiveApprovedAnchorsAndTwelvePointMargin(
        string anchorName,
        double expectedX,
        double expectedY)
    {
        var anchor = Enum.Parse<WatermarkAnchor>(anchorName);
        var placements = Plan(
            imageWidth: 4,
            imageHeight: 2,
            widthPercent: 30d,
            range: UtilityPageRange.Current(0),
            pageSizes: [new PdfPageSize(300d, 400d)],
            anchor);

        var placement = Assert.Single(placements);
        Assert.Equal(0, placement.PageIndex);
        Assert.Equal(90d, placement.Matrix.A, precision: 3);
        Assert.Equal(45d, placement.Matrix.D, precision: 3);
        Assert.Equal(expectedX, placement.Matrix.E, precision: 3);
        Assert.Equal(expectedY, placement.Matrix.F, precision: 3);
        Assert.Equal(0d, placement.Matrix.B);
        Assert.Equal(0d, placement.Matrix.C);
    }

    [Fact]
    public void Planner_PreservesPortraitAndLandscapeSizesAndInclusivePageRange()
    {
        var placements = Plan(
            imageWidth: 2,
            imageHeight: 1,
            widthPercent: 30d,
            range: UtilityPageRange.Explicit(1, 2),
            pageSizes: [new PdfPageSize(300d, 400d), new PdfPageSize(500d, 300d), new PdfPageSize(700d, 500d)],
            WatermarkAnchor.Center);

        Assert.Equal(new[] { 1, 2 }, placements.Select(item => item.PageIndex));
        Assert.Equal(150d, placements[0].Matrix.A, precision: 3);
        Assert.Equal(75d, placements[0].Matrix.D, precision: 3);
        Assert.Equal(175d, placements[0].Matrix.E, precision: 3);
        Assert.Equal(112.5d, placements[0].Matrix.F, precision: 3);
        Assert.Equal(210d, placements[1].Matrix.A, precision: 3);
        Assert.Equal(105d, placements[1].Matrix.D, precision: 3);
    }

    [Fact]
    public void Planner_ConstrainsTallImageToUsablePageHeight()
    {
        var placement = Assert.Single(Plan(
            imageWidth: 1,
            imageHeight: 8,
            widthPercent: 80d,
            range: UtilityPageRange.All,
            pageSizes: [new PdfPageSize(300d, 400d)],
            WatermarkAnchor.Center));

        Assert.Equal(47d, placement.Matrix.A, precision: 3);
        Assert.Equal(376d, placement.Matrix.D, precision: 3);
        Assert.Equal(126.5d, placement.Matrix.E, precision: 3);
        Assert.Equal(12d, placement.Matrix.F, precision: 3);
    }

    [Theory]
    [InlineData(9d)]
    [InlineData(81d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Planner_RejectsInvalidWidthPercentage(double widthPercent)
    {
        Assert.ThrowsAny<ArgumentException>(() => Plan(
            imageWidth: 4,
            imageHeight: 2,
            widthPercent,
            UtilityPageRange.All,
            [new PdfPageSize(300d, 400d)],
            WatermarkAnchor.Center));
    }

    [Fact]
    public void Planner_RejectsMoreThanOneHundredFiftySelectedPages()
    {
        var pageSizes = Enumerable.Repeat(new PdfPageSize(300d, 400d), 151).ToArray();
        Assert.Throws<ArgumentOutOfRangeException>(() => Plan(
            imageWidth: 4,
            imageHeight: 2,
            widthPercent: 30d,
            UtilityPageRange.All,
            pageSizes,
            WatermarkAnchor.Center));
    }

    [Fact]
    public void AssetLoader_ScalesOriginalPngAlphaByRequestedOpacity()
    {
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var path = Path.Combine(directory.Path, "watermark.png");
        Task5ImageTestFixture.WritePng(path, transparent: true);

        var asset = LoadAsset(path, 35d);
        var pixels = GetProperty<ReadOnlyMemory<byte>>(asset, "BgraPixels").Span;

        Assert.Equal(4, GetProperty<int>(asset, "PixelWidth"));
        Assert.Equal(2, GetProperty<int>(asset, "PixelHeight"));
        Assert.Equal((byte)22, pixels[3]);
        Assert.Equal((byte)89, pixels[(2 * 4) + 3]);
        Assert.Equal((byte)220, pixels[0]);
        Assert.Equal((byte)120, pixels[1]);
        Assert.Equal((byte)20, pixels[2]);
    }

    [Fact]
    public void AssetLoader_RejectsEncodedFileOverTwentyMiBBeforeReadingFormat()
    {
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var path = Path.Combine(directory.Path, "oversize.png");
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            stream.SetLength((20L * 1024L * 1024L) + 1L);

        var error = Assert.Throws<InvalidDataException>(() => LoadAsset(path, 35d));

        Assert.Contains("20 MiB", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(8001, 1)]
    [InlineData(4000, 4001)]
    public void AssetLoader_RejectsOversizedDimensionsBeforeCopyingPixels(int width, int height)
    {
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var path = Path.Combine(directory.Path, "oversize-dimensions.png");
        Task5ImageTestFixture.WritePng(path, transparent: false);
        RewritePngDimensions(path, width, height);

        Assert.Throws<InvalidDataException>(() => LoadAsset(path, 35d));
    }

    [Fact]
    public void AssetLoader_RejectsJpegWithExifOrientationOtherThanOne()
    {
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var path = Path.Combine(directory.Path, "rotated.jpg");
        Task5ImageTestFixture.WriteJpeg(path);
        AddJpegExifOrientation(path, 6);

        var error = Assert.Throws<InvalidDataException>(() => LoadAsset(path, 35d));

        Assert.Contains("EXIF", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Coordinator_SaveReopenRendersOpacityAndPngTransparencyOverPortraitAndLandscapePages()
    {
        using var fixture = WatermarkPdfFixture.Create();
        var imagePath = Path.Combine(fixture.DirectoryPath, "red-transparent.png");
        var destination = Path.Combine(fixture.DirectoryPath, "watermarked.pdf");
        WriteHalfTransparentRedPng(imagePath);
        var sourceHash = SHA256.HashData(File.ReadAllBytes(fixture.SourcePath));

        var result = await CreateCopyAsync(
            fixture.SourcePath,
            imagePath,
            destination,
            UtilityPageRange.All,
            widthPercent: 30d,
            opacityPercent: 35d,
            WatermarkAnchor.Center);

        Assert.True(GetProperty<bool>(result, "Succeeded"), GetProperty<string?>(result, "Error"));
        Assert.True(File.Exists(destination));
        Assert.Equal(sourceHash, SHA256.HashData(File.ReadAllBytes(fixture.SourcePath)));

        using var source = PdfDocumentSession.Open(fixture.SourcePath);
        using var output = PdfDocumentSession.Open(destination);
        Assert.Equal(2, output.PageCount);
        for (var pageIndex = 0; pageIndex < 2; pageIndex++)
        {
            Assert.Equal(
                source.GetPageText(pageIndex, CancellationToken.None),
                output.GetPageText(pageIndex, CancellationToken.None));
            Assert.Equal(source.GetImageObjects(pageIndex).Count + 1, output.GetImageObjects(pageIndex).Count);
        }

        var sourcePortrait = source.RenderPage(0, 72d);
        var outputPortrait = output.RenderPage(0, 72d);
        var portraitRed = Sample(outputPortrait, 130d, 200d, 400d);
        AssertBlend(sourcePortrait, outputPortrait, 130d, 200d, 400d, (255, 0, 0), 0.35d);
        AssertColorNear(
            Sample(sourcePortrait, 170d, 200d, 400d),
            Sample(outputPortrait, 170d, 200d, 400d),
            tolerance: 2,
            "A fully transparent source pixel must leave the original render unchanged.");
        Assert.True(portraitRed.R > portraitRed.G + 50);

        var fullOpacityDestination = Path.Combine(fixture.DirectoryPath, "watermarked-100-percent.pdf");
        var fullOpacityResult = await CreateCopyAsync(
            fixture.SourcePath,
            imagePath,
            fullOpacityDestination,
            UtilityPageRange.Current(0),
            widthPercent: 30d,
            opacityPercent: 100d,
            WatermarkAnchor.Center);
        Assert.True(GetProperty<bool>(fullOpacityResult, "Succeeded"), GetProperty<string?>(fullOpacityResult, "Error"));
        using var fullOpacityOutput = PdfDocumentSession.Open(fullOpacityDestination);
        var fullOpacityRed = Sample(fullOpacityOutput.RenderPage(0, 72d), 130d, 200d, 400d);
        Assert.True(
            Math.Abs(portraitRed.G - fullOpacityRed.G) > 20,
            "The same PNG rendered at 100% opacity must visibly differ from its 35% render.");

        var sourceLandscape = source.RenderPage(1, 72d);
        var outputLandscape = output.RenderPage(1, 72d);
        AssertBlend(sourceLandscape, outputLandscape, 200d, 150d, 300d, (255, 0, 0), 0.35d);
        ExportEvidenceIfRequested(fixture, imagePath, destination, source, output);
    }

    [Fact]
    public async Task Coordinator_PageTwoRangeLeavesOtherPageRenderAndImageCountUnchanged()
    {
        using var fixture = WatermarkPdfFixture.Create();
        var imagePath = Path.Combine(fixture.DirectoryPath, "red-transparent.png");
        var destination = Path.Combine(fixture.DirectoryPath, "page-two-only.pdf");
        WriteHalfTransparentRedPng(imagePath);

        var result = await CreateCopyAsync(
            fixture.SourcePath,
            imagePath,
            destination,
            UtilityPageRange.Explicit(1, 1),
            widthPercent: 30d,
            opacityPercent: 100d,
            WatermarkAnchor.Center);

        Assert.True(GetProperty<bool>(result, "Succeeded"), GetProperty<string?>(result, "Error"));
        using var source = PdfDocumentSession.Open(fixture.SourcePath);
        using var output = PdfDocumentSession.Open(destination);
        Assert.Equal(source.RenderPage(0, 72d).Pixels.ToArray(), output.RenderPage(0, 72d).Pixels.ToArray());
        Assert.Equal(source.GetImageObjects(0).Count, output.GetImageObjects(0).Count);
        Assert.Equal(source.GetImageObjects(1).Count + 1, output.GetImageObjects(1).Count);
        Assert.NotEqual(source.RenderPage(1, 72d).Pixels.ToArray(), output.RenderPage(1, 72d).Pixels.ToArray());
    }

    [Fact]
    public async Task Coordinator_AppliesOpaqueJpegAtRequestedAnchorAndPreservesSource()
    {
        using var fixture = WatermarkPdfFixture.Create();
        var imagePath = Path.Combine(fixture.DirectoryPath, "solid.jpg");
        var destination = Path.Combine(fixture.DirectoryPath, "jpeg-watermark.pdf");
        Task5ImageTestFixture.WriteJpeg(imagePath);
        var sourceHash = SHA256.HashData(File.ReadAllBytes(fixture.SourcePath));

        var result = await CreateCopyAsync(
            fixture.SourcePath,
            imagePath,
            destination,
            UtilityPageRange.Current(0),
            widthPercent: 40d,
            opacityPercent: 100d,
            WatermarkAnchor.TopRight);

        Assert.True(GetProperty<bool>(result, "Succeeded"), GetProperty<string?>(result, "Error"));
        Assert.Equal(sourceHash, SHA256.HashData(File.ReadAllBytes(fixture.SourcePath)));
        using var output = PdfDocumentSession.Open(destination);
        var matrix = Assert.Single(output.GetImageObjects(0)).Matrix;
        Assert.Equal(120d, matrix.A, precision: 2);
        Assert.Equal(60d, matrix.D, precision: 2);
        Assert.Equal(168d, matrix.E, precision: 2);
        Assert.Equal(328d, matrix.F, precision: 2);
        Assert.Empty(output.GetImageObjects(1));
    }

    [Fact]
    public async Task Coordinator_RejectsRotationAndAmbiguousCropWithoutOutput()
    {
        using var fixture = WatermarkPdfFixture.Create();
        var imagePath = Path.Combine(fixture.DirectoryPath, "red-transparent.png");
        WriteHalfTransparentRedPng(imagePath);

        var rotated = fixture.CreatePdfVariant("rotated.pdf", pageIndex: 0, rotate: 90);
        var rotatedDestination = Path.Combine(fixture.DirectoryPath, "rotated-out.pdf");
        var rotatedResult = await CreateCopyAsync(rotated, imagePath, rotatedDestination, UtilityPageRange.Current(0), 30, 35, WatermarkAnchor.Center);
        Assert.False(GetProperty<bool>(rotatedResult, "Succeeded"));
        Assert.False(File.Exists(rotatedDestination));

        var cropped = fixture.CreatePdfVariant("cropped.pdf", pageIndex: 0, cropLeft: 10d);
        var croppedDestination = Path.Combine(fixture.DirectoryPath, "cropped-out.pdf");
        var croppedResult = await CreateCopyAsync(cropped, imagePath, croppedDestination, UtilityPageRange.Current(0), 30, 35, WatermarkAnchor.Center);
        Assert.False(GetProperty<bool>(croppedResult, "Succeeded"));
        Assert.False(File.Exists(croppedDestination));
    }

    [Fact]
    public async Task Coordinator_RejectsSignedEncryptedAndComplexPdfsWithoutOutput()
    {
        using var fixture = WatermarkPdfFixture.Create();
        var imagePath = Path.Combine(fixture.DirectoryPath, "red-transparent.png");
        WriteHalfTransparentRedPng(imagePath);

        var signedPath = Path.Combine(fixture.DirectoryPath, "signed.pdf");
        using (var rsa = RSA.Create(2048))
        {
            var request = new CertificateRequest(
                "CN=SGPDF F15D synthetic",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddDays(1));
            PdfCryptographicSignatureWriter.Write(fixture.SourcePath, signedPath, certificate);
        }

        using (var signed = PdfDocumentSession.Open(signedPath))
            Assert.Equal(1, signed.GetCryptographicSignatureCount());
        await AssertRejectedAsync(signedPath, Path.Combine(fixture.DirectoryPath, "signed-out.pdf"));

        using (var protectedFixture = OrganizePdfFixtureFactory.CreateProtected())
        {
            await AssertRejectedAsync(
                protectedFixture.Path,
                Path.Combine(protectedFixture.DirectoryPath, "encrypted-out.pdf"));
        }

        using (var form = OrganizePdfFixtureFactory.CreateAcroForm())
            await AssertRejectedAsync(form.Path, Path.Combine(form.DirectoryPath, "form-out.pdf"));
        using (var navigation = OrganizePdfFixtureFactory.CreateNavigation())
            await AssertRejectedAsync(navigation.Path, Path.Combine(navigation.DirectoryPath, "navigation-out.pdf"));
        using (var destination = OrganizePdfFixtureFactory.CreateNamedDestination())
            await AssertRejectedAsync(destination.Path, Path.Combine(destination.DirectoryPath, "destination-out.pdf"));
        using (var labels = OrganizePdfFixtureFactory.CreatePageLabels())
            await AssertRejectedAsync(labels.Path, Path.Combine(labels.DirectoryPath, "labels-out.pdf"));
        using (var attachment = OrganizePdfFixtureFactory.CreateAttachment())
            await AssertRejectedAsync(attachment.Path, Path.Combine(attachment.DirectoryPath, "attachment-out.pdf"));
        using (var tagged = OrganizePdfFixtureFactory.CreateTagged())
            await AssertRejectedAsync(tagged.Path, Path.Combine(tagged.DirectoryPath, "tagged-out.pdf"));

        async Task AssertRejectedAsync(string sourcePath, string destinationPath)
        {
            var result = await CreateCopyAsync(
                sourcePath,
                imagePath,
                destinationPath,
                UtilityPageRange.All,
                30,
                35,
                WatermarkAnchor.Center);

            Assert.False(GetProperty<bool>(result, "Succeeded"));
            Assert.False(File.Exists(destinationPath));
        }
    }

    [Fact]
    public async Task Coordinator_RejectsChangedSourceAndImageBeforePublishing()
    {
        using var sourceFixture = WatermarkPdfFixture.Create();
        var sourceImagePath = Path.Combine(sourceFixture.DirectoryPath, "watermark.png");
        WriteHalfTransparentRedPng(sourceImagePath);
        var originalSourceHash = SHA256.HashData(File.ReadAllBytes(sourceFixture.SourcePath));
        var changedSourceDestination = Path.Combine(sourceFixture.DirectoryPath, "changed-source-out.pdf");
        var qpdfPath = Path.Combine(AppContext.BaseDirectory, "qpdf", "qpdf.exe");
        var sourceChangingCoordinator = new PdfImageWatermarkCoordinator(
            qpdfPath,
            (_, _, _) =>
            {
                File.AppendAllText(sourceFixture.SourcePath, "% synthetic concurrent source change\n", Encoding.ASCII);
                return Task.FromResult(new ChildProcessResult(2, string.Empty, string.Empty));
            });

        var changedSourceResult = await sourceChangingCoordinator.CreateCopyAsync(
            sourceFixture.SourcePath,
            sourceImagePath,
            changedSourceDestination,
            UtilityPageRange.All,
            30,
            35,
            WatermarkAnchor.Center,
            CancellationToken.None);

        Assert.False(changedSourceResult.Succeeded);
        Assert.Contains("cambió", changedSourceResult.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.False(originalSourceHash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(sourceFixture.SourcePath))));
        Assert.False(File.Exists(changedSourceDestination));
        Assert.Empty(Directory.GetFiles(sourceFixture.DirectoryPath, ".sgpdf-image-watermark-*.pdf"));

        using var imageFixture = WatermarkPdfFixture.Create();
        var imagePath = Path.Combine(imageFixture.DirectoryPath, "watermark.png");
        WriteHalfTransparentRedPng(imagePath);
        var imageDestination = Path.Combine(imageFixture.DirectoryPath, "changed-image-out.pdf");
        var imageMutation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var watcher = new FileSystemWatcher(imageFixture.DirectoryPath, ".sgpdf-image-watermark-*.pdf")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };
        watcher.Created += (_, _) =>
        {
            try
            {
                File.AppendAllText(imagePath, "% synthetic concurrent image change\n", Encoding.ASCII);
                imageMutation.TrySetResult(true);
            }
            catch (Exception exception)
            {
                imageMutation.TrySetException(exception);
            }
        };

        var changedImageResult = await CreateCopyAsync(
            imageFixture.SourcePath,
            imagePath,
            imageDestination,
            UtilityPageRange.All,
            30,
            35,
            WatermarkAnchor.Center);
        await imageMutation.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.False(GetProperty<bool>(changedImageResult, "Succeeded"));
        Assert.Contains("imagen", GetProperty<string?>(changedImageResult, "Error") ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(imageDestination));
        Assert.Empty(Directory.GetFiles(imageFixture.DirectoryPath, ".sgpdf-image-watermark-*.pdf"));
    }

    [Fact]
    public async Task Coordinator_CollisionSamePathAndCancellationPreserveOriginalAndDestination()
    {
        using var fixture = WatermarkPdfFixture.Create();
        var imagePath = Path.Combine(fixture.DirectoryPath, "red-transparent.png");
        WriteHalfTransparentRedPng(imagePath);
        var sourceHash = SHA256.HashData(File.ReadAllBytes(fixture.SourcePath));
        var destination = Path.Combine(fixture.DirectoryPath, "existing.pdf");
        File.WriteAllText(destination, "KEEP-DESTINATION");

        var collision = await CreateCopyAsync(
            fixture.SourcePath, imagePath, destination, UtilityPageRange.All, 30, 35, WatermarkAnchor.Center);
        var samePath = await CreateCopyAsync(
            fixture.SourcePath, imagePath, fixture.SourcePath, UtilityPageRange.All, 30, 35, WatermarkAnchor.Center);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateCopyAsync(
            fixture.SourcePath, imagePath, Path.Combine(fixture.DirectoryPath, "canceled.pdf"),
            UtilityPageRange.All, 30, 35, WatermarkAnchor.Center, canceled.Token));

        Assert.False(GetProperty<bool>(collision, "Succeeded"));
        Assert.False(GetProperty<bool>(samePath, "Succeeded"));
        Assert.Equal("KEEP-DESTINATION", File.ReadAllText(destination));
        Assert.Equal(sourceHash, SHA256.HashData(File.ReadAllBytes(fixture.SourcePath)));
        Assert.False(File.Exists(Path.Combine(fixture.DirectoryPath, "canceled.pdf")));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, ".sgpdf-image-watermark-*.pdf"));
    }

    private static IReadOnlyList<TestPlacement> Plan(
        int imageWidth,
        int imageHeight,
        double widthPercent,
        UtilityPageRange range,
        IReadOnlyList<PdfPageSize> pageSizes,
        WatermarkAnchor anchor)
    {
        var assembly = typeof(MainWindow).Assembly;
        var plannerType = assembly.GetType("SGPdf.App.Features.Utilities.PdfImageWatermarkPlanner");
        Assert.NotNull(plannerType);
        if (plannerType is null)
            return Array.Empty<TestPlacement>();

        var planMethod = plannerType.GetMethod("Plan", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(planMethod);
        if (planMethod is null)
            return Array.Empty<TestPlacement>();

        object? raw;
        try
        {
            raw = planMethod.Invoke(null, [imageWidth, imageHeight, widthPercent, range, pageSizes, anchor]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }

        Assert.NotNull(raw);
        var result = new List<TestPlacement>();
        foreach (var item in Assert.IsAssignableFrom<System.Collections.IEnumerable>(raw))
        {
            Assert.NotNull(item);
            var itemType = item!.GetType();
            var pageIndex = (int)itemType.GetProperty("PageIndex")!.GetValue(item)!;
            var matrix = (PdfObjectMatrix)itemType.GetProperty("Matrix")!.GetValue(item)!;
            result.Add(new TestPlacement(pageIndex, matrix));
        }

        return result;
    }

    private static object LoadAsset(string path, double opacityPercent)
    {
        var loaderType = typeof(MainWindow).Assembly.GetType("SGPdf.App.Features.Utilities.PdfImageWatermarkAssetLoader");
        Assert.NotNull(loaderType);
        if (loaderType is null)
            throw new InvalidOperationException("The image watermark asset loader has not been implemented.");

        var load = loaderType.GetMethod("Load", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(load);
        if (load is null)
            throw new InvalidOperationException("The image watermark asset loader has no Load method.");

        try
        {
            return load.Invoke(null, [path, opacityPercent, CancellationToken.None])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static T GetProperty<T>(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsType<T>(property!.GetValue(instance));
    }

    private static async Task<object> CreateCopyAsync(
        string sourcePath,
        string imagePath,
        string destinationPath,
        UtilityPageRange range,
        double widthPercent,
        double opacityPercent,
        WatermarkAnchor anchor,
        CancellationToken cancellationToken = default)
    {
        var coordinatorType = typeof(MainWindow).Assembly.GetType("SGPdf.App.Features.Utilities.PdfImageWatermarkCoordinator");
        Assert.NotNull(coordinatorType);
        if (coordinatorType is null)
            throw new InvalidOperationException("The image watermark coordinator has not been implemented.");

        var coordinator = Activator.CreateInstance(coordinatorType, nonPublic: true);
        Assert.NotNull(coordinator);
        if (coordinator is null)
            throw new InvalidOperationException("The image watermark coordinator could not be created.");

        var create = coordinatorType.GetMethod("CreateCopyAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(create);
        if (create is null)
            throw new InvalidOperationException("The image watermark coordinator has no CreateCopyAsync method.");

        var pending = Assert.IsAssignableFrom<Task>(create.Invoke(
            coordinator,
            [sourcePath, imagePath, destinationPath, range, widthPercent, opacityPercent, anchor, cancellationToken]));
        await pending.ConfigureAwait(false);
        var result = pending.GetType().GetProperty("Result")?.GetValue(pending);
        Assert.NotNull(result);
        return result!;
    }

    private static void WriteHalfTransparentRedPng(string path)
    {
        using var bitmap = new SKBitmap(4, 2, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width; x++)
            bitmap.SetPixel(x, y, new SKColor(255, 0, 0, x < 2 ? (byte)255 : (byte)0));

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    private static void AssertBlend(
        PdfRenderedPage source,
        PdfRenderedPage output,
        double xPoints,
        double yPoints,
        double pageHeightPoints,
        (byte R, byte G, byte B) imageColor,
        double opacity)
    {
        var before = Sample(source, xPoints, yPoints, pageHeightPoints);
        var after = Sample(output, xPoints, yPoints, pageHeightPoints);
        var expected = (
            R: (byte)Math.Round(imageColor.R * opacity + before.R * (1d - opacity)),
            G: (byte)Math.Round(imageColor.G * opacity + before.G * (1d - opacity)),
            B: (byte)Math.Round(imageColor.B * opacity + before.B * (1d - opacity)));
        AssertColorNear(expected, after, tolerance: 20, "The saved/reopened watermark render has the wrong alpha blend.");
    }

    private static (byte R, byte G, byte B) Sample(
        PdfRenderedPage page,
        double xPoints,
        double yPoints,
        double pageHeightPoints)
    {
        var x = Math.Clamp((int)Math.Round(xPoints * page.Dpi / 72d), 0, page.PixelWidth - 1);
        var y = Math.Clamp((int)Math.Round((pageHeightPoints - yPoints) * page.Dpi / 72d), 0, page.PixelHeight - 1);
        var offset = checked((y * page.Stride) + (x * 4));
        return (page.Pixels[offset + 2], page.Pixels[offset + 1], page.Pixels[offset]);
    }

    private static void AssertColorNear(
        (byte R, byte G, byte B) expected,
        (byte R, byte G, byte B) actual,
        int tolerance,
        string message)
    {
        Assert.True(
            Math.Abs(expected.R - actual.R) <= tolerance &&
            Math.Abs(expected.G - actual.G) <= tolerance &&
            Math.Abs(expected.B - actual.B) <= tolerance,
            message);
    }

    private static void ExportEvidenceIfRequested(
        WatermarkPdfFixture fixture,
        string imagePath,
        string outputPath,
        PdfDocumentSession source,
        PdfDocumentSession output)
    {
        var root = Environment.GetEnvironmentVariable("SGPDF_F15D_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(root))
            return;

        var directory = Path.Combine(root, "synthetic-png-alpha");
        Directory.CreateDirectory(directory);
        var files = new List<string>();
        AddCopy(fixture.SourcePath, "source.pdf");
        AddCopy(imagePath, "watermark.png");
        AddCopy(outputPath, "watermarked.pdf");
        for (var pageIndex = 0; pageIndex < 2; pageIndex++)
        {
            SaveRender(source.RenderPage(pageIndex, 72d), $"source-page-{pageIndex + 1}.png");
            SaveRender(output.RenderPage(pageIndex, 72d), $"watermarked-page-{pageIndex + 1}.png");
        }

        var manifest = files
            .Select(name => $"{Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, name))))}  {name}")
            .ToArray();
        File.WriteAllLines(Path.Combine(directory, "SHA256SUMS.txt"), manifest, Encoding.UTF8);

        void AddCopy(string sourcePath, string targetName)
        {
            var target = Path.Combine(directory, targetName);
            File.Copy(sourcePath, target, overwrite: false);
            files.Add(targetName);
        }

        void SaveRender(PdfRenderedPage page, string targetName)
        {
            var bitmap = BitmapSource.Create(
                page.PixelWidth,
                page.PixelHeight,
                96d,
                96d,
                PixelFormats.Bgra32,
                null,
                page.Pixels,
                page.Stride);
            bitmap.Freeze();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = new FileStream(Path.Combine(directory, targetName), FileMode.CreateNew, FileAccess.Write, FileShare.None);
            encoder.Save(stream);
            files.Add(targetName);
        }
    }

    private sealed class WatermarkPdfFixture : IDisposable
    {
        private WatermarkPdfFixture(string directoryPath, string sourcePath)
        {
            DirectoryPath = directoryPath;
            SourcePath = sourcePath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }

        internal string CreatePdfVariant(string fileName, int pageIndex, int rotate = 0, double? cropLeft = null)
        {
            var path = Path.Combine(DirectoryPath, fileName);
            using var document = PdfSharp.Pdf.IO.PdfReader.Open(SourcePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Modify);
            var page = document.Pages[pageIndex];
            page.Rotate = rotate;
            if (cropLeft is not null)
                page.CropBox = new PdfSharp.Pdf.PdfRectangle(new PdfSharp.Drawing.XRect(cropLeft.Value, 0d, 300d - cropLeft.Value, 400d));
            document.Save(path);
            return path;
        }

        internal static WatermarkPdfFixture Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f15d-watermark-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var sourcePath = Path.Combine(directory, "source.pdf");
            WritePdf(sourcePath);
            return new WatermarkPdfFixture(directory, sourcePath);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }

        private static void WritePdf(string path)
        {
            const string firstPage = "q\n1 1 1 rg\n0 0 300 400 re f\nQ\nBT\n/F1 16 Tf\n0 0 0 rg\n1 0 0 1 24 200 Tm\n(BASE PAGE ONE) Tj\nET\n";
            const string secondPage = "q\n0.6 0.8 0.6 rg\n0 0 500 300 re f\nQ\nBT\n/F1 16 Tf\n0 0 0 rg\n1 0 0 1 185 150 Tm\n(BASE PAGE TWO) Tj\nET\n";
            var objects = new[]
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Resources << /Font << /F1 5 0 R >> >> /Contents 6 0 R >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 500 300] /Resources << /Font << /F1 5 0 R >> >> /Contents 7 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
                MakeStream(firstPage),
                MakeStream(secondPage)
            };

            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true) { NewLine = "\n" };
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
            writer.Write($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
            foreach (var offset in offsets.Skip(1))
                writer.Write($"{offset:0000000000} 00000 n \n");
            writer.Write($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        }

        private static string MakeStream(string value)
            => $"<< /Length {Encoding.ASCII.GetByteCount(value)} >>\nstream\n{value}endstream";
    }

    private static void RewritePngDimensions(string path, int width, int height)
    {
        var bytes = File.ReadAllBytes(path);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16, 4), checked((uint)width));
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20, 4), checked((uint)height));
        var crc = ComputePngCrc32(bytes.AsSpan(12, 17));
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(29, 4), crc);
        File.WriteAllBytes(path, bytes);
    }

    private static uint ComputePngCrc32(ReadOnlySpan<byte> bytes)
    {
        var crc = uint.MaxValue;
        foreach (var value in bytes)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320u : crc >> 1;
        }
        return ~crc;
    }

    private static void AddJpegExifOrientation(string path, ushort orientation)
    {
        var jpeg = File.ReadAllBytes(path);
        Assert.Equal(new byte[] { 0xff, 0xd8 }, jpeg[..2]);

        byte[] payload =
        [
            0x45, 0x78, 0x69, 0x66, 0, 0, // Exif\0\0
            0x49, 0x49, 0x2a, 0, 8, 0, 0, 0, // little-endian TIFF header
            1, 0, // one IFD entry
            0x12, 0x01, 3, 0, 1, 0, 0, 0, // orientation, SHORT, count 1
            (byte)orientation, 0, 0, 0, // value and padding
            0, 0, 0, 0 // next IFD offset
        ];
        var segmentLength = checked((ushort)(payload.Length + 2));
        var prefix = new byte[]
        {
            0xff, 0xe1,
            (byte)(segmentLength >> 8), (byte)segmentLength
        };
        var combined = new byte[jpeg.Length + prefix.Length + payload.Length];
        jpeg.AsSpan(0, 2).CopyTo(combined);
        prefix.CopyTo(combined, 2);
        payload.CopyTo(combined, 2 + prefix.Length);
        jpeg.AsSpan(2).CopyTo(combined.AsSpan(2 + prefix.Length + payload.Length));
        File.WriteAllBytes(path, combined);
    }

    private sealed record TestPlacement(int PageIndex, PdfObjectMatrix Matrix);
}
