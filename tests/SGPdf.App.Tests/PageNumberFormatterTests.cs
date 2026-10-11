using System.Globalization;
using System.Reflection;
using SGPdf.App.Features.Edit.Text;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PageNumberFormatterTests
{
    private const string SettingsTypeName = "SGPdf.App.Features.Utilities.PageNumberingSettings";
    private const string FormatterTypeName = "SGPdf.App.Features.Utilities.PageNumberFormatter";

    [Fact]
    public void Format_CombinesPrefixNumberSuffix()
    {
        var settings = Settings(prefix: "DOC-", suffix: "-A");

        Assert.Equal("DOC-42-A", Format(42, settings));
    }

    [Fact]
    public void Format_AppliesZeroPadding()
    {
        var settings = Settings(paddingWidth: 6);

        Assert.Equal("000042", Format(42, settings));
    }

    [Fact]
    public void Format_WithoutPadding_UsesPlainDecimal()
    {
        Assert.Equal("42", Format(42, Settings()));
    }

    [Fact]
    public void Format_WidthSmallerThanNumber_DoesNotTruncate()
    {
        var settings = Settings(paddingWidth: 3);

        Assert.Equal("12345", Format(12345, settings));
    }

    [Fact]
    public void Format_SupportedUnicodePrefixSuffix_RoundTripsExactly()
    {
        var settings = Settings(prefix: "NIÑO Bogotá — € Ω ", suffix: " Ω");

        Assert.Equal("NIÑO Bogotá — € Ω 7 Ω", Format(7, settings));
    }

    [Fact]
    public void Format_UnsupportedRune_FailsClosed()
    {
        var settings = Settings(prefix: "東");

        var ex = AssertInvocationThrows<NotSupportedException>(() => Format(1, settings));
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
            var settings = Settings(prefix: "P", suffix: "S", paddingWidth: 4);

            Assert.Equal("P-0042S", Format(-42, settings));
            Assert.Equal("P-0042S", Format(-42, settings));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Format_LargeNumber_IsNotTruncated()
    {
        var settings = Settings(paddingWidth: 25);

        Assert.Equal("0000009223372036854775807", Format(long.MaxValue, settings));
    }

    [Fact]
    public void Settings_NumberAt_UsesStartingNumberAndSelectedOrder()
    {
        var settings = Settings(startNumber: 1001);

        Assert.Equal(1001L, NumberAt(settings, 0));
        Assert.Equal(1004L, NumberAt(settings, 3));
    }

    [Fact]
    public void Settings_NumberAt_Overflow_FailsClosed()
    {
        var settings = Settings(startNumber: long.MaxValue);

        AssertInvocationThrows<OverflowException>(() => NumberAt(settings, 1));
    }

    [Fact]
    public void InvalidSettings_FailClosed()
    {
        AssertConstructorThrows<ArgumentOutOfRangeException>(() => Settings(paddingWidth: -1));
        AssertConstructorThrows<ArgumentNullException>(() => Settings(prefix: null!));
        AssertConstructorThrows<ArgumentNullException>(() => Settings(suffix: null!));
    }

    private static object Settings(
        long startNumber = 1,
        string prefix = "",
        string suffix = "",
        int paddingWidth = 0)
    {
        var type = RequireType(SettingsTypeName);
        var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(ctor => ctor.GetParameters().Length == 4);
        return constructor.Invoke(new object?[] { startNumber, prefix, suffix, paddingWidth });
    }

    private static string Format(long number, object settings)
    {
        var type = RequireType(FormatterTypeName);
        var method = type.GetMethod("Format", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return Assert.IsType<string>(method!.Invoke(null, new[] { (object)number, settings }));
    }

    private static long NumberAt(object settings, int selectedPageOrdinal)
    {
        var method = settings.GetType().GetMethod("NumberAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return Assert.IsType<long>(method!.Invoke(settings, new object[] { selectedPageOrdinal }));
    }

    private static Type RequireType(string typeName)
    {
        var type = typeof(FallbackFontAsset).Assembly.GetType(typeName, throwOnError: false);
        Assert.NotNull(type);
        return type!;
    }

    private static TException AssertInvocationThrows<TException>(Action action)
        where TException : Exception
    {
        var wrapper = Assert.Throws<TargetInvocationException>(action);
        return Assert.IsType<TException>(wrapper.InnerException);
    }

    private static TException AssertConstructorThrows<TException>(Action action)
        where TException : Exception
    {
        var wrapper = Assert.Throws<TargetInvocationException>(action);
        return Assert.IsType<TException>(wrapper.InnerException);
    }
}
