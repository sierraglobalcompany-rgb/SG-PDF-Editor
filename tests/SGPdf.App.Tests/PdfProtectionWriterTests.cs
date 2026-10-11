using System.Reflection;
using PdfSharp.Pdf;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

[Collection(PdfiumNativeGateCollection.Name)]
public sealed class PdfProtectionWriterTests
{
    [Fact]
    public async Task SaveAsProtectedCopy_UnprotectedSource_RequiresNewPasswordAndPreservesSource()
    {
        using var fixture = ProtectionWriterFixture.Create(pageCount: 2);
        var original = File.ReadAllBytes(fixture.SourcePath);
        var destination = Path.Combine(fixture.DirectoryPath, "protected.pdf");
        var fingerprint = PdfEditSourceFingerprint.Capture(fixture.SourcePath);

        var result = await InvokeWriterAsync(
            fixture.SourcePath,
            fingerprint,
            openPassword: "new-secret",
            sourcePassword: null,
            authorizationConfirmed: true,
            destination,
            CancellationToken.None);

        Assert.True(ReadSucceeded(result), ReadError(result));
        Assert.Equal(original, File.ReadAllBytes(fixture.SourcePath));
        Assert.True(File.Exists(destination));
        AssertPasswordRequired(destination);
        AssertWrongPasswordRejected(destination, "wrong-password");

        using var output = PdfDocumentSession.Open(destination, "new-secret");
        Assert.Equal(2, output.PageCount);
        Assert.True(output.OpenedWithPassword);
    }

    [Fact]
    public async Task SaveAsProtectedCopy_AuthorizationNotConfirmed_FailsClosed()
    {
        using var fixture = ProtectionWriterFixture.Create(pageCount: 1);
        var destination = Path.Combine(fixture.DirectoryPath, "unauthorized.pdf");

        var result = await InvokeWriterAsync(
            fixture.SourcePath,
            PdfEditSourceFingerprint.Capture(fixture.SourcePath),
            openPassword: "new-secret",
            sourcePassword: null,
            authorizationConfirmed: false,
            destination,
            CancellationToken.None);

        Assert.False(ReadSucceeded(result));
        Assert.Contains("autoriz", ReadError(result), StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public async Task SaveAsProtectedCopy_ProtectedSource_WrongCurrentPasswordFailsWithoutBypass()
    {
        using var fixture = ProtectionWriterFixture.Create(pageCount: 1);
        var protectedSource = Path.Combine(fixture.DirectoryPath, "already-protected.pdf");
        await PdfProtectionCapabilityAdapter.CreateProtectedCopyAsync(
            fixture.SourcePath,
            protectedSource,
            "current-user",
            "current-owner",
            CancellationToken.None);
        var originalProtected = File.ReadAllBytes(protectedSource);
        var destination = Path.Combine(fixture.DirectoryPath, "reprotected.pdf");

        var result = await InvokeWriterAsync(
            protectedSource,
            PdfEditSourceFingerprint.Capture(protectedSource),
            openPassword: "new-secret",
            sourcePassword: "wrong-current",
            authorizationConfirmed: true,
            destination,
            CancellationToken.None);

        Assert.False(ReadSucceeded(result));
        Assert.Contains("contraseña actual", ReadError(result), StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(destination));
        Assert.Equal(originalProtected, File.ReadAllBytes(protectedSource));
        using var sourceStillProtected = PdfDocumentSession.Open(protectedSource, "current-user");
        Assert.Equal(1, sourceStillProtected.PageCount);
    }

    [Fact]
    public async Task SaveAsProtectedCopy_ProtectedSource_CorrectCurrentPasswordCanCreateNewProtectedCopy()
    {
        using var fixture = ProtectionWriterFixture.Create(pageCount: 1);
        var protectedSource = Path.Combine(fixture.DirectoryPath, "already-protected.pdf");
        await PdfProtectionCapabilityAdapter.CreateProtectedCopyAsync(
            fixture.SourcePath,
            protectedSource,
            "current-user",
            "current-owner",
            CancellationToken.None);
        var originalProtected = File.ReadAllBytes(protectedSource);
        var destination = Path.Combine(fixture.DirectoryPath, "reprotected.pdf");

        var result = await InvokeWriterAsync(
            protectedSource,
            PdfEditSourceFingerprint.Capture(protectedSource),
            openPassword: "new-secret",
            sourcePassword: "current-user",
            authorizationConfirmed: true,
            destination,
            CancellationToken.None);

        Assert.True(ReadSucceeded(result), ReadError(result));
        Assert.Equal(originalProtected, File.ReadAllBytes(protectedSource));
        AssertPasswordRequired(destination);
        using var output = PdfDocumentSession.Open(destination, "new-secret");
        Assert.Equal(1, output.PageCount);
    }

    [Fact]
    public async Task SaveAsProtectedCopy_StaleFingerprintFailsBeforePublishing()
    {
        using var fixture = ProtectionWriterFixture.Create(pageCount: 1);
        var fingerprint = PdfEditSourceFingerprint.Capture(fixture.SourcePath);
        File.SetLastWriteTimeUtc(fixture.SourcePath, File.GetLastWriteTimeUtc(fixture.SourcePath).AddSeconds(5));
        var destination = Path.Combine(fixture.DirectoryPath, "stale.pdf");

        var result = await InvokeWriterAsync(
            fixture.SourcePath,
            fingerprint,
            openPassword: "new-secret",
            sourcePassword: null,
            authorizationConfirmed: true,
            destination,
            CancellationToken.None);

        Assert.False(ReadSucceeded(result));
        Assert.Contains("cambió", ReadError(result), StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public async Task SaveAsProtectedCopy_PreCanceled_PreservesExistingDestination()
    {
        using var fixture = ProtectionWriterFixture.Create(pageCount: 1);
        var destination = Path.Combine(fixture.DirectoryPath, "existing.pdf");
        var sentinel = new byte[] { 1, 3, 3, 7 };
        File.WriteAllBytes(destination, sentinel);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await InvokeWriterAsync(
                fixture.SourcePath,
                PdfEditSourceFingerprint.Capture(fixture.SourcePath),
                openPassword: "new-secret",
                sourcePassword: null,
                authorizationConfirmed: true,
                destination,
                cts.Token));

        Assert.Equal(sentinel, File.ReadAllBytes(destination));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.sgpdf-tmp.pdf"));
    }

    private static async Task<object> InvokeWriterAsync(
        string sourcePath,
        PdfEditSourceFingerprint fingerprint,
        string openPassword,
        string? sourcePassword,
        bool authorizationConfirmed,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var assembly = typeof(PdfDocumentSession).Assembly;
        var settingsType = assembly.GetType("SGPdf.App.Features.Utilities.PdfProtectionSettings");
        Assert.NotNull(settingsType);
        var settingsConstructor = settingsType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(string), typeof(string), typeof(bool)],
            modifiers: null);
        Assert.NotNull(settingsConstructor);
        var settings = settingsConstructor.Invoke([openPassword, sourcePassword, authorizationConfirmed]);

        var writerType = assembly.GetType("SGPdf.App.Pdf.PdfProtectionWriter");
        Assert.NotNull(writerType);
        var writer = Activator.CreateInstance(writerType, nonPublic: true);
        Assert.NotNull(writer);

        var method = writerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(candidate =>
                candidate.Name == "SaveAsProtectedCopyAsync" &&
                candidate.GetParameters().Length == 5);
        Assert.NotNull(method);

        var task = Assert.IsAssignableFrom<Task>(method.Invoke(
            writer,
            [sourcePath, fingerprint, settings, destinationPath, cancellationToken]));
        await task;

        var result = task.GetType().GetProperty("Result")?.GetValue(task);
        Assert.NotNull(result);
        return result;
    }

    private static bool ReadSucceeded(object result)
    {
        var property = result.GetType().GetProperty("Succeeded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsType<bool>(property.GetValue(result));
    }

    private static string ReadError(object result)
    {
        var property = result.GetType().GetProperty("Error", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return property.GetValue(result) as string ?? string.Empty;
    }

    private static void AssertPasswordRequired(string path)
    {
        var error = Assert.Throws<PdfDocumentOpenException>(() => PdfDocumentSession.Open(path));
        Assert.Equal(PdfDocumentOpenError.PasswordRequiredOrIncorrect, error.Error);
    }

    private static void AssertWrongPasswordRejected(string path, string password)
    {
        var error = Assert.Throws<PdfDocumentOpenException>(() => PdfDocumentSession.Open(path, password));
        Assert.Equal(PdfDocumentOpenError.PasswordRequiredOrIncorrect, error.Error);
    }

    private sealed class ProtectionWriterFixture : IDisposable
    {
        private ProtectionWriterFixture(string directoryPath, string sourcePath)
        {
            DirectoryPath = directoryPath;
            SourcePath = sourcePath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }

        internal static ProtectionWriterFixture Create(int pageCount)
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f9-protection-writer-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = Path.Combine(directory, "source.pdf");
            using (var document = new PdfDocument())
            {
                for (var index = 0; index < pageCount; index++)
                    document.AddPage();
                document.Save(source);
            }

            return new ProtectionWriterFixture(directory, source);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
