using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Redaction;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfEditWriterRedactionTests
{
    [Fact]
    public void SaveAsCopy_Redaction_RemovesUnderlyingTextPreservesNeighborAndSource()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateTwoTextObjects();
        var sourceBytes = File.ReadAllBytes(fixture.Path);
        var destination = Path.Combine(fixture.DirectoryPath, "redacted.pdf");

        PdfTextObjectInfo target;
        using (var session = PdfDocumentSession.Open(fixture.Path))
            target = session.GetTextObjects(0).Single(item => item.Text == "CASA");

        var redaction = RedactionWorkspace.Create(fixture.Path);
        redaction.Stage(new RedactionPlan(0, new[] { new RedactionTarget(target) }));
        var images = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var text = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);

        new PdfEditWriter().SaveAsCopy(
            images,
            text,
            redaction,
            destination,
            warningsConfirmed: true);

        Assert.True(File.Exists(destination));
        Assert.Equal(sourceBytes, File.ReadAllBytes(fixture.Path));

        using (var saved = PdfDocumentSession.Open(destination))
        {
            var texts = saved.GetTextObjects(0).Select(item => item.Text).ToArray();
            Assert.DoesNotContain("CASA", texts);
            Assert.Contains("UNO", texts);

            var extracted = saved.GetTextRange(0, 0, 100);
            Assert.DoesNotContain("CASA", extracted, StringComparison.Ordinal);
            Assert.Contains("UNO", extracted, StringComparison.Ordinal);

            var rendered = saved.RenderPage(0, 36d);
            Assert.True(rendered.PixelWidth > 0);
            Assert.True(rendered.PixelHeight > 0);
            Assert.NotEmpty(rendered.Pixels);
        }

        AssertBlackRedactionPath(destination, target.Bounds);
    }

    [Fact]
    public void SaveAsCopy_RedactionWithChangedSnapshot_RejectsBeforePublishingDestination()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateTwoTextObjects();
        var destination = Path.Combine(fixture.DirectoryPath, "should-not-exist.pdf");

        PdfTextObjectInfo target;
        using (var session = PdfDocumentSession.Open(fixture.Path))
            target = session.GetTextObjects(0).Single(item => item.Text == "CASA");

        var redaction = RedactionWorkspace.Create(fixture.Path);
        redaction.Stage(new RedactionPlan(
            0,
            new[] { new RedactionTarget(target with { Text = "CAMBIADO" }) }));
        var images = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var text = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);

        var error = Capture(() =>
            new PdfEditWriter().SaveAsCopy(
                images,
                text,
                redaction,
                destination,
                warningsConfirmed: true));

        var invalid = Assert.IsType<InvalidOperationException>(error);
        Assert.Contains("snapshot", invalid.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(destination));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.sgpdf.tmp"));
    }

    [Fact]
    public void SaveAsCopy_Redaction_WhenSignatureExists_RemainsBlocked()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateTwoTextObjects();
        var destination = Path.Combine(fixture.DirectoryPath, "signed-blocked.pdf");

        PdfTextObjectInfo target;
        using (var session = PdfDocumentSession.Open(fixture.Path))
            target = session.GetTextObjects(0).Single(item => item.Text == "CASA");

        var redaction = RedactionWorkspace.Create(fixture.Path);
        redaction.Stage(new RedactionPlan(0, new[] { new RedactionTarget(target) }));
        var images = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var text = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var writer = new PdfEditWriter(signatureCountOverride: _ => 1);

        var error = Capture(() =>
            writer.SaveAsCopy(images, text, redaction, destination, warningsConfirmed: true));

        var invalid = Assert.IsType<InvalidOperationException>(error);
        Assert.Contains("firma criptográfica", invalid.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(destination));
    }

    private static void AssertBlackRedactionPath(string path, PdfObjectBounds expectedBounds)
    {
        PdfiumRuntime.EnsureInitialized();
        PdfiumRuntime.NativeGate.Wait();

        IntPtr document = IntPtr.Zero;
        IntPtr page = IntPtr.Zero;
        try
        {
            document = PdfiumNative.FPDF_LoadDocument(path, null);
            Assert.NotEqual(IntPtr.Zero, document);
            page = PdfiumNative.FPDF_LoadPage(document, 0);
            Assert.NotEqual(IntPtr.Zero, page);

            var matches = new List<IntPtr>();
            var count = PdfiumNative.FPDFPage_CountObjects(page);
            Assert.True(count > 0);
            for (var index = 0; index < count; index++)
            {
                var pageObject = PdfiumNative.FPDFPage_GetObject(page, index);
                if (pageObject == IntPtr.Zero ||
                    PdfiumNative.FPDFPageObj_GetType(pageObject) != PdfiumNative.FPDF_PAGEOBJ_PATH ||
                    PdfiumNative.FPDFPageObj_GetBounds(
                        pageObject,
                        out var left,
                        out var bottom,
                        out var right,
                        out var top) == 0)
                {
                    continue;
                }

                if (NearlyEqual(left, expectedBounds.Left) &&
                    NearlyEqual(bottom, expectedBounds.Bottom) &&
                    NearlyEqual(right, expectedBounds.Right) &&
                    NearlyEqual(top, expectedBounds.Top))
                {
                    matches.Add(pageObject);
                }
            }

            var rectangle = Assert.Single(matches);
            Assert.NotEqual(IntPtr.Zero, rectangle);
            Assert.NotEqual(0, PdfiumNative.FPDFPageObj_GetFillColor(
                rectangle,
                out var red,
                out var green,
                out var blue,
                out var alpha));
            Assert.Equal(0u, red);
            Assert.Equal(0u, green);
            Assert.Equal(0u, blue);
            Assert.Equal(255u, alpha);

            Assert.NotEqual(0, PdfiumNative.FPDFPath_GetDrawMode(
                rectangle,
                out var fillMode,
                out var stroke));
            Assert.Equal(PdfiumNative.FPDF_FILLMODE_WINDING, fillMode);
            Assert.Equal(0, stroke);
        }
        finally
        {
            if (page != IntPtr.Zero)
                PdfiumNative.FPDF_ClosePage(page);
            if (document != IntPtr.Zero)
                PdfiumNative.FPDF_CloseDocument(document);
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static bool NearlyEqual(double left, double right)
        => double.IsFinite(left) && double.IsFinite(right) && Math.Abs(left - right) <= 0.10d;

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
