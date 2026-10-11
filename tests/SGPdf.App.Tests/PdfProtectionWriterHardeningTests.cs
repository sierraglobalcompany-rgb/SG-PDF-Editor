using PdfSharp.Pdf;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

[Collection(PdfiumNativeGateCollection.Name)]
public sealed class PdfProtectionWriterHardeningTests
{
    [Fact]
    public async Task SignedSource_BlocksBeforeQpdfIsInvoked()
    {
        using var fixture = ProtectionHardeningFixture.Create();
        var destination = Path.Combine(fixture.DirectoryPath, "signed-blocked.pdf");
        var protectCalled = false;
        var writer = new PdfProtectionWriter(
            protectCopy: (_, _, _, _, _, _) =>
            {
                protectCalled = true;
                return Task.CompletedTask;
            },
            validator: (_, _, _, _) => PdfProtectionValidationResult.Success,
            signatureCountReader: (_, _) => 1);

        var result = await writer.SaveAsProtectedCopyAsync(
            fixture.SourcePath,
            PdfEditSourceFingerprint.Capture(fixture.SourcePath),
            new PdfProtectionSettings("new-secret", null, true),
            destination,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("firma", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(protectCalled);
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public async Task ValidatorFailure_PreservesExistingDestinationAndCleansSiblingTemp()
    {
        using var fixture = ProtectionHardeningFixture.Create();
        var destination = Path.Combine(fixture.DirectoryPath, "existing.pdf");
        var sentinel = new byte[] { 7, 4, 1, 0 };
        File.WriteAllBytes(destination, sentinel);
        var writer = new PdfProtectionWriter(
            protectCopy: (source, temporary, _, _, _, _) =>
            {
                File.Copy(source, temporary);
                return Task.CompletedTask;
            },
            validator: (_, _, _, _) => PdfProtectionValidationResult.Failure("validator forced failure"),
            signatureCountReader: (_, _) => 0);

        var result = await writer.SaveAsProtectedCopyAsync(
            fixture.SourcePath,
            PdfEditSourceFingerprint.Capture(fixture.SourcePath),
            new PdfProtectionSettings("new-secret", null, true),
            destination,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("validator forced failure", result.Error, StringComparison.Ordinal);
        Assert.Equal(sentinel, File.ReadAllBytes(destination));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.sgpdf-tmp.pdf"));
    }

    [Fact]
    public async Task CancellationAfterTemporaryCreation_PreservesExistingDestinationAndCleansTemp()
    {
        using var fixture = ProtectionHardeningFixture.Create();
        var destination = Path.Combine(fixture.DirectoryPath, "existing.pdf");
        var sentinel = new byte[] { 2, 0, 2, 6 };
        File.WriteAllBytes(destination, sentinel);
        var writer = new PdfProtectionWriter(
            protectCopy: (source, temporary, _, _, _, cancellationToken) =>
            {
                File.Copy(source, temporary);
                return Task.FromException(new OperationCanceledException(cancellationToken));
            },
            validator: (_, _, _, _) => PdfProtectionValidationResult.Success,
            signatureCountReader: (_, _) => 0);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            writer.SaveAsProtectedCopyAsync(
                fixture.SourcePath,
                PdfEditSourceFingerprint.Capture(fixture.SourcePath),
                new PdfProtectionSettings("new-secret", null, true),
                destination,
                CancellationToken.None));

        Assert.Equal(sentinel, File.ReadAllBytes(destination));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.sgpdf-tmp.pdf"));
    }

    private sealed class ProtectionHardeningFixture : IDisposable
    {
        private ProtectionHardeningFixture(string directoryPath, string sourcePath)
        {
            DirectoryPath = directoryPath;
            SourcePath = sourcePath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }

        internal static ProtectionHardeningFixture Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f9-protection-hardening-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = Path.Combine(directory, "source.pdf");
            using (var document = new PdfDocument())
            {
                document.AddPage();
                document.Save(source);
            }

            return new ProtectionHardeningFixture(directory, source);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
