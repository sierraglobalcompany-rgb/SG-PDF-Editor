using System.Globalization;
using SGPdf.App.Features.Utilities;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PageNumberFormatterTests
{
    [Fact]
    public void Format_CombinesPrefixNumberSuffix()
    {
        var settings = new PageNumberingSettings(prefix: "DOC-", suffix: "-A");

        Assert.Equal("DOC-42-A", PageNumberFormatter.Format(42, settings));
    }

    [Fact]
    public void Format_AppliesZeroPadding()
    {
        var settings = new PageNumberingSettings(paddingWidth: 6);

        Assert.Equal("000042", PageNumberFormatter.Format(42, settings));
    }

    [Fact]
    public void Format_WithoutPadding_UsesPlainDecimal()
    {
        Assert.Equal("42", PageNumberFormatter.Format(42, new PageNumberingSettings()));
    }

    [Fact]
    public void Format_WidthSmallerThanNumber_DoesNotTruncate()
    {
        var settings = new PageNumberingSettings(paddingWidth: 3);

        Assert.Equal("12345", PageNumberFormatter.Format(12345, settings));
    }

    [Fact]
    public void Format_SupportedUnicodePrefixSuffix_RoundTripsExactly()
    {
        var settings = new PageNumberingSettings(prefix: "NIÑO Bogotá — € Ω ", suffix: " Ω");

        Assert.Equal("NIÑO Bogotá — € Ω 7 Ω", PageNumberFormatter.Format(7, settings));
    }

    [Fact]
    public void Format_UnsupportedRune_FailsClosed()
    {
        var settings = new PageNumberingSettings(prefix: "東");

        var ex = Assert.Throws<NotSupportedException>(() => PageNumberFormatter.Format(1, settings));
        Assert.Contains("U+6771", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_IsDeterministicAndCultureInvariant()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var customCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        customCulture.NumberFormat.NegativeSign = "~";

        try
        {
            CultureInfo.CurrentCulture = customCulture;
            var settings = new PageNumberingSettings(prefix: "P", suffix: "S", paddingWidth: 4);

            Assert.Equal("P-0042S", PageNumberFormatter.Format(-42, settings));
            Assert.Equal("P-0042S", PageNumberFormatter.Format(-42, settings));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Format_LargeNumber_IsNotTruncated()
    {
        var settings = new PageNumberingSettings(paddingWidth: 25);

        Assert.Equal("0000009223372036854775807", PageNumberFormatter.Format(long.MaxValue, settings));
    }

    [Fact]
    public void Settings_NumberAt_UsesStartingNumberAndSelectedOrder()
    {
        var settings = new PageNumberingSettings(startNumber: 1001);

        Assert.Equal(1001L, settings.NumberAt(0));
        Assert.Equal(1004L, settings.NumberAt(3));
    }

    [Fact]
    public void Settings_NumberAt_Overflow_FailsClosed()
    {
        var settings = new PageNumberingSettings(startNumber: long.MaxValue);

        Assert.Throws<OverflowException>(() => settings.NumberAt(1));
    }

    [Fact]
    public void InvalidSettings_FailClosed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PageNumberingSettings(paddingWidth: -1));
        Assert.Throws<ArgumentNullException>(() => new PageNumberingSettings(prefix: null!));
        Assert.Throws<ArgumentNullException>(() => new PageNumberingSettings(suffix: null!));
    }
}
