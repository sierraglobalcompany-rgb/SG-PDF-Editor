using SGPdf.App.Pdf;

namespace SGPdf.App.Tests;

internal sealed class UtilityOverlayFixture : IDisposable
{
    internal UtilityOverlayFixture(string directoryPath, string sourcePath)
    {
        DirectoryPath = directoryPath;
        SourcePath = sourcePath;
    }

    internal string DirectoryPath { get; }
    internal string SourcePath { get; }

    internal string Output(string fileName) => Path.Combine(DirectoryPath, fileName);

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }
}

internal readonly record struct UtilityOverlayObjectSnapshot(
    int Index,
    int Type,
    string? Text,
    uint Red,
    uint Green,
    uint Blue,
    uint Alpha,
    PdfObjectMatrix Matrix);

internal sealed record UtilityOverlayResult(
    IReadOnlyList<UtilityOverlayObjectSnapshot> Objects,
    int RenderPixelWidth,
    int RenderPixelHeight);

internal static class UtilityOverlayNativeHarness
{
    internal const string BaseText = "BASE";
    internal const uint OverlayRed = 35;
    internal const uint OverlayGreen = 90;
    internal const uint OverlayBlue = 200;
    internal const uint OverlayAlpha = 96;
    internal const uint ImageAlpha = 112;

    internal static readonly PdfObjectMatrix BackTextMatrix =
        RotationMatrix(degrees: 24d, x: 42d, y: 210d);

    internal static readonly PdfObjectMatrix FrontTextMatrix =
        RotationMatrix(degrees: -18d, x: 74d, y: 126d);

    internal static readonly PdfObjectMatrix BackImageMatrix =
        new(70d, 0d, 0d, 42d, 38d, 190d);

    internal static readonly PdfObjectMatrix FrontImageMatrix =
        new(54d, 0d, 0d, 32d, 116d, 102d);

    internal static UtilityOverlayFixture CreateSourcePdf()
        => throw new NotSupportedException("F9 Task 1 RED: source fixture not implemented.");

    internal static UtilityOverlayResult CreateTextOverlayCopy(
        string sourcePath,
        string destinationPath,
        string overlayText)
        => throw new NotSupportedException("F9 Task 1 RED: text overlay characterization not implemented.");

    internal static UtilityOverlayResult CreateImageOverlayCopy(
        string sourcePath,
        string destinationPath)
        => throw new NotSupportedException("F9 Task 1 RED: image overlay characterization not implemented.");

    private static PdfObjectMatrix RotationMatrix(double degrees, double x, double y)
    {
        var radians = degrees * Math.PI / 180d;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return new PdfObjectMatrix(cos, sin, -sin, cos, x, y);
    }
}
