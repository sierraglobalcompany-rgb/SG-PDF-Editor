using System.Reflection;
using PdfSharp.Pdf;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

[Collection(PdfiumNativeGateCollection.Name)]
public sealed class PdfProtectionCapabilityTests
{
    [Fact]
    public async Task SelectedEngine_CreatesProtectedCopy_WithWrongRightPasswordRoundTrip()
    {
        using var fixture = ProtectionFixture.Create();
        var original = File.ReadAllBytes(fixture.SourcePath);
        var destination = Path.Combine(fixture.DirectoryPath, "protected-copy.pdf");

        await CreateProtectedCopyAsync(
            fixture.SourcePath,
            destination,
            userPassword: "user-secret",
            ownerPassword: "owner-secret");

        Assert.Equal(original, File.ReadAllBytes(fixture.SourcePath));
        Assert.True(File.Exists(destination));

        var noPassword = Assert.Throws<PdfDocumentOpenException>(() => PdfDocumentSession.Open(destination));
        Assert.Equal(PdfDocumentOpenError.PasswordRequiredOrIncorrect, noPassword.Error);

        var wrongPassword = Assert.Throws<PdfDocumentOpenException>(
            () => PdfDocumentSession.Open(destination, "wrong-password"));
        Assert.Equal(PdfDocumentOpenError.PasswordRequiredOrIncorrect, wrongPassword.Error);

        using var session = PdfDocumentSession.Open(destination, "user-secret");
        Assert.Equal(1, session.PageCount);
        Assert.True(session.OpenedWithPassword);
    }

    private static async Task CreateProtectedCopyAsync(
        string sourcePath,
        string destinationPath,
        string userPassword,
        string ownerPassword)
    {
        var adapterType = typeof(PdfDocumentSession).Assembly.GetType(
            "SGPdf.App.Features.Utilities.PdfProtectionCapabilityAdapter");
        Assert.NotNull(adapterType);

        var method = adapterType.GetMethod(
            "CreateProtectedCopyAsync",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types:
            [
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(CancellationToken)
            ],
            modifiers: null);
        Assert.NotNull(method);

        var task = Assert.IsAssignableFrom<Task>(method.Invoke(
            null,
            [sourcePath, destinationPath, userPassword, ownerPassword, CancellationToken.None]));
        await task;
    }

    private sealed class ProtectionFixture : IDisposable
    {
        private ProtectionFixture(string directoryPath, string sourcePath)
        {
            DirectoryPath = directoryPath;
            SourcePath = sourcePath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }

        internal static ProtectionFixture Create()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                $"sgpdf-f9-protection-gate-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = Path.Combine(directory, "source.pdf");

            using var document = new PdfDocument();
            document.AddPage();
            document.Save(source);

            return new ProtectionFixture(directory, source);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
