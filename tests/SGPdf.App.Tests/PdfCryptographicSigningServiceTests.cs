using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using PdfSharp.Pdf;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCryptographicSigningServiceTests
{
    private const string Password = "task5-test-password";

    [Fact]
    public void Sign_PublishesValidatedSignatureWithoutChangingSource_AndCleansTemp()
    {
        using var fixture = SigningFixture.Create();
        byte[] sourceBefore = File.ReadAllBytes(fixture.SourcePath);
        string? observedTemp = null;

        object service = CreateService((sourcePath, tempPath) =>
        {
            Assert.Equal(Path.GetFullPath(fixture.SourcePath), Path.GetFullPath(sourcePath));
            Assert.True(File.Exists(tempPath));
            Assert.False(File.Exists(fixture.DestinationPath));
            Assert.Equal(Path.GetDirectoryName(Path.GetFullPath(fixture.DestinationPath)), Path.GetDirectoryName(Path.GetFullPath(tempPath)));
            observedTemp = tempPath;
        });

        InvokeSign(service, fixture.SourcePath, fixture.DestinationPath, fixture.PfxPath, Password);

        Assert.Equal(sourceBefore, File.ReadAllBytes(fixture.SourcePath));
        Assert.True(File.Exists(fixture.DestinationPath));
        Assert.NotNull(observedTemp);
        Assert.False(File.Exists(observedTemp));

        using PdfDocumentSession session = PdfDocumentSession.Open(fixture.DestinationPath);
        Assert.Equal(1, session.GetCryptographicSignatureCount());
        _ = session.RenderPage(0, 36d);
        InvokeValidator(fixture.DestinationPath);
    }

    [Fact]
    public void Sign_WrongPfxPassword_PreservesExistingDestination_AndLeavesNoTemp()
    {
        using var fixture = SigningFixture.Create(withExistingDestination: true);
        byte[] destinationBefore = File.ReadAllBytes(fixture.DestinationPath);
        object service = CreateService(null);

        Assert.Throws<CryptographicException>(() =>
            InvokeSign(service, fixture.SourcePath, fixture.DestinationPath, fixture.PfxPath, "wrong-password"));

        Assert.Equal(destinationBefore, File.ReadAllBytes(fixture.DestinationPath));
        Assert.Empty(fixture.FindSigningTemps());
    }

    [Fact]
    public void Sign_AlreadySignedSource_IsBlockedBeforeWritingDestination()
    {
        using var fixture = SigningFixture.Create();
        string signedSource = fixture.CreateAdditionalPath("already-signed-source.pdf");
        using X509Certificate2 certificate = LoadPfx(fixture.PfxPath, Password);
        InvokeWriter(fixture.SourcePath, signedSource, certificate);
        object service = CreateService(null);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            InvokeSign(service, signedSource, fixture.DestinationPath, fixture.PfxPath, Password));

        Assert.Contains("firma", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(fixture.DestinationPath));
        Assert.Empty(fixture.FindSigningTemps());
    }

    [Fact]
    public void Sign_SourceFingerprintDriftBeforePublish_PreservesExistingDestination_AndCleansTemp()
    {
        using var fixture = SigningFixture.Create(withExistingDestination: true);
        byte[] destinationBefore = File.ReadAllBytes(fixture.DestinationPath);
        string? observedTemp = null;
        object service = CreateService((sourcePath, tempPath) =>
        {
            observedTemp = tempPath;
            using var stream = new FileStream(sourcePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            stream.WriteByte((byte)'\n');
        });

        IOException exception = Assert.Throws<IOException>(() =>
            InvokeSign(service, fixture.SourcePath, fixture.DestinationPath, fixture.PfxPath, Password));

        Assert.Contains("camb", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(destinationBefore, File.ReadAllBytes(fixture.DestinationPath));
        Assert.NotNull(observedTemp);
        Assert.False(File.Exists(observedTemp));
        Assert.Empty(fixture.FindSigningTemps());
    }

    [Fact]
    public void Sign_ValidatorFailure_PreservesExistingDestination_AndCleansTemp()
    {
        using var fixture = SigningFixture.Create(withExistingDestination: true);
        byte[] destinationBefore = File.ReadAllBytes(fixture.DestinationPath);
        string? observedTemp = null;
        object service = CreateService((_, tempPath) =>
        {
            observedTemp = tempPath;
            CorruptCmsContents(tempPath);
        });

        Assert.Throws<CryptographicException>(() =>
            InvokeSign(service, fixture.SourcePath, fixture.DestinationPath, fixture.PfxPath, Password));

        Assert.Equal(destinationBefore, File.ReadAllBytes(fixture.DestinationPath));
        Assert.NotNull(observedTemp);
        Assert.False(File.Exists(observedTemp));
        Assert.Empty(fixture.FindSigningTemps());
    }

    [Fact]
    public void Sign_CancelledBeforeWork_PreservesExistingDestination_AndLeavesNoTemp()
    {
        using var fixture = SigningFixture.Create(withExistingDestination: true);
        byte[] destinationBefore = File.ReadAllBytes(fixture.DestinationPath);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        object service = CreateService(null);

        Assert.Throws<OperationCanceledException>(() =>
            InvokeSign(
                service,
                fixture.SourcePath,
                fixture.DestinationPath,
                fixture.PfxPath,
                Password,
                cancellation.Token));

        Assert.Equal(destinationBefore, File.ReadAllBytes(fixture.DestinationPath));
        Assert.Empty(fixture.FindSigningTemps());
    }

    [Fact]
    public void Sign_SuccessfullyReplacesExistingDestinationOnlyAfterValidation()
    {
        using var fixture = SigningFixture.Create(withExistingDestination: true);
        byte[] destinationBefore = File.ReadAllBytes(fixture.DestinationPath);
        string? observedTemp = null;
        object service = CreateService((_, tempPath) =>
        {
            observedTemp = tempPath;
            Assert.True(File.Exists(tempPath));
            Assert.Equal(destinationBefore, File.ReadAllBytes(fixture.DestinationPath));
        });

        InvokeSign(service, fixture.SourcePath, fixture.DestinationPath, fixture.PfxPath, Password);

        Assert.NotEqual(destinationBefore, File.ReadAllBytes(fixture.DestinationPath));
        Assert.NotNull(observedTemp);
        Assert.False(File.Exists(observedTemp));
        InvokeValidator(fixture.DestinationPath);
    }

    private static object CreateService(Action<string, string>? beforeValidation)
    {
        Type? type = typeof(MainWindow).Assembly.GetType(
            "SGPdf.App.Features.Signing.PdfCryptographicSigningService");
        Assert.NotNull(type);

        ConstructorInfo? constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(Action<string, string>) },
            modifiers: null);
        Assert.NotNull(constructor);
        return constructor.Invoke(new object?[] { beforeValidation });
    }

    private static void InvokeSign(
        object service,
        string sourcePath,
        string destinationPath,
        string pfxPath,
        string password,
        CancellationToken cancellationToken = default)
    {
        MethodInfo method = service.GetType().GetMethod(
            "Sign",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        try
        {
            method.Invoke(
                service,
                new object[] { sourcePath, destinationPath, pfxPath, password, cancellationToken });
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static void InvokeValidator(string path)
    {
        Type type = typeof(MainWindow).Assembly.GetType(
            "SGPdf.App.Pdf.PdfCryptographicSignatureValidator",
            throwOnError: true)!;
        object validator = Activator.CreateInstance(type, nonPublic: true)!;
        MethodInfo method = type.GetMethod("Validate", BindingFlags.Instance | BindingFlags.NonPublic)!;
        try
        {
            method.Invoke(validator, new object[] { path, CancellationToken.None });
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static X509Certificate2 LoadPfx(string path, string password)
    {
        return X509CertificateLoader.LoadPkcs12FromFile(
            path,
            password,
            X509KeyStorageFlags.EphemeralKeySet);
    }

    private static void InvokeWriter(
        string sourcePath,
        string destinationPath,
        X509Certificate2 certificate)
    {
        Type type = typeof(MainWindow).Assembly.GetType(
            "SGPdf.App.Features.Signing.PdfCryptographicSignatureWriter",
            throwOnError: true)!;
        MethodInfo method = type.GetMethod("Write", BindingFlags.Static | BindingFlags.NonPublic)!;
        try
        {
            method.Invoke(
                null,
                new object[] { sourcePath, destinationPath, certificate, CancellationToken.None });
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static void CorruptCmsContents(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        string text = Encoding.ASCII.GetString(bytes);
        Match match = Regex.Match(text, @"/Contents\s*<(?<cms>[0-9A-Fa-f]+)>");
        Assert.True(match.Success);
        int position = match.Groups["cms"].Index;
        bytes[position] = bytes[position] == (byte)'0' ? (byte)'1' : (byte)'0';
        File.WriteAllBytes(path, bytes);
    }

    private sealed class SigningFixture : IDisposable
    {
        private readonly List<string> _paths = new();
        private readonly string _directory;

        private SigningFixture(string directory, string sourcePath, string destinationPath, string pfxPath)
        {
            _directory = directory;
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
            PfxPath = pfxPath;
            _paths.Add(sourcePath);
            _paths.Add(destinationPath);
            _paths.Add(pfxPath);
        }

        internal string SourcePath { get; }
        internal string DestinationPath { get; }
        internal string PfxPath { get; }

        internal static SigningFixture Create(bool withExistingDestination = false)
        {
            string directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f12-2-task5-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            string sourcePath = Path.Combine(directory, "source.pdf");
            string destinationPath = Path.Combine(directory, "destination.pdf");
            string pfxPath = Path.Combine(directory, "certificate.pfx");

            CreatePdf(sourcePath);
            if (withExistingDestination)
                CreatePdf(destinationPath);
            WritePfx(pfxPath);

            return new SigningFixture(directory, sourcePath, destinationPath, pfxPath);
        }

        internal string CreateAdditionalPath(string name)
        {
            string path = Path.Combine(_directory, name);
            _paths.Add(path);
            return path;
        }

        internal string[] FindSigningTemps()
            => Directory.Exists(_directory)
                ? Directory.GetFiles(_directory, ".sgpdf-sign-*.tmp.pdf")
                : Array.Empty<string>();

        public void Dispose()
        {
            foreach (string path in _paths)
            {
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            try
            {
                if (Directory.Exists(_directory))
                    Directory.Delete(_directory, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static void CreatePdf(string path)
        {
            using var document = new PdfDocument();
            document.AddPage();
            document.Save(path);
        }

        private static void WritePfx(string path)
        {
            using RSA rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                "CN=SG PDF Task 5 Service",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            using X509Certificate2 certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(1));
            File.WriteAllBytes(path, certificate.Export(X509ContentType.Pkcs12, Password));
        }
    }
}
