using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowUtilitiesTests
{
    [Fact]
    public void HerramientasCommands_ArePresentAndDisabledWithoutPdf()
    {
        OrganizeWindowTestHost.RunInSta(() =>
        {
            var window = new MainWindow();
            OrganizeWindowTestHost.EnsureLoaded(window);
            try
            {
                var tools = OrganizeWindowTestHost.Element<MenuItem>(window, "UtilitiesMenuItem");
                var watermark = OrganizeWindowTestHost.Element<MenuItem>(window, "WatermarkTextMenuItem");
                var numbering = OrganizeWindowTestHost.Element<MenuItem>(window, "PageNumberingMenuItem");

                Assert.Equal("_Herramientas", tools.Header);
                Assert.False(watermark.IsEnabled);
                Assert.False(numbering.IsEnabled);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void HerramientasCommands_EnableAfterOpeningPdf()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(3);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True(OrganizeWindowTestHost.Element<MenuItem>(window, "WatermarkTextMenuItem").IsEnabled);
                Assert.True(OrganizeWindowTestHost.Element<MenuItem>(window, "PageNumberingMenuItem").IsEnabled);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void WatermarkCommand_SaveAsCancellation_IsNoOp()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var original = File.ReadAllBytes(fixture.Path);
                var request = CreateRequest(
                    "WatermarkUtilityRequest",
                    new Dictionary<string, object?>
                    {
                        ["text"] = "CONFIDENCIAL",
                        ["fontSize"] = 28d,
                        ["opacity"] = 0.35d,
                        ["rotationDegrees"] = -35d,
                        ["offsetXPoints"] = 0d,
                        ["offsetYPoints"] = 0d,
                        ["startPageNumber"] = 1,
                        ["endPageNumber"] = 2,
                        ["useCurrentPage"] = false,
                        ["background"] = true,
                        ["anchorName"] = "Center",
                        ["red"] = (byte)96,
                        ["green"] = (byte)96,
                        ["blue"] = (byte)96
                    });

                Assert.False((bool)InvokeByName(window, "TryRunWatermarkUtility", request, null)!);
                Assert.Equal(original, File.ReadAllBytes(fixture.Path));
                Assert.Contains("cancel", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
                Assert.False(ReadBoolField(window, "_utilityExecuting"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void WatermarkCommand_InvalidRequest_ShowsValidationAndDoesNotWrite()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var destination = Path.Combine(fixture.DirectoryPath, "invalid-watermark.pdf");
                var request = CreateRequest(
                    "WatermarkUtilityRequest",
                    new Dictionary<string, object?>
                    {
                        ["text"] = "",
                        ["fontSize"] = 0d,
                        ["opacity"] = 2d,
                        ["rotationDegrees"] = 0d,
                        ["offsetXPoints"] = 0d,
                        ["offsetYPoints"] = 0d,
                        ["startPageNumber"] = 1,
                        ["endPageNumber"] = 1,
                        ["useCurrentPage"] = false,
                        ["background"] = false,
                        ["anchorName"] = "Center",
                        ["red"] = (byte)0,
                        ["green"] = (byte)0,
                        ["blue"] = (byte)0
                    });

                Assert.False((bool)InvokeByName(window, "TryRunWatermarkUtility", request, destination)!);
                Assert.False(File.Exists(destination));
                Assert.Contains("válid", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
                Assert.False(ReadBoolField(window, "_utilityExecuting"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void PageNumberingCommand_SaveAsCancellation_IsNoOp()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(3);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var original = File.ReadAllBytes(fixture.Path);
                var request = CreateRequest(
                    "PageNumberingUtilityRequest",
                    new Dictionary<string, object?>
                    {
                        ["startNumber"] = 1001L,
                        ["prefix"] = "SG-",
                        ["suffix"] = "",
                        ["paddingWidth"] = 6,
                        ["fontSize"] = 11d,
                        ["opacity"] = 1d,
                        ["offsetXPoints"] = 0d,
                        ["offsetYPoints"] = 0d,
                        ["startPageNumber"] = 1,
                        ["endPageNumber"] = 3,
                        ["useCurrentPage"] = false,
                        ["background"] = false,
                        ["anchorName"] = "FooterCenter",
                        ["red"] = (byte)0,
                        ["green"] = (byte)0,
                        ["blue"] = (byte)0
                    });

                Assert.False((bool)InvokeByName(window, "TryRunPageNumberingUtility", request, null)!);
                Assert.Equal(original, File.ReadAllBytes(fixture.Path));
                Assert.Contains("cancel", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
                Assert.False(ReadBoolField(window, "_utilityExecuting"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void WatermarkCommand_WriterError_PreservesSourceAndReportsError()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var original = File.ReadAllBytes(fixture.Path);
                var request = CreateRequest(
                    "WatermarkUtilityRequest",
                    new Dictionary<string, object?>
                    {
                        ["text"] = "ERROR TEST",
                        ["fontSize"] = 18d,
                        ["opacity"] = 1d,
                        ["rotationDegrees"] = 0d,
                        ["offsetXPoints"] = 0d,
                        ["offsetYPoints"] = 0d,
                        ["startPageNumber"] = 1,
                        ["endPageNumber"] = 1,
                        ["useCurrentPage"] = false,
                        ["background"] = false,
                        ["anchorName"] = "Center",
                        ["red"] = (byte)0,
                        ["green"] = (byte)0,
                        ["blue"] = (byte)0
                    });

                Assert.False((bool)InvokeByName(window, "TryRunWatermarkUtility", request, fixture.Path)!);
                Assert.Equal(original, File.ReadAllBytes(fixture.Path));
                Assert.Contains("no se pudo", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
                Assert.False(ReadBoolField(window, "_utilityExecuting"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    private static bool ReadBoolField(MainWindow window, string name)
        => Assert.IsType<bool>(OrganizeWindowTestHost.GetField(window, name));

    private static object CreateRequest(string typeName, IReadOnlyDictionary<string, object?> values)
    {
        var type = typeof(MainWindow).Assembly.GetType($"SGPdf.App.Features.Utilities.{typeName}");
        Assert.NotNull(type);
        var constructor = Assert.Single(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        var arguments = constructor.GetParameters()
            .Select(parameter => values.TryGetValue(parameter.Name!, out var value)
                ? value
                : throw new InvalidOperationException($"Missing test value for {parameter.Name}."))
            .ToArray();
        return constructor.Invoke(arguments);
    }

    private static object? InvokeByName(MainWindow window, string name, params object?[] args)
    {
        var method = typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(candidate => candidate.Name == name && candidate.GetParameters().Length == args.Length);
        Assert.NotNull(method);
        return method.Invoke(window, args);
    }
}
