using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PdfSharp.Pdf;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfAcroFormSaveServicePreflightTests
{
    private const string UserPassword = "task3-user-password";

    [Fact]
    public void SaveTextFields_SignedSource_IsBlockedBeforeTempOrDestinationChange()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "sgpdf-f12-3-task3-signed-source-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string unsignedSourcePath = Path.Combine(directory, "unsigned-source.pdf");
            string signedSourcePath = Path.Combine(directory, "signed-source.pdf");
            string destinationPath = Path.Combine(directory, "destination.pdf");
            CreateSimplePdf(unsignedSourcePath);
            CreateSimplePdf(destinationPath);
            byte[] destinationBefore = File.ReadAllBytes(destinationPath);

            using X509Certificate2 certificate = CreateCertificate();
            InvokeSignatureWriter(unsignedSourcePath, signedSourcePath, certificate);

            using (PdfDocumentSession session = PdfDocumentSession.Open(signedSourcePath))
                Assert.Equal(1, session.GetCryptographicSignatureCount());

            object service = CreateService();
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                InvokeSave(
                    service,
                    signedSourcePath,
                    destinationPath,
                    new Dictionary<string, string> { ["irrelevant"] = "value" }));

            Assert.Contains("firma", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(destinationBefore, File.ReadAllBytes(destinationPath));
            Assert.Empty(Directory.GetFiles(directory, ".sgpdf-form-*.tmp.pdf"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void SaveTextFields_PasswordProtectedSource_IsBlockedBeforeTempOrDestinationChange()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "sgpdf-f12-3-task3-password-source-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string plainSourcePath = Path.Combine(directory, "plain-source.pdf");
            string protectedSourcePath = Path.Combine(directory, "protected-source.pdf");
            string destinationPath = Path.Combine(directory, "destination.pdf");
            CreateSimplePdf(plainSourcePath);
            CreateSimplePdf(destinationPath);
            byte[] destinationBefore = File.ReadAllBytes(destinationPath);

            EncryptPdfWithQpdf(plainSourcePath, protectedSourcePath, UserPassword);

            PdfDocumentOpenException directOpen = Assert.Throws<PdfDocumentOpenException>(() =>
                PdfDocumentSession.Open(protectedSourcePath));
            Assert.Equal(PdfDocumentOpenError.PasswordRequiredOrIncorrect, directOpen.Error);

            using (PdfDocumentSession passwordSession = PdfDocumentSession.Open(protectedSourcePath, UserPassword))
                Assert.True(passwordSession.OpenedWithPassword);

            object service = CreateService();
            PdfDocumentOpenException exception = Assert.Throws<PdfDocumentOpenException>(() =>
                InvokeSave(
                    service,
                    protectedSourcePath,
                    destinationPath,
                    new Dictionary<string, string> { ["irrelevant"] = "value" }));

            Assert.Equal(PdfDocumentOpenError.PasswordRequiredOrIncorrect, exception.Error);
            Assert.Equal(destinationBefore, File.ReadAllBytes(destinationPath));
            Assert.Empty(Directory.GetFiles(directory, ".sgpdf-form-*.tmp.pdf"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static object CreateService()
    {
        Type type = typeof(MainWindow).Assembly.GetType(
            "SGPdf.App.Features.Forms.PdfAcroFormSaveService",
            throwOnError: true)!;
        ConstructorInfo constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(Action<string, string>) },
            modifiers: null)!;
        return constructor.Invoke(new object?[] { null });
    }

    private static void InvokeSave(
        object service,
        string sourcePath,
        string destinationPath,
        IReadOnlyDictionary<string, string> values)
    {
        MethodInfo method = service.GetType().GetMethod(
            "Save",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[]
            {
                typeof(string),
                typeof(string),
                typeof(IReadOnlyDictionary<string, string>),
                typeof(CancellationToken)
            },
            modifiers: null)!;

        try
        {
            method.Invoke(
                service,
                new object[] { sourcePath, destinationPath, values, CancellationToken.None });
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static void InvokeSignatureWriter(
        string sourcePath,
        string destinationPath,
        X509Certificate2 certificate)
    {
        Type type = typeof(MainWindow).Assembly.GetType(
            "SGPdf.App.Features.Signing.PdfCryptographicSignatureWriter",
            throwOnError: true)!;
        MethodInfo method = type.GetMethod(
            "Write",
            BindingFlags.Static | BindingFlags.NonPublic)!;

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

    private static void EncryptPdfWithQpdf(
        string sourcePath,
        string destinationPath,
        string userPassword)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = FindQpdfPath(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--encrypt");
        startInfo.ArgumentList.Add(userPassword);
        startInfo.ArgumentList.Add("task3-owner-password");
        startInfo.ArgumentList.Add("256");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add(sourcePath);
        startInfo.ArgumentList.Add(destinationPath);

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("No se pudo iniciar qpdf para crear el fixture protegido.");
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(
            process.ExitCode == 0,
            $"qpdf no pudo crear el PDF protegido. Exit={process.ExitCode}. STDOUT={stdout} STDERR={stderr}");
        Assert.True(File.Exists(destinationPath));
    }

    private static string FindQpdfPath()
    {
        foreach (string seed in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (DirectoryInfo? directory = new(seed); directory is not null; directory = directory.Parent)
            {
                string candidate = Path.Combine(
                    directory.FullName,
                    "third_party",
                    "runtime",
                    "qpdf",
                    "qpdf.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        throw new FileNotFoundException(
            "No se encontró third_party/runtime/qpdf/qpdf.exe para crear el fixture protegido.");
    }

    private static X509Certificate2 CreateCertificate()
    {
        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=SG PDF F12.3 Signed Source Gate",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));
    }

    private static void CreateSimplePdf(string path)
    {
        using var document = new PdfDocument();
        document.AddPage();
        document.Save(path);
    }
}
