using System.Reflection;
using System.Text;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfTextOverlayWriterTests
{
    private const string WriterTypeName = "SGPdf.App.Pdf.PdfTextOverlayWriter";
    private const string PlacementTypeName = "SGPdf.App.Features.Utilities.TextOverlayPlacement";

    [Fact]
    public void SaveAsCopy_SourceAndDestinationMustDiffer()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var fingerprint = PdfEditSourceFingerprint.Capture(fixture.SourcePath);
        var writer = CreateWriter();
        var placements = CreatePlacements(
            CreatePlacement(0, "WM-SAME-PATH", 22d, 0.6d, TextOverlayLayer.Foreground,
                new PdfObjectMatrix(1d, 0d, 0d, 1d, 48d, 160d)));

        AssertInvocationThrows<ArgumentException>(() =>
            Save(writer, fixture.SourcePath, fingerprint, placements, fixture.SourcePath, CancellationToken.None));
    }

    [Fact]
    public void SaveAsCopy_StaleFingerprintFailsWithoutPublishing()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var fingerprint = PdfEditSourceFingerprint.Capture(fixture.SourcePath);
        File.AppendAllText(fixture.SourcePath, "\n% stale-source\n", Encoding.ASCII);
        var destination = fixture.Output("stale-output.pdf");

        var result = Save(
            CreateWriter(),
            fixture.SourcePath,
            fingerprint,
            CreatePlacements(CreatePlacement(0, "WM-STALE", 22d, 0.6d, TextOverlayLayer.Foreground,
                new PdfObjectMatrix(1d, 0d, 0d, 1d, 48d, 160d))),
            destination,
            CancellationToken.None);

        Assert.False(Succeeded(result));
        Assert.False(File.Exists(destination));
        Assert.Empty(TemporaryOutputs(fixture.DirectoryPath));
    }

    [Fact]
    public void SaveAsCopy_PasswordRiskFailsClosed()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("password-blocked.pdf");
        var writer = CreateInjectedWriter(
            static (_, _) => true,
            static (_, _) => Block(PdfEditFindingKind.PasswordProtectedSource, "password protected"));

        var result = Save(
            writer,
            fixture.SourcePath,
            PdfEditSourceFingerprint.Capture(fixture.SourcePath),
            CreatePlacements(CreatePlacement(0, "WM-PASSWORD", 22d, 0.6d, TextOverlayLayer.Foreground,
                new PdfObjectMatrix(1d, 0d, 0d, 1d, 48d, 160d))),
            destination,
            CancellationToken.None);

        Assert.False(Succeeded(result));
        Assert.Contains("password", Error(result) ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(destination));
        Assert.Empty(TemporaryOutputs(fixture.DirectoryPath));
    }

    [Fact]
    public void SaveAsCopy_SignatureRiskFailsClosed()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("signature-blocked.pdf");
        var writer = CreateInjectedWriter(
            static (_, _) => true,
            static (_, _) => Block(PdfEditFindingKind.CryptographicSignature, "cryptographic signature"));

        var result = Save(
            writer,
            fixture.SourcePath,
            PdfEditSourceFingerprint.Capture(fixture.SourcePath),
            CreatePlacements(CreatePlacement(0, "WM-SIGNATURE", 22d, 0.6d, TextOverlayLayer.Foreground,
                new PdfObjectMatrix(1d, 0d, 0d, 1d, 48d, 160d))),
            destination,
            CancellationToken.None);

        Assert.False(Succeeded(result));
        Assert.Contains("signature", Error(result) ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(destination));
        Assert.Empty(TemporaryOutputs(fixture.DirectoryPath));
    }

    [Fact]
    public void SaveAsCopy_BackgroundAndForegroundPreserveExpectedObjectOrder()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("layers.pdf");
        var backMatrix = new PdfObjectMatrix(1d, 0d, 0d, 1d, 34d, 205d);
        var frontMatrix = new PdfObjectMatrix(1d, 0d, 0d, 1d, 72d, 110d);

        var result = Save(
            CreateWriter(),
            fixture.SourcePath,
            PdfEditSourceFingerprint.Capture(fixture.SourcePath),
            CreatePlacements(
                CreatePlacement(0, "WM-BACK", 21d, 0.35d, TextOverlayLayer.Background, backMatrix),
                CreatePlacement(0, "WM-FRONT", 23d, 0.75d, TextOverlayLayer.Foreground, frontMatrix)),
            destination,
            CancellationToken.None);

        Assert.True(Succeeded(result), Error(result));
        using var session = PdfDocumentSession.Open(destination);
        var textObjects = session.GetTextObjects(0);
        var back = Assert.Single(textObjects, item => item.Text == "WM-BACK");
        var original = Assert.Single(textObjects, item => item.Text == UtilityOverlayNativeHarness.BaseText);
        var front = Assert.Single(textObjects, item => item.Text == "WM-FRONT");

        Assert.True(back.Key.PageObjectIndex < original.Key.PageObjectIndex);
        Assert.True(original.Key.PageObjectIndex < front.Key.PageObjectIndex);
        Assert.InRange(back.FillColor.Alpha, 88u, 90u);
        Assert.InRange(front.FillColor.Alpha, 190u, 192u);
    }

    [Fact]
    public void SaveAsCopy_RotatedPagePreservesPlacementMatrix()
    {
        using var fixture = CreateRotatedSourcePdf();
        var destination = fixture.Output("rotated.pdf");
        var degrees = 27d;
        var radians = degrees * Math.PI / 180d;
        var expected = new PdfObjectMatrix(
            Math.Cos(radians),
            Math.Sin(radians),
            -Math.Sin(radians),
            Math.Cos(radians),
            64d,
            132d);

        var result = Save(
            CreateWriter(),
            fixture.SourcePath,
            PdfEditSourceFingerprint.Capture(fixture.SourcePath),
            CreatePlacements(CreatePlacement(0, "WM-ROTATED", 24d, 0.65d, TextOverlayLayer.Foreground, expected)),
            destination,
            CancellationToken.None);

        Assert.True(Succeeded(result), Error(result));
        using var session = PdfDocumentSession.Open(destination);
        var overlay = Assert.Single(session.GetTextObjects(0), item => item.Text == "WM-ROTATED");
        AssertMatrix(expected, overlay.Matrix, 0.0001d);
    }

    [Fact]
    public void SaveAsCopy_CancellationDuringValidationCleansTempAndPreservesDestination()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("cancelled.pdf");
        File.Copy(fixture.SourcePath, destination);
        var originalDestination = File.ReadAllBytes(destination);
        using var cts = new CancellationTokenSource();
        var writer = CreateInjectedWriter(
            (_, token) =>
            {
                cts.Cancel();
                token.ThrowIfCancellationRequested();
                return true;
            },
            static (_, _) => Supported());

        AssertInvocationThrows<OperationCanceledException>(() =>
            Save(
                writer,
                fixture.SourcePath,
                PdfEditSourceFingerprint.Capture(fixture.SourcePath),
                CreatePlacements(CreatePlacement(0, "WM-CANCEL", 22d, 0.6d, TextOverlayLayer.Foreground,
                    new PdfObjectMatrix(1d, 0d, 0d, 1d, 48d, 160d))),
                destination,
                cts.Token));

        Assert.Equal(originalDestination, File.ReadAllBytes(destination));
        Assert.Empty(TemporaryOutputs(fixture.DirectoryPath));
    }

    [Fact]
    public void SaveAsCopy_ValidatorFailurePreservesDestinationAndCleansTemp()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("validator-failure.pdf");
        File.Copy(fixture.SourcePath, destination);
        var originalDestination = File.ReadAllBytes(destination);
        var writer = CreateInjectedWriter(
            static (_, _) => false,
            static (_, _) => Supported());

        var result = Save(
            writer,
            fixture.SourcePath,
            PdfEditSourceFingerprint.Capture(fixture.SourcePath),
            CreatePlacements(CreatePlacement(0, "WM-INVALID", 22d, 0.6d, TextOverlayLayer.Foreground,
                new PdfObjectMatrix(1d, 0d, 0d, 1d, 48d, 160d))),
            destination,
            CancellationToken.None);

        Assert.False(Succeeded(result));
        Assert.Equal(originalDestination, File.ReadAllBytes(destination));
        Assert.Empty(TemporaryOutputs(fixture.DirectoryPath));
    }

    [Fact]
    public void SaveAsCopy_UnsupportedGlyphFailsClosedWithoutPublishing()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("unsupported-glyph.pdf");

        AssertInvocationThrows<NotSupportedException>(() =>
            Save(
                CreateWriter(),
                fixture.SourcePath,
                PdfEditSourceFingerprint.Capture(fixture.SourcePath),
                CreatePlacements(CreatePlacement(0, "東", 22d, 0.6d, TextOverlayLayer.Foreground,
                    new PdfObjectMatrix(1d, 0d, 0d, 1d, 48d, 160d))),
                destination,
                CancellationToken.None));

        Assert.False(File.Exists(destination));
        Assert.Empty(TemporaryOutputs(fixture.DirectoryPath));
    }

    private static PdfEditPreflightResult Supported() =>
        new(Array.Empty<PdfEditFinding>());

    private static PdfEditPreflightResult Block(PdfEditFindingKind kind, string message) =>
        new(Array.AsReadOnly(new[]
        {
            new PdfEditFinding(
                kind,
                PdfEditFindingSeverity.Block,
                message,
                PdfEditPreservationStatus.Unknown)
        }));

    private static object CreateWriter()
    {
        var type = RequireType(WriterTypeName);
        var writer = Activator.CreateInstance(type, nonPublic: true);
        Assert.NotNull(writer);
        return writer!;
    }

    private static object CreateInjectedWriter(
        Func<string, CancellationToken, bool> validator,
        Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult> preflight)
    {
        var type = RequireType(WriterTypeName);
        var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(candidate =>
            {
                var parameters = candidate.GetParameters();
                return parameters.Length == 2 &&
                       parameters[0].ParameterType == typeof(Func<string, CancellationToken, bool>) &&
                       parameters[1].ParameterType == typeof(Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult>);
            });
        Assert.NotNull(constructor);
        var writer = constructor!.Invoke(new object[] { validator, preflight });
        Assert.NotNull(writer);
        return writer;
    }

    private static Array CreatePlacements(params object[] placements)
    {
        var placementType = RequireType(PlacementTypeName);
        var array = Array.CreateInstance(placementType, placements.Length);
        for (var index = 0; index < placements.Length; index++)
            array.SetValue(placements[index], index);
        return array;
    }

    private static object CreatePlacement(
        int pageIndex,
        string text,
        double fontSize,
        double opacity,
        TextOverlayLayer layer,
        PdfObjectMatrix matrix)
    {
        var type = RequireType(PlacementTypeName);
        var placement = Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { pageIndex, text, fontSize, opacity, layer, matrix },
            culture: null);
        Assert.NotNull(placement);
        return placement!;
    }

    private static object Save(
        object writer,
        string sourcePath,
        PdfEditSourceFingerprint fingerprint,
        Array placements,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var method = writer.GetType().GetMethod(
            "SaveAsCopy",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var result = method!.Invoke(
            writer,
            new object[] { sourcePath, fingerprint, placements, destinationPath, cancellationToken });
        Assert.NotNull(result);
        return result!;
    }

    private static bool Succeeded(object result)
    {
        var property = result.GetType().GetProperty("Succeeded");
        Assert.NotNull(property);
        return Assert.IsType<bool>(property!.GetValue(result));
    }

    private static string? Error(object result)
    {
        var property = result.GetType().GetProperty("Error");
        Assert.NotNull(property);
        return property!.GetValue(result) as string;
    }

    private static Type RequireType(string typeName)
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType(typeName, throwOnError: false);
        Assert.NotNull(type);
        return type!;
    }

    private static TException AssertInvocationThrows<TException>(Action action)
        where TException : Exception
    {
        var wrapper = Assert.Throws<TargetInvocationException>(action);
        return Assert.IsType<TException>(wrapper.InnerException);
    }

    private static string[] TemporaryOutputs(string directoryPath) =>
        Directory.GetFiles(directoryPath, "*.sgpdf-tmp.pdf", SearchOption.TopDirectoryOnly);

    private static void AssertMatrix(PdfObjectMatrix expected, PdfObjectMatrix actual, double tolerance)
    {
        Assert.InRange(actual.A, expected.A - tolerance, expected.A + tolerance);
        Assert.InRange(actual.B, expected.B - tolerance, expected.B + tolerance);
        Assert.InRange(actual.C, expected.C - tolerance, expected.C + tolerance);
        Assert.InRange(actual.D, expected.D - tolerance, expected.D + tolerance);
        Assert.InRange(actual.E, expected.E - tolerance, expected.E + tolerance);
        Assert.InRange(actual.F, expected.F - tolerance, expected.F + tolerance);
    }

    private static UtilityOverlayFixture CreateRotatedSourcePdf()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f9-writer-rotated-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var sourcePath = Path.Combine(directory, "source-rotated.pdf");
        WriteMinimalHelveticaPdf(sourcePath, "ROTATED-BASE", rotateDegrees: 90);
        return new UtilityOverlayFixture(directory, sourcePath);
    }

    private static void WriteMinimalHelveticaPdf(string path, string text, int rotateDegrees)
    {
        var content = $"BT\n/F1 20 Tf\n0 0 0 rg\n1 0 0 1 92 150 Tm\n({text}) Tj\nET\n";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Rotate {rotateDegrees} /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream"
        };

        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true) { NewLine = "\n" };
        writer.Write("%PDF-1.4\n");
        writer.Flush();
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(stream.Position);
            writer.Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
            writer.Flush();
        }
        var xrefOffset = stream.Position;
        writer.Write($"xref\n0 {objects.Length + 1}\n");
        writer.Write("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            writer.Write($"{offset:0000000000} 00000 n \n");
        writer.Write($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        writer.Flush();
    }
}
