using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowUtilitiesTests
{
    [Fact]
    public void PdfToJpgUi_ExposesMinimalMainWindowHandlers()
    {
        Type windowType = typeof(MainWindow);
        MethodInfo? click = windowType.GetMethod("ExportPdfToJpg_Click", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo? runner = windowType.GetMethod("TryExportPdfToJpgAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(click);
        Assert.NotNull(runner);
        Assert.Equal(typeof(Task<bool>), runner!.ReturnType);
    }

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
                var pdfToJpg = OrganizeWindowTestHost.Element<MenuItem>(window, "PdfToJpgMenuItem");
                var compression = OrganizeWindowTestHost.Element<MenuItem>(window, "CompressPdfMenuItem");
                var imageWatermark = OrganizeWindowTestHost.Element<MenuItem>(window, "ImageWatermarkMenuItem");

                Assert.Equal("_Herramientas", tools.Header);
                Assert.False(watermark.IsEnabled);
                Assert.False(numbering.IsEnabled);
                Assert.Equal("Exportar PDF a _JPG...", pdfToJpg.Header);
                Assert.False(pdfToJpg.IsEnabled);
                Assert.Equal("_Comprimir PDF...", compression.Header);
                Assert.False(compression.IsEnabled);
                Assert.Equal("Marca de agua de _imagen...", imageWatermark.Header);
                Assert.False(imageWatermark.IsEnabled);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void HerramientasTopLevelMenu_IsSingleAndReusedAfterLoaded()
    {
        OrganizeWindowTestHost.RunInSta(() =>
        {
            var window = new MainWindow();
            OrganizeWindowTestHost.EnsureLoaded(window);
            try
            {
                var tools = OrganizeWindowTestHost.Element<MenuItem>(window, "UtilitiesMenuItem");
                var menu = Assert.IsType<Menu>(ItemsControl.ItemsControlFromItemContainer(tools));
                var toolsMenus = menu.Items
                    .OfType<MenuItem>()
                    .Where(item => string.Equals((string?)item.Header, "_Herramientas", StringComparison.Ordinal))
                    .ToArray();

                Assert.Same(tools, Assert.Single(toolsMenus));
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
                Assert.True(OrganizeWindowTestHost.Element<MenuItem>(window, "CompressPdfMenuItem").IsEnabled);
                Assert.True(OrganizeWindowTestHost.Element<MenuItem>(window, "PdfToJpgMenuItem").IsEnabled);
                Assert.True(OrganizeWindowTestHost.Element<MenuItem>(window, "ImageWatermarkMenuItem").IsEnabled);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void ImageWatermarkDialog_ExposesAccessibleControlsAndOnlyRevealsSelectedRange()
    {
        OrganizeWindowTestHost.RunInSta(() =>
        {
            var dialog = new SGPdf.App.Features.Utilities.ImageWatermarkDialog(pageCount: 4, currentPageIndex: 2);
            try
            {
                var choose = Assert.IsType<Button>(dialog.FindName("ChooseImageButton"));
                var scope = Assert.IsType<ComboBox>(dialog.FindName("ScopeComboBox"));
                var range = Assert.IsType<StackPanel>(dialog.FindName("RangePanel"));
                var width = Assert.IsType<TextBox>(dialog.FindName("WidthPercentTextBox"));
                var opacity = Assert.IsType<TextBox>(dialog.FindName("OpacityPercentTextBox"));
                Assert.Equal("Elegir imagen PNG o JPEG", AutomationProperties.GetName(choose));
                Assert.Equal("30", width.Text);
                Assert.Equal("35", opacity.Text);
                Assert.Equal(Visibility.Collapsed, range.Visibility);

                scope.SelectedIndex = 2;
                Assert.Equal(Visibility.Visible, range.Visibility);
                Assert.Equal("4", Assert.IsType<TextBox>(dialog.FindName("EndPageTextBox")).Text);
            }
            finally { dialog.Close(); }
        });
    }

    [Fact]
    public void PdfToJpgCommand_CancelledFolder_IsNoOp()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var originalHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(fixture.Path)));
                var resultTask = Assert.IsAssignableFrom<Task<bool>>(
                    InvokeByName(window, "TryExportPdfToJpgAsync", string.Empty));

                Assert.False(await resultTask);
                Assert.Equal(originalHash, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(fixture.Path))));
                Assert.Contains("cancel", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
                Assert.False(ReadBoolField(window, "_utilityExecuting"));
                Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.jpg", SearchOption.AllDirectories));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void CompressionCommand_CancelledDestination_IsNoOp()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var original = File.ReadAllBytes(fixture.Path);
                var resultTask = Assert.IsAssignableFrom<Task<bool>>(
                    InvokeByName(window, "TryRunPdfCompressionUtilityAsync", string.Empty));

                Assert.False(await resultTask);
                Assert.Equal(original, File.ReadAllBytes(fixture.Path));
                Assert.Contains("cancel", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
                Assert.False(ReadBoolField(window, "_utilityExecuting"));
                Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*-comprimido.pdf"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void CompressionCommand_AlreadyExecuting_IsRejected()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                OrganizeWindowTestHost.SetField(window, "_utilityExecuting", true);
                var destination = Path.Combine(fixture.DirectoryPath, "must-not-exist.pdf");
                var resultTask = Assert.IsAssignableFrom<Task<bool>>(
                    InvokeByName(window, "TryRunPdfCompressionUtilityAsync", destination));

                Assert.False(await resultTask);
                Assert.False(File.Exists(destination));
                Assert.True(ReadBoolField(window, "_utilityExecuting"));
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
                var original = File.ReadAllBytes(fixture.Path);
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
    public void WatermarkCommand_SaveAsCopy_MaterializesAndReopens()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var original = File.ReadAllBytes(fixture.Path);
                var destination = Path.Combine(fixture.DirectoryPath, "watermark-output.pdf");
                var request = CreateRequest(
                    "WatermarkUtilityRequest",
                    new Dictionary<string, object?>
                    {
                        ["text"] = "SG WATERMARK",
                        ["fontSize"] = 24d,
                        ["opacity"] = 0.5d,
                        ["rotationDegrees"] = -25d,
                        ["offsetXPoints"] = 0d,
                        ["offsetYPoints"] = 0d,
                        ["startPageNumber"] = 1,
                        ["endPageNumber"] = 2,
                        ["useCurrentPage"] = false,
                        ["background"] = false,
                        ["anchorName"] = "Center",
                        ["red"] = (byte)40,
                        ["green"] = (byte)60,
                        ["blue"] = (byte)80
                    });

                Assert.True((bool)InvokeByName(window, "TryRunWatermarkUtility", request, destination)!);
                Assert.Equal(original, File.ReadAllBytes(fixture.Path));
                using var output = PdfDocumentSession.Open(destination);
                Assert.NotEmpty(output.FindTextOnPage(0, "SG WATERMARK"));
                Assert.NotEmpty(output.FindTextOnPage(1, "SG WATERMARK"));
                Assert.Contains("guardada", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
                Assert.False(ReadBoolField(window, "_utilityExecuting"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void PageNumberingCommand_SaveAsCopy_MaterializesBatesSequenceAndReopens()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(3);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var original = File.ReadAllBytes(fixture.Path);
                var destination = Path.Combine(fixture.DirectoryPath, "bates-output.pdf");
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

                Assert.True((bool)InvokeByName(window, "TryRunPageNumberingUtility", request, destination)!);
                Assert.Equal(original, File.ReadAllBytes(fixture.Path));
                using var output = PdfDocumentSession.Open(destination);
                Assert.NotEmpty(output.FindTextOnPage(0, "SG-001001"));
                Assert.NotEmpty(output.FindTextOnPage(1, "SG-001002"));
                Assert.NotEmpty(output.FindTextOnPage(2, "SG-001003"));
                Assert.Contains("guardada", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
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
