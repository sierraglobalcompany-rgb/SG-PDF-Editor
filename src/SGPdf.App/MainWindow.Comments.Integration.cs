using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool CommentShortcutGuardRegistered = RegisterCommentShortcutGuard();
    private static readonly bool CommentRecentGuardRegistered = RegisterCommentRecentGuard();

    private static bool RegisterCommentShortcutGuard()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            Keyboard.PreviewKeyDownEvent,
            new KeyEventHandler(CommentShortcutGuard_PreviewKeyDown),
            handledEventsToo: false);
        return true;
    }

    private static bool RegisterCommentRecentGuard()
    {
        EventManager.RegisterClassHandler(
            typeof(MenuItem),
            MenuItem.ClickEvent,
            new RoutedEventHandler(CommentRecentMenuItem_Click),
            handledEventsToo: false);
        return true;
    }

    private static void CommentShortcutGuard_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not MainWindow window || !window._commentModeActive)
            return;

        _ = CommentShortcutGuardRegistered;
        if (e.Key != Key.O || (Keyboard.Modifiers & ModifierKeys.Control) == 0)
            return;
        if (IsReaderShortcutEditableSource(e.OriginalSource as DependencyObject))
            return;

        if (!window.TryLeaveCommentModeWithGuard())
            e.Handled = true;
    }

    private static void CommentRecentMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _ = CommentRecentGuardRegistered;
        if (sender is not MenuItem { Tag: string } menuItem ||
            Window.GetWindow(menuItem) is not MainWindow window ||
            !window._commentModeActive)
        {
            return;
        }

        if (!window.TryLeaveCommentModeWithGuard())
            e.Handled = true;
    }
}
