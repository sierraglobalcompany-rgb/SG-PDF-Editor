using SGPdf.App.Features.Labels;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class LabelizeProcessRendererTests
{
    [Fact]
    public async Task RenderAsync_SingleDesign_ReturnsPngAndCleansTempDirectory()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var document = ZplDocumentParser.Parse(
                "single.zpl",
                "^XA^FO40,40^A0N,36,36^FDSG PDF LABEL^FS^XZ");
            var renderer = CreateRenderer(tempRoot);

            var labels = await renderer.RenderAsync(document, ZplRenderOptions.Default);

            var label = Assert.Single(labels);
            Assert.Equal(0, label.DesignIndex);
            Assert.Equal(102d, label.WidthMm);
            Assert.Equal(152d, label.HeightMm);
            Assert.Equal(8, label.Dpmm);
            AssertPng(label.PngBytes);
            Assert.Empty(Directory.EnumerateFileSystemEntries(tempRoot));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task RenderAsync_TwoPrintableDesigns_MapsPngsInSourceOrder()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var document = ZplDocumentParser.Parse(
                "two.zpl",
                "^XA^FO40,40^A0N,36,36^FDFIRST^FS^XZ" +
                "^XA^FO40,40^A0N,36,36^FDSECOND^FS^XZ");
            var renderer = CreateRenderer(tempRoot);

            var labels = await renderer.RenderAsync(document, ZplRenderOptions.Default);

            Assert.Equal(2, labels.Count);
            Assert.Equal([0, 1], labels.Select(label => label.DesignIndex).ToArray());
            Assert.All(labels, label => AssertPng(label.PngBytes));
            Assert.False(labels[0].PngBytes.SequenceEqual(labels[1].PngBytes));
            Assert.Empty(Directory.EnumerateFileSystemEntries(tempRoot));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task RenderAsync_StoredFormatSupportBlock_MapsOnlyPrintableDesign()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var document = ZplDocumentParser.Parse(
                "stored-format.zpl",
                "^XA^DFR:SGFORM.ZPL^FO50,60^A0N,36,36^FN1^FS^XZ" +
                "^XA^XFR:SGFORM.ZPL^FS^FN1^FDPlantilla OK^FS^XZ");
            Assert.Single(document.Designs);
            var renderer = CreateRenderer(tempRoot);

            var labels = await renderer.RenderAsync(document, ZplRenderOptions.Default);

            var label = Assert.Single(labels);
            Assert.Equal(0, label.DesignIndex);
            AssertPng(label.PngBytes);
            Assert.Empty(Directory.EnumerateFileSystemEntries(tempRoot));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task RenderAsync_PreCancelled_LeavesNoRequestTempDirectory()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var document = ZplDocumentParser.Parse(
                "cancel.zpl",
                "^XA^FO40,40^A0N,36,36^FDCANCEL^FS^XZ");
            var renderer = CreateRenderer(tempRoot);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                renderer.RenderAsync(document, ZplRenderOptions.Default, cancellation.Token));

            Assert.Empty(Directory.EnumerateFileSystemEntries(tempRoot));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task RenderAsync_WhenProcessStartFails_CleansRequestTempDirectory()
    {
        var tempRoot = CreateTempDirectory();
        var fakeExecutable = Path.Combine(
            Path.GetTempPath(),
            $"sgpdf-not-executable-{Guid.NewGuid():N}.exe");
        File.WriteAllText(fakeExecutable, "not a Windows executable");

        try
        {
            var document = ZplDocumentParser.Parse(
                "start-failure.zpl",
                "^XA^FO40,40^A0N,36,36^FDSTART FAIL^FS^XZ");
            var renderer = new LabelizeProcessRenderer(
                fakeExecutable,
                TimeSpan.FromSeconds(10),
                tempRoot);

            await Assert.ThrowsAnyAsync<Exception>(() =>
                renderer.RenderAsync(document, ZplRenderOptions.Default));

            Assert.Empty(Directory.EnumerateFileSystemEntries(tempRoot));
        }
        finally
        {
            if (File.Exists(fakeExecutable))
                File.Delete(fakeExecutable);
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void ZplRenderOptions_RejectsUnsupportedDpmm()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ZplRenderOptions(102d, 152d, 10));

        Assert.Equal("dpmm", error.ParamName);
    }

    private static LabelizeProcessRenderer CreateRenderer(string tempRoot)
    {
        var executable = LabelizeRuntime.ResolveBundledExecutablePath(AppContext.BaseDirectory);
        return new LabelizeProcessRenderer(executable, TimeSpan.FromSeconds(10), tempRoot);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "sgpdf-labelize-render-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void AssertPng(byte[] bytes)
    {
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        Assert.True(bytes.Length > signature.Length);
        Assert.True(bytes.AsSpan(0, signature.Length).SequenceEqual(signature));
    }
}
