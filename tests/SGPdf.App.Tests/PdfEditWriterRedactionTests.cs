using System.Text;
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

    [Fact]
    public void SaveAsCopy_ManuallyStagedRotatedText_FailsClosedAtTransactionBoundary()
    {
        using var fixture = RotatedTextFixture.Create();
        var destination = Path.Combine(fixture.DirectoryPath, "rotated-redacted.pdf");

        PdfTextObjectInfo target;
        using (var session = PdfDocumentSession.Open(fixture.Path))
            target = Assert.Single(session.GetTextObjects(0));

        var redaction = RedactionWorkspace.Create(fixture.Path);
        redaction.Stage(new RedactionPlan(0, new[] { new RedactionTarget(target) }));
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
        Assert.Contains("horizontal", invalid.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(destination));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.sgpdf.tmp"));
    }

    [Fact]
    public void SaveAsCopy_RedactionWithPreCanceledToken_PublishesNothing()
    {
        using var fixture = PdfEditWriterTextFixtureFactory.CreateTwoTextObjects();
        var destination = Path.Combine(fixture.DirectoryPath, "cancelled.pdf");

        PdfTextObjectInfo target;
        using (var session = PdfDocumentSession.Open(fixture.Path))
            target = session.GetTextObjects(0).Single(item => item.Text == "CASA");

        var redaction = RedactionWorkspace.Create(fixture.Path);
        redaction.Stage(new RedactionPlan(0, new[] { new RedactionTarget(target) }));
        var images = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var text = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var error = Capture(() =>
            new PdfEditWriter().SaveAsCopy(
                images,
                text,
                redaction,
                destination,
                warningsConfirmed: true,
                cancellation.Token));

        Assert.IsType<OperationCanceledException>(error);
        Assert.False(File.Exists(destination));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.sgpdf.tmp"));
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

    private sealed class RotatedTextFixture : IDisposable
    {
        private RotatedTextFixture(string directoryPath, string path)
        {
            DirectoryPath = directoryPath;
            Path = path;
        }

        internal string DirectoryPath { get; }
        internal string Path { get; }

        internal static RotatedTextFixture Create()
        {
            var directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"sgpdf-f12-rotated-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var path = System.IO.Path.Combine(directory, "rotated.pdf");
            Write(path);
            return new RotatedTextFixture(directory, path);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }

        private static void Write(string path)
        {
            const string content = "BT /F1 14 Tf 0 1 -1 0 100 500 Tm (Rotado) Tj ET";
            var stream = $"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}\nendstream";
            var objects = new Dictionary<int, string>
            {
                [1] = "<< /Type /Catalog /Pages 2 0 R >>",
                [2] = "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                [3] = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
                [4] = stream,
                [5] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
            };

            var builder = new StringBuilder("%PDF-1.7\n%SGPDF-F12-ROTATED\n");
            var offsets = new long[6];
            for (var number = 1; number <= 5; number++)
            {
                offsets[number] = Encoding.Latin1.GetByteCount(builder.ToString());
                builder.Append(number).Append(" 0 obj\n")
                    .Append(objects[number]).Append("\nendobj\n");
            }

            var xrefOffset = Encoding.Latin1.GetByteCount(builder.ToString());
            builder.Append("xref\n0 6\n")
                .Append("0000000000 65535 f \n");
            for (var number = 1; number <= 5; number++)
                builder.Append(offsets[number].ToString("D10")).Append(" 00000 n \n");

            builder.Append("trailer\n<< /Size 6 /Root 1 0 R >>\n")
                .Append("startxref\n").Append(xrefOffset).Append("\n%%EOF\n");
            File.WriteAllBytes(path, Encoding.Latin1.GetBytes(builder.ToString()));
        }
    }
}
