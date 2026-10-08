using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfPasswordDialogTests
{
    [Fact]
    public void PasswordDialog_UsesMaskedPasswordBox()
    {
        RunInSta(() =>
        {
            var dialog = new PdfPasswordDialog("protected.pdf");
            try
            {
                var password = Assert.IsType<PasswordBox>(dialog.FindName("PdfPasswordBox"));
                Assert.Equal('\u25CF', password.PasswordChar);
                Assert.Null(dialog.PasswordValue);
            }
            finally { dialog.Close(); }
        });
    }

    [Fact]
    public void PasswordPrompt_CancelReturnsNull()
    {
        RunInSta(() =>
        {
            var dialog = new PdfPasswordDialog("protected.pdf");
            dialog.Show();
            try
            {
                var cancel = Assert.IsType<Button>(dialog.FindName("PdfPasswordCancelButton"));
                cancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, cancel));
                Assert.Null(dialog.PasswordValue);
                Assert.False(dialog.DialogResult ?? false);
            }
            finally
            {
                if (dialog.IsVisible)
                    dialog.Close();
            }
        });
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
