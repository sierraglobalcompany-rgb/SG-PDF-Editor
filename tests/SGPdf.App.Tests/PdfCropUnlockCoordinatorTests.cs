using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using SGPdf.App.Features.Labels;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

[Collection(PdfSharpFontGateCollection.Name)]
public sealed class PdfCropUnlockCoordinatorTests
{
    private const string SyntheticPassword = "SGPDF_F15C_SYNTHETIC_ONLY_8D1A63";
    private const string SyntheticOwnerPassword = "SGPDF_F15C_SYNTHETIC_OWNER_2B4F90";
    private static readonly string QpdfPath = Path.Combine(AppContext.BaseDirectory, "qpdf", "qpdf.exe");

    [Fact]
    public async Task UnlockCopy_ValidAes256Password_PreservesSourceAndCreatesUnencryptedCopy()
    {
        using var fixture = CreateVisibleFixture();
        var encrypted = fixture.PathFor("encrypted.pdf");
        await EncryptAsync(fixture.SourcePath, encrypted);
        var destination = fixture.PathFor("unlocked.pdf");
        var sourceHash = await HashAsync(encrypted);

        var result = await new PdfUnlockCoordinator().CreateUnlockedCopyAsync(
            encrypted, destination, SyntheticPassword, authorizationConfirmed: true, CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(sourceHash, await HashAsync(encrypted));
        Assert.Equal(2, await IsEncryptedExitCodeAsync(destination));
        using var output = PdfDocumentSession.Open(destination);
        Assert.Equal(2, output.PageCount);
        Assert.Contains("CENTER_PAGE_1", output.GetPageText(0));
        Assert.Contains("CENTER_PAGE_2", output.GetPageText(1));
    }

    [Fact]
    public async Task UnlockCopy_WrongPasswordOrMissingAuthorization_CreatesNoOutput()
    {
        using var fixture = CreateVisibleFixture();
        var encrypted = fixture.PathFor("encrypted.pdf");
        await EncryptAsync(fixture.SourcePath, encrypted);
        var sourceHash = await HashAsync(encrypted);
        var destination = fixture.PathFor("never-created.pdf");

        var wrong = await new PdfUnlockCoordinator().CreateUnlockedCopyAsync(
            encrypted, destination, "wrong-synthetic", authorizationConfirmed: true, CancellationToken.None);
        var unauthorized = await new PdfUnlockCoordinator().CreateUnlockedCopyAsync(
            encrypted, destination, SyntheticPassword, authorizationConfirmed: false, CancellationToken.None);

        Assert.False(wrong.Succeeded);
        Assert.Contains("contraseña", wrong.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(unauthorized.Succeeded);
        Assert.Contains("autoriz", unauthorized.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(destination));
        Assert.Equal(sourceHash, await HashAsync(encrypted));
        AssertNoOperationTemps(fixture.DirectoryPath);
    }

    [Fact]
    public async Task UnlockCopy_UnencryptedCollisionSamePathAndCancellation_CreateNoOutputOrOverwrite()
    {
        using var fixture = CreateVisibleFixture();
        var collision = fixture.PathFor("existing.pdf");
        File.WriteAllText(collision, "KEEP", Encoding.UTF8);

        var notEncrypted = await new PdfUnlockCoordinator().CreateUnlockedCopyAsync(
            fixture.SourcePath, fixture.PathFor("unencrypted-out.pdf"), SyntheticPassword, true, CancellationToken.None);
        var collisionResult = await new PdfUnlockCoordinator().CreateUnlockedCopyAsync(
            fixture.SourcePath, collision, SyntheticPassword, true, CancellationToken.None);
        var samePath = await new PdfUnlockCoordinator().CreateUnlockedCopyAsync(
            fixture.SourcePath, fixture.SourcePath, SyntheticPassword, true, CancellationToken.None);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new PdfUnlockCoordinator()
            .CreateUnlockedCopyAsync(fixture.SourcePath, fixture.PathFor("canceled.pdf"), SyntheticPassword, true, canceled.Token));

        Assert.False(notEncrypted.Succeeded);
        Assert.Contains("cifrado", notEncrypted.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(collisionResult.Succeeded);
        Assert.False(samePath.Succeeded);
        Assert.Equal("KEEP", File.ReadAllText(collision));
        AssertNoOperationTemps(fixture.DirectoryPath);
    }

    [Fact]
    public async Task UnlockCopy_SignedEncryptedPdfAndSignatureReadError_AreRejected()
    {
        using var fixture = CreateVisibleFixture();
        var signed = fixture.PathFor("signed.pdf");
        using (var certificate = CreateCertificate())
            InvokeSignatureWriter(fixture.SourcePath, signed, certificate);
        using (var signedSession = PdfDocumentSession.Open(signed))
            Assert.Equal(1, signedSession.GetCryptographicSignatureCount());

        var signedEncrypted = fixture.PathFor("signed-encrypted.pdf");
        await EncryptAsync(signed, signedEncrypted);
        using (var encryptedSession = PdfDocumentSession.Open(signedEncrypted, SyntheticPassword))
            Assert.Equal(1, encryptedSession.GetCryptographicSignatureCount());
        var signedHash = await HashAsync(signedEncrypted);
        var signedOutput = fixture.PathFor("signed-unlocked.pdf");
        var signedResult = await new PdfUnlockCoordinator().CreateUnlockedCopyAsync(
            signedEncrypted, signedOutput, SyntheticPassword, true, CancellationToken.None);

        var readErrorOutput = fixture.PathFor("signature-read-error.pdf");
        var readErrorCoordinator = new PdfUnlockCoordinator(
            QpdfPath,
            ChildProcessRunner.RunAsync,
            static _ => throw new InvalidOperationException("synthetic signature read failure"));
        var readError = await readErrorCoordinator.CreateUnlockedCopyAsync(
            fixture.SourcePath, readErrorOutput, SyntheticPassword, true, CancellationToken.None);

        Assert.False(signedResult.Succeeded);
        Assert.False(File.Exists(signedOutput));
        Assert.Equal(signedHash, await HashAsync(signedEncrypted));
        Assert.False(readError.Succeeded);
        Assert.False(File.Exists(readErrorOutput));
        AssertNoOperationTemps(fixture.DirectoryPath);
    }

    [Fact]
    public async Task CropCopy_TwoPagePortraitAndLandscape_UpdatesCropBoxAndKeepsUnderlyingText()
    {
        using var fixture = CreateVisibleFixture();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);
        var destination = fixture.PathFor("cropped.pdf");
        var sourceHash = await HashAsync(fixture.SourcePath);
        var margins = new PdfCropMargins(10d, 20d, 5d, 15d);
        var result = await new PdfCropCoordinator().CreateCroppedCopyAsync(
            session, destination, margins, CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(sourceHash, await HashAsync(fixture.SourcePath));
        using var output = PdfDocumentSession.Open(destination);
        Assert.Equal(2, output.PageCount);
        Assert.Contains("CENTER_PAGE_1", output.GetPageText(0));
        Assert.Contains("CENTER_PAGE_2", output.GetPageText(1));
        Assert.Contains("LEFT_EDGE_PAGE_1", output.GetPageText(0));
        Assert.Contains("TOP_EDGE_PAGE_2", output.GetPageText(1));
        AssertCropBox(destination, pageIndex: 0, expectedWidth: 300d - MmToPoints(30d), expectedHeight: 400d - MmToPoints(20d));
        AssertCropBox(destination, pageIndex: 1, expectedWidth: 500d - MmToPoints(30d), expectedHeight: 300d - MmToPoints(20d));

        var rendered = output.RenderPage(0, 72d);
        Assert.True(IsNearWhite(Sample(rendered, xPoints: 2d, yPoints: 2d, pageHeightPoints: 400d - MmToPoints(20d))));
    }

    [Fact]
    public async Task CropCopy_InvalidMarginsRotationFormsAnnotationsAndSignatures_ProduceNoOutput()
    {
        using var fixture = CreateVisibleFixture();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);
        var sourceHash = await HashAsync(fixture.SourcePath);

        var negative = await TryCropAsync(session, fixture.PathFor("negative.pdf"), new(-1, 0, 0, 0));
        var nonFinite = await TryCropAsync(session, fixture.PathFor("nonfinite.pdf"), new(double.NaN, 0, 0, 0));
        var tooLarge = await TryCropAsync(session, fixture.PathFor("too-small.pdf"), new(200, 200, 0, 0));
        Assert.False(negative.Succeeded);
        Assert.False(nonFinite.Succeeded);
        Assert.False(tooLarge.Succeeded);

        using var rotated = CreateOnePageFixture("rotated.pdf", 300d, 400d, rotate: 90);
        using (var rotatedSession = PdfDocumentSession.Open(rotated.SourcePath))
        {
            var result = await TryCropAsync(rotatedSession, rotated.PathFor("rotated-out.pdf"), new(5, 5, 5, 5));
            Assert.False(result.Succeeded);
        }

        using (var formFixture = OrganizePdfFixtureFactory.CreateAcroForm())
        using (var formSession = PdfDocumentSession.Open(formFixture.Path))
        {
            var result = await TryCropAsync(formSession, Path.Combine(formFixture.DirectoryPath, "form-out.pdf"), new(5, 5, 5, 5));
            Assert.False(result.Succeeded);
            Assert.False(File.Exists(Path.Combine(formFixture.DirectoryPath, "form-out.pdf")));
        }

        using (var annotationFixture = OrganizePdfFixtureFactory.CreateNavigation())
        using (var annotationSession = PdfDocumentSession.Open(annotationFixture.Path))
        {
            var result = await TryCropAsync(annotationSession, Path.Combine(annotationFixture.DirectoryPath, "annotation-out.pdf"), new(5, 5, 5, 5));
            Assert.False(result.Succeeded);
            Assert.False(File.Exists(Path.Combine(annotationFixture.DirectoryPath, "annotation-out.pdf")));
        }

        var signed = fixture.PathFor("signed-crop.pdf");
        using (var certificate = CreateCertificate())
            InvokeSignatureWriter(fixture.SourcePath, signed, certificate);
        using (var signedSession = PdfDocumentSession.Open(signed))
        {
            var result = await TryCropAsync(signedSession, fixture.PathFor("signed-crop-out.pdf"), new(5, 5, 5, 5));
            Assert.False(result.Succeeded);
        }

        Assert.Equal(sourceHash, await HashAsync(fixture.SourcePath));
        AssertNoOperationTemps(fixture.DirectoryPath);
        Assert.False(File.Exists(fixture.PathFor("signed-crop-out.pdf")));
    }

    [Fact]
    public async Task CropCopy_CancelAndDestinationCollision_AreNoOp()
    {
        using var fixture = CreateVisibleFixture();
        using var session = PdfDocumentSession.Open(fixture.SourcePath);
        var sourceHash = await HashAsync(fixture.SourcePath);
        var destination = fixture.PathFor("existing-crop.pdf");
        File.WriteAllText(destination, "KEEP", Encoding.UTF8);
        var collision = await TryCropAsync(session, destination, new(5, 5, 5, 5));
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new PdfCropCoordinator()
            .CreateCroppedCopyAsync(session, fixture.PathFor("canceled-crop.pdf"), new(5, 5, 5, 5), canceled.Token));

        Assert.False(collision.Succeeded);
        Assert.Equal("KEEP", File.ReadAllText(destination));
        Assert.Equal(sourceHash, await HashAsync(fixture.SourcePath));
        AssertNoOperationTemps(fixture.DirectoryPath);
    }

    private static async Task<PdfCropResult> TryCropAsync(
        PdfDocumentSession session,
        string destination,
        PdfCropMargins margins)
        => await new PdfCropCoordinator().CreateCroppedCopyAsync(
            session, destination, margins, CancellationToken.None);

    private static async Task EncryptAsync(string source, string destination)
    {
        var startInfo = new ProcessStartInfo(QpdfPath);
        startInfo.ArgumentList.Add("--encrypt");
        startInfo.ArgumentList.Add(SyntheticPassword);
        startInfo.ArgumentList.Add(SyntheticOwnerPassword);
        startInfo.ArgumentList.Add("256");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add(source);
        startInfo.ArgumentList.Add(destination);
        var result = await ChildProcessRunner.RunAsync(startInfo, TimeSpan.FromSeconds(20), CancellationToken.None);
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(destination));
    }

    private static async Task<int> IsEncryptedExitCodeAsync(string path)
    {
        var startInfo = new ProcessStartInfo(QpdfPath);
        startInfo.ArgumentList.Add("--is-encrypted");
        startInfo.ArgumentList.Add(path);
        return (await ChildProcessRunner.RunAsync(startInfo, TimeSpan.FromSeconds(10), CancellationToken.None)).ExitCode;
    }

    private static TestPdfFixture CreateVisibleFixture()
    {
        EnablePdfSharpFontResolver();
        var fixture = TestPdfFixture.Create("crop-unlock");
        var source = fixture.SourcePath;
        using var document = new PdfDocument();
        document.Info.Title = "SGPDF F15C synthetic metadata";
        document.Info.Author = "Synthetic QA";
        AddVisiblePage(document, 300d, 400d, 1);
        AddVisiblePage(document, 500d, 300d, 2);
        document.Save(source);
        return fixture;
    }

    private static TestPdfFixture CreateOnePageFixture(string prefix, double width, double height, int rotate = 0)
    {
        EnablePdfSharpFontResolver();
        var fixture = TestPdfFixture.Create(prefix);
        using var document = new PdfDocument();
        AddVisiblePage(document, width, height, 1);
        document.Pages[0].Rotate = rotate;
        document.Save(fixture.SourcePath);
        return fixture;
    }

    private static void AddVisiblePage(PdfDocument document, double width, double height, int pageNumber)
    {
        var page = document.AddPage();
        page.Width = XUnit.FromPoint(width);
        page.Height = XUnit.FromPoint(height);
        using var graphics = XGraphics.FromPdfPage(page);
        graphics.DrawRectangle(new XPen(XColors.Black, 2d), 2d, 2d, width - 4d, height - 4d);
        var font = new XFont("Arial", 11d);
        graphics.DrawString($"LEFT_EDGE_PAGE_{pageNumber}", font, XBrushes.Black, new XPoint(6d, 22d));
        graphics.DrawString($"TOP_EDGE_PAGE_{pageNumber}", font, XBrushes.Black, new XPoint(width / 2d, 9d));
        graphics.DrawString($"CENTER_PAGE_{pageNumber}", font, XBrushes.Black, new XPoint(width / 2d, height / 2d));
    }

    private static void AssertCropBox(string path, int pageIndex, double expectedWidth, double expectedHeight)
    {
        using var document = PdfSharp.Pdf.IO.PdfReader.Open(path, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        var crop = document.Pages[pageIndex].EffectiveCropBoxReadOnly;
        Assert.InRange(Math.Abs(crop.Width - expectedWidth), 0d, 0.5d);
        Assert.InRange(Math.Abs(crop.Height - expectedHeight), 0d, 0.5d);
    }

    private static (byte B, byte G, byte R) Sample(PdfRenderedPage page, double xPoints, double yPoints, double pageHeightPoints)
    {
        var x = Math.Clamp((int)Math.Round(xPoints * page.Dpi / 72d), 0, page.PixelWidth - 1);
        var y = Math.Clamp((int)Math.Round((pageHeightPoints - yPoints) * page.Dpi / 72d), 0, page.PixelHeight - 1);
        var offset = (y * page.Stride) + (x * 4);
        return (page.Pixels[offset], page.Pixels[offset + 1], page.Pixels[offset + 2]);
    }

    private static bool IsNearWhite((byte B, byte G, byte R) color)
        => color.B > 220 && color.G > 220 && color.R > 220;

    private static double MmToPoints(double mm) => mm * 72d / 25.4d;

    private static async Task<byte[]> HashAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return await SHA256.HashDataAsync(stream);
    }

    private static void InvokeSignatureWriter(string sourcePath, string destinationPath, X509Certificate2 certificate)
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType("SGPdf.App.Features.Signing.PdfCryptographicSignatureWriter");
        Assert.NotNull(type);
        var method = type!.GetMethod("Write", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);
        try
        {
            method!.Invoke(null, [sourcePath, destinationPath, certificate, CancellationToken.None]);
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=SGPDF F15C Synthetic QA", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    private static void EnablePdfSharpFontResolver()
    {
        if (GlobalFontSettings.FontResolver is null)
            GlobalFontSettings.FontResolver = new SyntheticFontResolver();
    }

    private static void AssertNoOperationTemps(string directory)
    {
        Assert.Empty(Directory.GetFiles(directory, ".sgpdf-unlock-*.pdf"));
        Assert.Empty(Directory.GetFiles(directory, ".sgpdf-crop-*.pdf"));
    }

    private sealed class TestPdfFixture : IDisposable
    {
        private TestPdfFixture(string directoryPath, string sourcePath)
        {
            DirectoryPath = directoryPath;
            SourcePath = sourcePath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }
        internal string PathFor(string name) => Path.Combine(DirectoryPath, name);

        internal static TestPdfFixture Create(string name)
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f15c-{name}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            return new TestPdfFixture(directory, Path.Combine(directory, "source.pdf"));
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }

    private sealed class SyntheticFontResolver : IFontResolver
    {
        private const string FaceName = "SGPDF_F15C_SYNTHETIC_FONT";
        private readonly byte[] _fontBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fonts", "DejaVuSans.ttf"));

        public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
            => new(FaceName, bold, italic);

        public byte[]? GetFont(string faceName)
            => string.Equals(faceName, FaceName, StringComparison.Ordinal) ? _fontBytes : null;
    }
}
