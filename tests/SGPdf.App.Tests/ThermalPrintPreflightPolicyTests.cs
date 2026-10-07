using SGPdf.App.Printing;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ThermalPrintPreflightPolicyTests
{
    [Fact]
    public void ExactValidatedMedia_IsAccepted()
    {
        var decision = ThermalPrintPreflightPolicy.Evaluate(Input(102, 152, 102, 152));

        Assert.True(decision.CanPrint);
        Assert.Null(decision.BlockReason);
        Assert.Empty(decision.Warnings);
        Assert.Equal(1, decision.EffectiveCopyCount);
    }

    [Fact]
    public void QuantizedFourBySixWithinHalfMillimeter_IsAcceptedWithoutScalingPermission()
    {
        var decision = ThermalPrintPreflightPolicy.Evaluate(Input(102, 152, 101.6, 152.4));

        Assert.True(decision.CanPrint);
        Assert.DoesNotContain(decision.Warnings, warning => warning.Contains("escala", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(decision.Warnings, warning => warning.Contains("ajust", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidatedMediaBeyondHalfMillimeter_IsBlocked()
    {
        var decision = ThermalPrintPreflightPolicy.Evaluate(Input(102, 152, 101.4, 152.4));

        Assert.False(decision.CanPrint);
        Assert.NotNull(decision.BlockReason);
        Assert.Contains("tamaño", decision.BlockReason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingValidatedMediaDimension_IsBlocked()
    {
        var decision = ThermalPrintPreflightPolicy.Evaluate(Input(102, 152, null, 152));

        Assert.False(decision.CanPrint);
        Assert.NotNull(decision.BlockReason);
        Assert.Contains("tamaño", decision.BlockReason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SwappedDimensions_AreBlockedForUnrotatedRequest()
    {
        var decision = ThermalPrintPreflightPolicy.Evaluate(Input(102, 152, 152, 102));

        Assert.False(decision.CanPrint);
        Assert.NotNull(decision.BlockReason);
    }

    [Fact]
    public void ImageableAreaShortfall_AddsWarningButDoesNotBlock()
    {
        var decision = ThermalPrintPreflightPolicy.Evaluate(Input(
            102,
            152,
            102,
            152,
            imageableArea: new ThermalImageableAreaMm(1, 2, 100, 148)));

        Assert.True(decision.CanPrint);
        Assert.Contains(decision.Warnings, warning => warning.Contains("recorte", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MissingImageableArea_DoesNotBlock()
    {
        var decision = ThermalPrintPreflightPolicy.Evaluate(Input(102, 152, 102, 152, imageableArea: null));

        Assert.True(decision.CanPrint);
        Assert.DoesNotContain(decision.Warnings, warning => warning.Contains("recorte", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MatchingResolution_IsSelected()
    {
        var capabilities = new[]
        {
            new ThermalPrinterResolution(300, 300),
            new ThermalPrinterResolution(204, 202),
            new ThermalPrinterResolution(203, 203)
        };

        var selected = ThermalPrintPreflightPolicy.SelectMatchingResolution(203, capabilities);

        Assert.NotNull(selected);
        Assert.Equal(new ThermalPrinterResolution(203, 203), selected);
    }

    [Fact]
    public void UnsupportedResolution_AddsWarning()
    {
        var decision = ThermalPrintPreflightPolicy.Evaluate(Input(
            102,
            152,
            102,
            152,
            requestedDpi: 203,
            validatedDpi: 300));

        Assert.True(decision.CanPrint);
        Assert.Contains(decision.Warnings, warning => warning.Contains("resolución", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DialogCopyCountAboveOne_NormalizesEffectiveCopiesToOneAndWarns()
    {
        var decision = ThermalPrintPreflightPolicy.Evaluate(Input(
            102,
            152,
            102,
            152,
            dialogCopyCount: 3));

        Assert.True(decision.CanPrint);
        Assert.Equal(1, decision.EffectiveCopyCount);
        Assert.Contains(decision.Warnings, warning =>
            warning.Contains("SG PDF Editor", StringComparison.OrdinalIgnoreCase) &&
            warning.Contains("cantidad", StringComparison.OrdinalIgnoreCase));
    }

    private static ThermalPrintPreflightInput Input(
        double requestedWidth,
        double requestedHeight,
        double? validatedWidth,
        double? validatedHeight,
        int requestedDpi = 203,
        int? validatedDpi = 203,
        int? dialogCopyCount = 1,
        ThermalImageableAreaMm? imageableArea = null)
        => new(
            requestedWidth,
            requestedHeight,
            validatedWidth,
            validatedHeight,
            requestedDpi,
            validatedDpi,
            dialogCopyCount,
            imageableArea);
}
