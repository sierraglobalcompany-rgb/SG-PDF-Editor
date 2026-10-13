using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using PdfSharp.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCryptographicSignatureValidatorTests
{
    [Fact]
    public void Validate_AcceptsValidPdfSharpSignature()
    {
        using var fixture = SignedPdfFixture.Create();

        InvokeValidate(fixture.SignedPath);
    }

    [Fact]
    public void Validate_RejectsUnsignedPdf()
    {
        using var fixture = SignedPdfFixture.Create();

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => InvokeValidate(fixture.SourcePath));

        Assert.Contains("firma", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("negative")]
    [InlineData("out-of-range")]
    [InlineData("overlap")]
    [InlineData("wrong-count")]
    public void Validate_RejectsInvalidByteRange(string mutation)
    {
        using var fixture = SignedPdfFixture.Create();
        string mutatedPath = fixture.CreateCopy($"byterange-{mutation}");
        MutateByteRange(mutatedPath, mutation);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => InvokeValidate(mutatedPath));

        Assert.Contains("ByteRange", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsTamperedCoveredByte()
    {
        using var fixture = SignedPdfFixture.Create();
        string mutatedPath = fixture.CreateCopy("tampered-covered-byte");
        TamperCoveredWhitespace(mutatedPath);

        Assert.Throws<CryptographicException>(() => InvokeValidate(mutatedPath));
    }

    [Fact]
    public void Validate_RejectsInvalidCmsContents()
    {
        using var fixture = SignedPdfFixture.Create();
        string mutatedPath = fixture.CreateCopy("invalid-cms");
        CorruptCmsContents(mutatedPath);

        Assert.ThrowsAny<Exception>(() => InvokeValidateExpectingProductionException(mutatedPath));
    }

    [Fact]
    public void Validate_RejectsUnknownSubFilterFailClosed()
    {
        using var fixture = SignedPdfFixture.Create();
        string mutatedPath = fixture.CreateCopy("unknown-subfilter");
        ReplaceAsciiSameLength(mutatedPath, "adbe.pkcs7.detached", "adbe.pkcs7.detacheX");

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => InvokeValidate(mutatedPath));

        Assert.Contains("SubFilter", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static void InvokeValidate(string filePath)
    {
        Exception? exception = InvokeValidateExpectingProductionException(filePath);
        if (exception is not null)
            throw exception;
    }

    private static Exception? InvokeValidateExpectingProductionException(string filePath)
    {
        Type? type = typeof(MainWindow).Assembly.GetType(
            "SGPdf.App.Pdf.PdfCryptographicSignatureValidator");
        Assert.NotNull(type);

        ConstructorInfo? constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);
        Assert.NotNull(constructor);
        object validator = constructor.Invoke(null);

        MethodInfo? method = type.GetMethod(
            "Validate",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        try
        {
            method.Invoke(validator, new object[] { filePath, CancellationToken.None });
            return null;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            return exception.InnerException;
        }
    }

    private static void MutateByteRange(string path, string mutation)
    {
        byte[] bytes = File.ReadAllBytes(path);
        string text = Encoding.ASCII.GetString(bytes);
        Match match = Regex.Match(text, @"/ByteRange\s*\[(?<range>[^\]]+)\]");
        Assert.True(match.Success, "El PDF firmado debe contener /ByteRange.");

        int[] values = Regex.Matches(match.Groups["range"].Value, @"-?\d+")
            .Select(item => int.Parse(item.Value))
            .ToArray();
        Assert.Equal(4, values.Length);

        string replacement = mutation switch
        {
            "negative" => $"0 -1 {values[2]} {values[3]}",
            "out-of-range" => $"0 {values[1]} {int.MaxValue} 1",
            "overlap" => $"0 {checked(values[2] + 1)} {values[2]} {values[3]}",
            "wrong-count" => $"0 {values[1]}",
            _ => throw new ArgumentOutOfRangeException(nameof(mutation))
        };

        ReplaceGroupWithPadding(bytes, match.Groups["range"], replacement);
        File.WriteAllBytes(path, bytes);
    }

    private static void TamperCoveredWhitespace(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        string text = Encoding.ASCII.GetString(bytes);
        Match match = Regex.Match(text, @"/ByteRange\s*\[(?<range>[^\]]+)\]");
        Assert.True(match.Success);
        int[] values = Regex.Matches(match.Groups["range"].Value, @"\d+")
            .Select(item => int.Parse(item.Value))
            .ToArray();
        Assert.Equal(4, values.Length);

        int start = values[0];
        int end = checked(start + values[1]);
        int position = Array.FindIndex(bytes, start, end - start, value => value == (byte)' ');
        Assert.InRange(position, start, end - 1);
        bytes[position] = (byte)'\t';
        File.WriteAllBytes(path, bytes);
    }

    private static void CorruptCmsContents(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        string text = Encoding.ASCII.GetString(bytes);
        Match match = Regex.Match(text, @"/Contents\s*<(?<cms>[0-9A-Fa-f]+)>");
        Assert.True(match.Success, "El PDF firmado debe contener Contents hexadecimal.");

        int position = match.Groups["cms"].Index;
        bytes[position] = bytes[position] == (byte)'0' ? (byte)'1' : (byte)'0';
        File.WriteAllBytes(path, bytes);
    }

    private static void ReplaceAsciiSameLength(string path, string oldValue, string newValue)
    {
        Assert.Equal(oldValue.Length, newValue.Length);
        byte[] bytes = File.ReadAllBytes(path);
        string text = Encoding.ASCII.GetString(bytes);
        int index = text.IndexOf(oldValue, StringComparison.Ordinal);
        Assert.True(index >= 0, $"No se encontró '{oldValue}' en el PDF.");
        Encoding.ASCII.GetBytes(newValue).CopyTo(bytes, index);
        File.WriteAllBytes(path, bytes);
    }

    private static void ReplaceGroupWithPadding(byte[] bytes, Group group, string replacement)
    {
        Assert.True(replacement.Length <= group.Length, "La mutación debe caber en el placeholder original.");
        string padded = replacement.PadRight(group.Length, ' ');
        Encoding.ASCII.GetBytes(padded).CopyTo(bytes, group.Index);
    }

    private sealed class SignedPdfFixture : IDisposable
    {
        private readonly List<string> _paths = new();

        private SignedPdfFixture(string sourcePath, string signedPath)
        {
            SourcePath = sourcePath;
            SignedPath = signedPath;
            _paths.Add(sourcePath);
            _paths.Add(signedPath);
        }

        internal string SourcePath { get; }
        internal string SignedPath { get; }

        internal static SignedPdfFixture Create()
        {
            string sourcePath = Path.Combine(Path.GetTempPath(), $"sgpdf-f12-2-validator-source-{Guid.NewGuid():N}.pdf");
            string signedPath = Path.Combine(Path.GetTempPath(), $"sgpdf-f12-2-validator-signed-{Guid.NewGuid():N}.pdf");
            using (var document = new PdfDocument())
            {
                document.AddPage();
                document.Save(sourcePath);
            }

            using RSA rsa = RSA.Create(2048);
            using X509Certificate2 certificate = CreateCertificate(rsa);
            InvokeWriter(sourcePath, signedPath, certificate);
            return new SignedPdfFixture(sourcePath, signedPath);
        }

        internal string CreateCopy(string suffix)
        {
            string copyPath = Path.Combine(Path.GetTempPath(), $"sgpdf-f12-2-validator-{suffix}-{Guid.NewGuid():N}.pdf");
            File.Copy(SignedPath, copyPath);
            _paths.Add(copyPath);
            return copyPath;
        }

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
        }

        private static X509Certificate2 CreateCertificate(RSA rsa)
        {
            var request = new CertificateRequest(
                "CN=SG PDF Task 4 Validator",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(1));
        }

        private static void InvokeWriter(
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
                throw exception.InnerException;
            }
        }
    }
}
