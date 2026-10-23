using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using PdfSharp.Pdf;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Features.Signing;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

[Collection(PdfiumNativeGateCollection.Name)]
public sealed class PdfCompressionWorkflowTests
{
    [Fact]
    public async Task CompressCopy_CompressiblePdf_PublishesSmallerCopyAndPreservesSourceAndMetadata()
    {
        using var fixture = CompressionFixture.Create(repetitiveMetadata: true);
        var sourceHash = Hash(fixture.SourcePath);
        var destination = fixture.Path("compressed.pdf");
        using var source = PdfDocumentSession.Open(fixture.SourcePath);
        var pageCount = source.PageCount;

        var result = await new PdfCompressionCoordinator().CompressCopyAsync(
            source,
            destination,
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.False(result.NoReduction);
        Assert.True(File.Exists(destination));
        Assert.True(new FileInfo(destination).Length < new FileInfo(fixture.SourcePath).Length);
        Assert.Equal(sourceHash, Hash(fixture.SourcePath));
        using var output = PdfDocumentSession.Open(destination);
        Assert.Equal(pageCount, output.PageCount);
        Assert.Equal(fixture.Title, output.GetOrganizeMetadataText("Title"));
        Assert.Empty(TemporaryFiles(fixture.DirectoryPath));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.sgpdf-qpdf-job-*.json"));
    }

    [Fact]
    public async Task CompressCopy_NoReduction_DoesNotPublishAndCleansTemporaryPdf()
    {
        using var fixture = CompressionFixture.Create(repetitiveMetadata: true);
        var destination = fixture.Path("unchanged.pdf");
        using var source = PdfDocumentSession.Open(fixture.SourcePath);
        var calls = 0;
        var coordinator = new PdfCompressionCoordinator((sourcePath, temporaryPath, token) =>
        {
            calls++;
            token.ThrowIfCancellationRequested();
            File.Copy(sourcePath, temporaryPath);
            var length = new FileInfo(sourcePath).Length;
            return Task.FromResult(new PdfOptimizeCapabilityResult(true, length, length, false, null));
        });

        var result = await coordinator.CompressCopyAsync(source, destination, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.True(result.NoReduction);
        Assert.Contains("reducción", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, calls);
        Assert.False(File.Exists(destination));
        Assert.Empty(TemporaryFiles(fixture.DirectoryPath));
    }

    [Fact]
    public async Task CompressCopy_ExistingDestination_PreservesItAndDoesNotRunQpdf()
    {
        using var fixture = CompressionFixture.Create(repetitiveMetadata: true);
        var destination = fixture.Path("occupied.pdf");
        var existing = Encoding.ASCII.GetBytes("pre-existing synthetic destination");
        File.WriteAllBytes(destination, existing);
        using var source = PdfDocumentSession.Open(fixture.SourcePath);
        var coordinator = new PdfCompressionCoordinator((_, _, _) =>
            Task.FromException<PdfOptimizeCapabilityResult>(new Xunit.Sdk.XunitException("qpdf must not run")));

        var result = await coordinator.CompressCopyAsync(source, destination, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(existing, File.ReadAllBytes(destination));
        Assert.Empty(TemporaryFiles(fixture.DirectoryPath));
    }

    [Fact]
    public async Task CompressCopy_SameSourceAndDestination_IsRejectedWithoutCallingQpdf()
    {
        using var fixture = CompressionFixture.Create(repetitiveMetadata: true);
        var sourceHash = Hash(fixture.SourcePath);
        using var source = PdfDocumentSession.Open(fixture.SourcePath);
        var coordinator = new PdfCompressionCoordinator((_, _, _) =>
            Task.FromException<PdfOptimizeCapabilityResult>(new Xunit.Sdk.XunitException("qpdf must not run")));

        var result = await coordinator.CompressCopyAsync(source, fixture.SourcePath, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(sourceHash, Hash(fixture.SourcePath));
        Assert.Empty(TemporaryFiles(fixture.DirectoryPath));
    }

    [Fact]
    public async Task CompressCopy_SignedPdf_IsRejectedBeforeQpdf()
    {
        using var fixture = CompressionFixture.Create(repetitiveMetadata: false);
        var signedPath = fixture.Path("signed.pdf");
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=SGPDF F15A synthetic",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(1));
        PdfCryptographicSignatureWriter.Write(fixture.SourcePath, signedPath, certificate);
        var destination = fixture.Path("signed-compressed.pdf");
        using var source = PdfDocumentSession.Open(signedPath);
        Assert.Equal(1, source.GetCryptographicSignatureCount());
        var optimizerCalls = 0;
        var coordinator = new PdfCompressionCoordinator((_, _, _) =>
        {
            optimizerCalls++;
            return Task.FromException<PdfOptimizeCapabilityResult>(new Xunit.Sdk.XunitException("qpdf must not run"));
        });

        var result = await coordinator.CompressCopyAsync(source, destination, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(0, optimizerCalls);
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public async Task CompressCopy_PasswordProtectedPdfOpenedWithPassword_IsRejectedBeforeQpdf()
    {
        using var fixture = CompressionFixture.Create(repetitiveMetadata: false);
        var protectedPath = fixture.Path("protected.pdf");
        await PdfProtectionCapabilityAdapter.CreateProtectedCopyAsync(
            fixture.SourcePath,
            protectedPath,
            "synthetic-user",
            "synthetic-owner",
            CancellationToken.None);
        using var source = PdfDocumentSession.Open(protectedPath, "synthetic-user");
        Assert.True(source.OpenedWithPassword);
        var destination = fixture.Path("protected-compressed.pdf");
        var optimizerCalls = 0;
        var coordinator = new PdfCompressionCoordinator((_, _, _) =>
        {
            optimizerCalls++;
            return Task.FromException<PdfOptimizeCapabilityResult>(new Xunit.Sdk.XunitException("qpdf must not run"));
        });

        var result = await coordinator.CompressCopyAsync(source, destination, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(0, optimizerCalls);
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public async Task CompressCopy_SourceFingerprintChangesBeforePublish_AbortsAndCleansTemporaryPdf()
    {
        using var fixture = CompressionFixture.Create(repetitiveMetadata: true);
        var sourceHash = Hash(fixture.SourcePath);
        var destination = fixture.Path("source-changed.pdf");
        using var source = PdfDocumentSession.Open(fixture.SourcePath);
        var coordinator = new PdfCompressionCoordinator(async (sourcePath, temporaryPath, token) =>
        {
            var optimized = await PdfOptimizeRepairCapabilityAdapter.OptimizeCopyAsync(
                sourcePath,
                temporaryPath,
                token);
            File.SetLastWriteTimeUtc(sourcePath, File.GetLastWriteTimeUtc(sourcePath).AddMinutes(-10));
            return optimized;
        });

        var result = await coordinator.CompressCopyAsync(source, destination, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("cambió", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(sourceHash, Hash(fixture.SourcePath));
        Assert.False(File.Exists(destination));
        Assert.Empty(TemporaryFiles(fixture.DirectoryPath));
    }

    [Fact]
    public async Task CompressCopy_OptimizerTimeout_DoesNotPublishOrLeakTemporaryFiles()
    {
        using var fixture = CompressionFixture.Create(repetitiveMetadata: true);
        var destination = fixture.Path("timeout.pdf");
        using var source = PdfDocumentSession.Open(fixture.SourcePath);
        var coordinator = new PdfCompressionCoordinator((_, _, _) =>
            Task.FromException<PdfOptimizeCapabilityResult>(new TimeoutException("synthetic timeout")));

        var result = await coordinator.CompressCopyAsync(source, destination, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("timeout", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(destination));
        Assert.Empty(TemporaryFiles(fixture.DirectoryPath));
    }

    [Fact]
    public async Task CompressCopy_UnreadableOptimizedOutput_IsNotPublished()
    {
        using var fixture = CompressionFixture.Create(repetitiveMetadata: true);
        var destination = fixture.Path("invalid-output.pdf");
        using var source = PdfDocumentSession.Open(fixture.SourcePath);
        var coordinator = new PdfCompressionCoordinator(async (sourcePath, temporaryPath, token) =>
        {
            token.ThrowIfCancellationRequested();
            await File.WriteAllTextAsync(temporaryPath, "not a PDF", token);
            return new PdfOptimizeCapabilityResult(
                true,
                new FileInfo(sourcePath).Length,
                new FileInfo(temporaryPath).Length,
                false,
                null);
        });

        var result = await coordinator.CompressCopyAsync(source, destination, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.False(File.Exists(destination));
        Assert.Empty(TemporaryFiles(fixture.DirectoryPath));
    }

    [Fact]
    public async Task CompressCopy_PreCancelled_DoesNotInvokeOptimizerOrLeaveFiles()
    {
        using var fixture = CompressionFixture.Create(repetitiveMetadata: true);
        var destination = fixture.Path("cancelled.pdf");
        using var source = PdfDocumentSession.Open(fixture.SourcePath);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var calls = 0;
        var coordinator = new PdfCompressionCoordinator((_, _, _) =>
        {
            calls++;
            return Task.FromResult(new PdfOptimizeCapabilityResult(false, 0, 0, false, null));
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await coordinator.CompressCopyAsync(source, destination, cts.Token));

        Assert.Equal(0, calls);
        Assert.False(File.Exists(destination));
        Assert.Empty(TemporaryFiles(fixture.DirectoryPath));
    }

    private static string Hash(string path)
        => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static string[] TemporaryFiles(string directory)
        => Directory.GetFiles(directory, ".sgpdf-compress-*.pdf");

    private sealed class CompressionFixture : IDisposable
    {
        private CompressionFixture(string directoryPath, string sourcePath, string title)
        {
            DirectoryPath = directoryPath;
            SourcePath = sourcePath;
            Title = title;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }
        internal string Title { get; }
        internal string Path(string name) => System.IO.Path.Combine(DirectoryPath, name);

        internal static CompressionFixture Create(bool repetitiveMetadata)
        {
            var directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"sgpdf-f15a-compress-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = System.IO.Path.Combine(directory, "source.pdf");
            var title = repetitiveMetadata ? new string('R', 48_000) : "SGPDF F15A synthetic source";
            using (var document = new PdfDocument())
            {
                document.Info.Title = title;
                document.AddPage();
                document.Save(source);
            }

            return new CompressionFixture(directory, source, title);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
