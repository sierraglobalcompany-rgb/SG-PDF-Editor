using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using SGPdf.App.Features.Ocr;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OcrDialogTests
{
    private const string DialogTypeName = "SGPdf.App.Features.Ocr.OcrDialog";

    [Fact]
    public void DefaultsAndChoices_AreSpanishAutomaticAndOnlyApprovedOptions()
    {
        OrganizeWindowTestHost.RunInSta(() =>
        {
            var dialog = CreateDialog(pageCount: 4, currentPageIndex: 1);
            try
            {
                var language = Element<ComboBox>(dialog, "OcrLanguageComboBox");
                var scope = Element<ComboBox>(dialog, "OcrScopeComboBox");

                Assert.Equal("Español", SelectedText(language));
                Assert.Equal(new[] { "Español", "Inglés", "Español + Inglés" }, ItemTexts(language));
                Assert.Equal("Automático", SelectedText(scope));
                Assert.Equal(new[] { "Automático", "Página actual", "Rango" }, ItemTexts(scope));

                Assert.Null(dialog.FindName("OcrDpiTextBox"));
                Assert.Null(dialog.FindName("OcrPsmComboBox"));
                Assert.Null(dialog.FindName("OcrOemComboBox"));
            }
            finally
            {
                dialog.Close();
            }
        });
    }

    [Fact]
    public void RangeValidation_AppliesOnlyToRange_AndMapsOneBasedPagesToRequest()
    {
        OrganizeWindowTestHost.RunInSta(() =>
        {
            var dialog = CreateDialog(pageCount: 5, currentPageIndex: 2);
            try
            {
                var scope = Element<ComboBox>(dialog, "OcrScopeComboBox");
                var start = Element<TextBox>(dialog, "OcrRangeStartTextBox");
                var end = Element<TextBox>(dialog, "OcrRangeEndTextBox");

                start.Text = "5";
                end.Text = "2";
                scope.SelectedIndex = 0;
                Assert.True(TryBuildRequest(dialog, out var automatic, out _));
                Assert.NotNull(automatic);
                Assert.Equal(OcrPageScopeKind.Automatic, automatic!.Scope.Kind);

                scope.SelectedIndex = 2;
                Assert.False(TryBuildRequest(dialog, out _, out var invalidError));
                Assert.False(string.IsNullOrWhiteSpace(invalidError));

                start.Text = "2";
                end.Text = "4";
                Assert.True(TryBuildRequest(dialog, out var range, out var validError));
                Assert.True(string.IsNullOrWhiteSpace(validError));
                Assert.NotNull(range);
                Assert.Equal(OcrPageScopeKind.Range, range!.Scope.Kind);
                Assert.Equal(1, range.Scope.StartPageIndex);
                Assert.Equal(3, range.Scope.EndPageIndex);
            }
            finally
            {
                dialog.Close();
            }
        });
    }

    [Fact]
    public void ProgressAndCancelContract_ArePresentAndProgressShowsPageTotal()
    {
        OrganizeWindowTestHost.RunInSta(() =>
        {
            var dialog = CreateDialog(pageCount: 3, currentPageIndex: 0);
            try
            {
                Assert.NotNull(dialog.FindName("OcrProgressBar"));
                Assert.NotNull(dialog.FindName("OcrCancelButton"));
                var progressText = Element<TextBlock>(dialog, "OcrProgressText");

                Invoke(dialog, "UpdateProgress", new OcrProgress(2, 3, 1, OcrProgressStage.Recognizing));

                Assert.Contains("2", progressText.Text, StringComparison.Ordinal);
                Assert.Contains("3", progressText.Text, StringComparison.Ordinal);
            }
            finally
            {
                dialog.Close();
            }
        });
    }

    private static Window CreateDialog(int pageCount, int currentPageIndex)
    {
        var type = typeof(MainWindow).Assembly.GetType(DialogTypeName);
        Assert.NotNull(type);
        var instance = Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { pageCount, currentPageIndex },
            culture: null);
        return Assert.IsAssignableFrom<Window>(instance);
    }

    private static T Element<T>(Window dialog, string name) where T : FrameworkElement
        => Assert.IsType<T>(dialog.FindName(name));

    private static string SelectedText(ComboBox combo)
        => ItemText(Assert.IsType<ComboBoxItem>(combo.SelectedItem));

    private static string[] ItemTexts(ComboBox combo)
        => combo.Items.Cast<object>()
            .Select(item => ItemText(Assert.IsType<ComboBoxItem>(item)))
            .ToArray();

    private static string ItemText(ComboBoxItem item)
        => Assert.IsType<string>(item.Content);

    private static bool TryBuildRequest(Window dialog, out OcrRequest? request, out string? error)
    {
        var method = dialog.GetType().GetMethod("TryBuildRequest", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        object?[] args = { null, null };
        var result = Assert.IsType<bool>(method.Invoke(dialog, args));
        request = args[0] as OcrRequest;
        error = args[1] as string;
        return result;
    }

    private static object? Invoke(Window dialog, string name, params object?[] args)
    {
        var method = dialog.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return method.Invoke(dialog, args);
    }
}
