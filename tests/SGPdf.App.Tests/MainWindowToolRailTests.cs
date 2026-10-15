using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowToolRailTests
{
    private static readonly string[] ExpectedActionOrder =
    [
        "read", "edit", "comment", "sign", "organize",
        "forms", "ocr", "redact", "compare", "convert", "protect"
    ];

    [Fact]
    public void DocumentRail_Is96Pixels_OrderedAccessible_AndHasOneSelectedItem()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                RaiseLoaded(window);

                var rail = Assert.IsType<Border>(window.FindName("DocumentToolRail"));
                Assert.Equal(96d, rail.Width);

                var items = Assert.IsType<StackPanel>(window.FindName("DocumentToolRailItems"));
                var buttons = items.Children.OfType<Button>().ToArray();
                Assert.Equal(ExpectedActionOrder, buttons.Select(button => Assert.IsType<string>(button.Tag)));
                Assert.All(buttons, button =>
                {
                    Assert.True(button.Focusable);
                    Assert.False(string.IsNullOrWhiteSpace(button.Content?.ToString()));
                    Assert.Equal(button.Content?.ToString(), AutomationProperties.GetName(button));
                });

                var selectedStyle = window.FindResource("SgRailItemSelectedStyle");
                Assert.Single(buttons.Where(button => ReferenceEquals(button.Style, selectedStyle)));
            }
            finally { window.Close(); }
        });
    }

    [Fact]
    public void Router_ExposesOnlyFiveF13Actions_AndIgnoresFutureActions()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                RaiseLoaded(window);
                var method = GetRouter();

                SetField(window, "_organizeModeActive", true);
                SetField(window, "_organizePlanDirty", false);
                method.Invoke(window, ["forms"]);

                Assert.True(GetField<bool>(window, "_organizeModeActive"));
            }
            finally { window.Close(); }
        });
    }

    [Fact]
    public void Router_DoesNotBypassOrganizeDirtyGuard()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                RaiseLoaded(window);
                SetField(window, "_organizeModeActive", true);
                SetField(window, "_organizePlanDirty", true);
                SetField(window, "_confirmDiscardOrganizeChanges", (Func<bool>)(() => false));

                GetRouter().Invoke(window, ["read"]);

                Assert.True(GetField<bool>(window, "_organizeModeActive"));
                Assert.True(GetField<bool>(window, "_organizePlanDirty"));
            }
            finally { window.Close(); }
        });
    }

    [Fact]
    public void Router_DoesNotBypassCommentMaterializationGuard()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                RaiseLoaded(window);
                SetField(window, "_commentModeActive", true);
                SetField(window, "_commentMaterializing", true);

                GetRouter().Invoke(window, ["read"]);

                Assert.True(GetField<bool>(window, "_commentModeActive"));
                Assert.True(GetField<bool>(window, "_commentMaterializing"));
            }
            finally { window.Close(); }
        });
    }

    private static MethodInfo GetRouter()
    {
        var method = typeof(MainWindow).GetMethod("HandleToolAction", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method.ReturnType);
        Assert.Equal([typeof(string)], method.GetParameters().Select(parameter => parameter.ParameterType));
        return method;
    }

    private static void RaiseLoaded(MainWindow window)
        => window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

    private static void SetField(MainWindow window, string name, object value)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(window, value);
    }

    private static T GetField<T>(MainWindow window, string name)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<T>(field.GetValue(window));
    }

    private static void RunInSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
