using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Xml.Linq;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class HomeDashboardContractTests
{
    [Fact]
    public void Dashboard_XamlMatchesApprovedLocalHomeStructure()
    {
        var path = FindRepoFile("src", "SGPdf.App", "Shell", "HomeDashboard.xaml");
        var xaml = File.ReadAllText(path);
        var document = XDocument.Load(path);
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        var named = document.Descendants()
            .Select(element => (Element: element, Name: (string?)element.Attribute(x + "Name")))
            .Where(item => item.Name is not null)
            .ToDictionary(item => item.Name!, item => item.Element, StringComparer.Ordinal);

        Assert.Equal("96", (string?)named["HomeRail"].Attribute("Width"));
        Assert.Contains("HomeOpenPdfButton", named.Keys);
        Assert.Contains("HomeOpenZplButton", named.Keys);
        Assert.Contains("HomeToolsItemsControl", named.Keys);
        Assert.Contains("HomeRecentItemsControl", named.Keys);
        Assert.Contains("¡Bienvenido a SG PDF Editor!", xaml, StringComparison.Ordinal);
        Assert.Contains("local", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("offline", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Abrir PDF", xaml, StringComparison.Ordinal);
        Assert.Contains("Abrir ZPL", xaml, StringComparison.Ordinal);
        Assert.Contains("Configuración", xaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility=\"Disabled\"", xaml, StringComparison.Ordinal);
        Assert.Contains("WrapPanel", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void HomeRail_GeometryAndWrapping_KeepSpanishLabelsUnclipped()
    {
        var path = FindRepoFile("src", "SGPdf.App", "Shell", "HomeDashboard.xaml");
        var document = XDocument.Load(path);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        var rail = document.Descendants(presentation + "Border")
            .Single(element => (string?)element.Attribute(x + "Name") == "HomeRail");
        var railWidth = double.Parse((string?)rail.Attribute("Width") ?? "0", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(96d, railWidth);

        var railGrid = rail.Elements(presentation + "Grid").Single();
        var gridHorizontalMargin = HorizontalThicknessSum((string?)railGrid.Attribute("Margin"), 0d);

        var buttons = railGrid.Elements(presentation + "Button").ToArray();
        Assert.Equal(4, buttons.Length);

        foreach (var button in buttons)
        {
            var buttonMargin = HorizontalThicknessSum((string?)button.Attribute("Margin"), 12d);
            var buttonPadding = HorizontalThicknessSum((string?)button.Attribute("Padding"), 16d);
            var availableTextWidth = railWidth - 1d - gridHorizontalMargin - buttonMargin - buttonPadding;
            Assert.True(
                availableTextWidth >= 75d,
                $"Expected Home rail button '{button.Attribute("AutomationProperties.Name")?.Value}' to leave at least 75 DIPs for text, but left {availableTextWidth} DIPs.");

            var label = button.Descendants(presentation + "TextBlock").Single();
            Assert.Equal("Wrap", (string?)label.Attribute("TextWrapping"));
            Assert.Null(label.Attribute("TextTrimming"));
        }
    }

    private static double HorizontalThicknessSum(string? value, double defaultHorizontalSum)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultHorizontalSum;

        var parts = value.Split(',', StringSplitOptions.TrimEntries)
            .Select(part => double.Parse(part, System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();

        return parts.Length switch
        {
            1 => parts[0] * 2d,
            2 => parts[0] * 2d,
            4 => parts[0] + parts[2],
            _ => defaultHorizontalSum
        };
    }

    [Fact]
    public void Dashboard_DeclaresIntentOnlyHostingContract()
    {
        var type = GetDashboardType();

        AssertProperty(type, "Tools", "ToolDescriptor");
        AssertProperty(type, "RecentFiles", "RecentPdfEntry");
        AssertEvent(type, "OpenPdfRequested", typeof(Action));
        AssertEvent(type, "OpenZplRequested", typeof(Action));
        AssertEvent(type, "RecentPdfRequested", typeof(Action<string>));
        AssertEvent(type, "ToolRequested", typeof(Action<string>));
    }

    [Fact]
    public void Dashboard_NullActionNeverRaisesToolRequested_ButExecutableCardDoes()
    {
        RunInSta(() =>
        {
            var dashboardType = GetDashboardType();
            var dashboard = Activator.CreateInstance(dashboardType, nonPublic: true);
            Assert.NotNull(dashboard);

            string? requested = null;
            AddEventHandler(dashboardType, dashboard!, "ToolRequested", (Action<string>)(action => requested = action));

            var requestTool = dashboardType.GetMethod("RequestTool", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(requestTool);

            var home = ReadCatalog("Home");
            Type descriptorType = home[0].GetType();
            var constructor = descriptorType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: [typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(bool), typeof(string)],
                modifiers: null);
            Assert.NotNull(constructor);
            var disabled = constructor!.Invoke(["disabled", "Deshabilitada", "PDF", "No disponible.", "ShellIconEdit", null, false, "No disponible"]);
            requestTool.Invoke(dashboard, new[] { disabled });
            Assert.Null(requested);

            var imageToPdf = Assert.Single(home, item => ReadString(item, "Id") == "image-to-pdf");
            requestTool.Invoke(dashboard, new[] { imageToPdf });
            Assert.Equal("image-to-pdf", requested);

            requested = null;
            var zpl = Assert.Single(home, item => ReadString(item, "Id") == "labels-zpl");
            requestTool.Invoke(dashboard, new[] { zpl });
            Assert.Equal("open-zpl", requested);
        });
    }

    [Fact]
    public void HomeToolCards_PreserveThemeAwareSurfaceAndTitleContrastForEnabledAndDisabledStates()
    {
        var path = FindRepoFile("src", "SGPdf.App", "Shell", "HomeDashboard.xaml");
        var document = XDocument.Load(path);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        var itemsControl = document.Descendants(presentation + "ItemsControl")
            .Single(element => (string?)element.Attribute(x + "Name") == "HomeToolsItemsControl");
        var cardButton = itemsControl.Descendants(presentation + "Button").Single();

        var buttonStyle = cardButton.Element(presentation + "Button.Style")?.Element(presentation + "Style");
        Assert.NotNull(buttonStyle);

        var disabledTrigger = buttonStyle!.Descendants(presentation + "DataTrigger")
            .SingleOrDefault(trigger => (string?)trigger.Attribute("Value") == "{x:Null}");
        Assert.NotNull(disabledTrigger);
        Assert.Contains(
            disabledTrigger!.Elements(presentation + "Setter"),
            setter => (string?)setter.Attribute("Property") == "IsEnabled" && (string?)setter.Attribute("Value") == "False");

        var templateSetter = buttonStyle.Elements(presentation + "Setter")
            .SingleOrDefault(setter => (string?)setter.Attribute("Property") == "Template");
        Assert.NotNull(templateSetter);
        Assert.NotNull(templateSetter!.Descendants(presentation + "ContentPresenter").SingleOrDefault());

        var textBlocks = cardButton.Descendants(presentation + "TextBlock").ToArray();
        Assert.Equal(2, textBlocks.Length);
        Assert.Equal("{DynamicResource TextFillColorPrimaryBrush}", (string?)textBlocks[0].Attribute("Foreground"));
        Assert.Equal("{DynamicResource SgMutedTextStyle}", (string?)textBlocks[1].Attribute("Style"));
    }

    private static Type GetDashboardType()
        => typeof(MainWindow).Assembly.GetType("SGPdf.App.Shell.HomeDashboard", throwOnError: true)!;

    private static void AssertProperty(Type type, string name, string expectedElementType)
    {
        var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        Assert.Contains(expectedElementType, property.PropertyType.FullName ?? property.PropertyType.Name, StringComparison.Ordinal);
    }

    private static void AssertEvent(Type type, string name, Type expectedHandlerType)
    {
        var eventInfo = type.GetEvent(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(eventInfo);
        Assert.Equal(expectedHandlerType, eventInfo.EventHandlerType);
    }

    private static void AddEventHandler(Type type, object target, string name, Delegate handler)
    {
        var eventInfo = type.GetEvent(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(eventInfo);
        var addMethod = eventInfo.GetAddMethod(nonPublic: true);
        Assert.NotNull(addMethod);
        addMethod.Invoke(target, new object[] { handler });
    }

    private static IReadOnlyList<object> ReadCatalog(string propertyName)
    {
        var assembly = typeof(MainWindow).Assembly;
        var catalogType = assembly.GetType("SGPdf.App.Shell.ToolCatalog", throwOnError: true)!;
        var property = catalogType.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        var value = Assert.IsAssignableFrom<IEnumerable>(property.GetValue(null));
        return value.Cast<object>().ToArray();
    }

    private static string? ReadString(object value, string name)
        => (string?)value.GetType().GetProperty(name)?.GetValue(value);

    private static string FindRepoFile(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = segments.Aggregate(directory.FullName, Path.Combine);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Repository file not found: {Path.Combine(segments)}");
    }

    private static void RunInSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
