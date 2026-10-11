using System.Reflection;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Ocr;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfOcrWriterTests
{
    private const string WriterTypeName = "SGPdf.App.Pdf.PdfOcrWriter";

    [Fact]
    public void SaveAsCopy_SourceAndDestinationMustDiffer()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var writer = CreateInjectedWriter(static (_, _) => true);

        AssertInvocationThrows<ArgumentException>(() =>
            Save(
                writer,
                fixture.SourcePath,
                PdfEditSourceFingerprint.Capture(fixture.SourcePath),
                Placements(0, "OCR"),
                fixture.SourcePath,
                CancellationToken.None));
    }

    [Fact]
    public void SaveAsCopy_StaleFingerprintFailsWithoutPublishing()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var fingerprint = PdfEditSourceFingerprint.Capture(fixture.SourcePath);
        File.AppendAllText(fixture.SourcePath, "\n% stale-ocr-source\n");
        var destination = fixture.Output("ocr-stale.pdf");

        var result = Save(
            CreateInjectedWriter(static (_, _) => true),
            fixture.SourcePath,
            fingerprint,
            Placements(0, "OCR"),
            destination,
            CancellationToken.None);

        Assert.False(Succeeded(result));
        Assert.False(File.Exists(destination));
        Assert.Empty(TemporaryOutputs(fixture.DirectoryPath));
    }

    [Fact]
    public void SaveAsCopy_InvalidPageIndexFailsWithoutPublishing()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("ocr-invalid-page.pdf");

        var result = Save(
            CreateInjectedWriter(static (_, _) => true),
            fixture.SourcePath,
            PdfEditSourceFingerprint.Capture(fixture.SourcePath),
            Placements(99, "OCR"),
            destination,
            CancellationToken.None);

        Assert.False(Succeeded(result));
        Assert.False(File.Exists(destination));
        Assert.Empty(TemporaryOutputs(fixture.DirectoryPath));
    }

    [Fact]
    public void SaveAsCopy_UnsupportedGlyphFailsClosedWithoutPublishing()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("ocr-unsupported-glyph.pdf");

        AssertInvocationThrows<NotSupportedException>(() =>
            Save(
                CreateInjectedWriter(static (_, _) => true),
                fixture.SourcePath,
                PdfEditSourceFingerprint.Capture(fixture.SourcePath),
                Placements(0, "東"),
                destination,
                CancellationToken.None));

        Assert.False(File.Exists(destination));
        Assert.Empty(TemporaryOutputs(fixture.DirectoryPath));
    }

    [Fact]
    public void SaveAsCopy_SuccessNeverChangesSource()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var sourceBefore = File.ReadAllBytes(fixture.SourcePath);
        var destination = fixture.Output("ocr-success.pdf");

        var result = Save(
            CreateInjectedWriter(static (_, _) => true),
            fixture.SourcePath,
            PdfEditSourceFingerprint.Capture(fixture.SourcePath),
            Placements(0, "OCR"),
            destination,
            CancellationToken.None);

        Assert.True(Succeeded(result), Error(result));
        Assert.Equal(sourceBefore, File.ReadAllBytes(fixture.SourcePath));
        Assert.True(File.Exists(destination));
        Assert.Empty(TemporaryOutputs(fixture.DirectoryPath));
    }

    [Fact]
    public void SaveAsCopy_ValidatorFailurePreservesExistingDestinationAndCleansTemp()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("ocr-validator-failure.pdf");
        File.WriteAllText(destination, "existing destination");
        var destinationBefore = File.ReadAllBytes(destination);

        var result = Save(
            CreateInjectedWriter(static (_, _) => false),
            fixture.SourcePath,
            PdfEditSourceFingerprint.Capture(fixture.SourcePath),
            Placements(0, "OCR"),
            destination,
            CancellationToken.None);

        Assert.False(Succeeded(result));
        Assert.Equal(destinationBefore, File.ReadAllBytes(destination));
        Assert.Empty(TemporaryOutputs(fixture.DirectoryPath));
    }

    [Fact]
    public void SaveAsCopy_ValidationRunsBeforePublication()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("ocr-validation-order.pdf");
        File.WriteAllText(destination, "old destination");
        var destinationBefore = File.ReadAllBytes(destination);
        var validatorCalled = false;

        var writer = CreateInjectedWriter((temporaryPath, _) =>
        {
            validatorCalled = true;
            Assert.True(File.Exists(temporaryPath));
            Assert.Equal(destinationBefore, File.ReadAllBytes(destination));
            return true;
        });

        var result = Save(
            writer,
            fixture.SourcePath,
            PdfEditSourceFingerprint.Capture(fixture.SourcePath),
            Placements(0, "OCR"),
            destination,
            CancellationToken.None);

        Assert.True(validatorCalled);
        Assert.True(Succeeded(result), Error(result));
        Assert.NotEqual(destinationBefore, File.ReadAllBytes(destination));
        Assert.Empty(TemporaryOutputs(fixture.DirectoryPath));
    }

    [Fact]
    public void SaveAsCopy_AlreadyCancelledDoesNotMutateOrPublish()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("ocr-cancelled-before.pdf");
        var sourceBefore = File.ReadAllBytes(fixture.SourcePath);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        AssertInvocationThrows<OperationCanceledException>(() =>
            Save(
                CreateInjectedWriter(static (_, _) => true),
                fixture.SourcePath,
                PdfEditSourceFingerprint.Capture(fixture.SourcePath),
                Placements(0, "OCR"),
                destination,
                cts.Token));

        Assert.Equal(sourceBefore, File.ReadAllBytes(fixture.SourcePath));
        Assert.False(File.Exists(destination));
        Assert.Empty(TemporaryOutputs(fixture.DirectoryPath));
    }

    [Fact]
    public void SaveAsCopy_CancellationDuringWriteCleansTempAndPreservesDestination()
    {
        using var fixture = UtilityOverlayNativeHarness.CreateSourcePdf();
        var destination = fixture.Output("ocr-cancelled-during-write.pdf");
        File.WriteAllText(destination, "existing destination");
        var destinationBefore = File.ReadAllBytes(destination);
        var placements = Enumerable.Range(0, 2500)
            .Select(index => Placement(0, $"OCR {index}", 40d + (index % 20), 120d, 90d + (index % 20), 138d))
            .ToArray();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(10));

        AssertInvocationThrows<OperationCanceledException>(() =>
            Save(
                CreateInjectedWriter(static (_, _) => true),
                fixture.SourcePath,
                PdfEditSourceFingerprint.Capture(fixture.SourcePath),
                placements,
                destination,
                cts.Token));

        Assert.Equal(destinationBefore, File.ReadAllBytes(destination));
        Assert.Empty(TemporaryOutputs(fixture.DirectoryPath));
    }

    private static object CreateInjectedWriter(Func<string, CancellationToken, bool> validator)
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
        var writer = constructor!.Invoke(new object[]
        {
            validator,
            (Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult>)((_, _) => Supported())
        });
        Assert.NotNull(writer);
        return writer!;
    }

    private static OcrTextLinePlacement[] Placements(int pageIndex, string text) =>
        new[] { Placement(pageIndex, text, 48d, 120d, 120d, 142d) };

    private static OcrTextLinePlacement Placement(
        int pageIndex,
        string text,
        double left,
        double bottom,
        double right,
        double top)
    {
        var bounds = new PdfOcrRect(left, bottom, right, top);
        var word = new OcrTextWordPlacement(text, 95d, bounds, 0, 1);
        return new OcrTextLinePlacement(
            pageIndex,
            text,
            bounds,
            new[] { word },
            0,
            1,
            1,
            1);
    }

    private static object Save(
        object writer,
        string sourcePath,
        PdfEditSourceFingerprint fingerprint,
        IReadOnlyList<OcrTextLinePlacement> placements,
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

    private static PdfEditPreflightResult Supported() =>
        new(Array.Empty<PdfEditFinding>());

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
}
