using System.Windows;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        PdfiumRuntime.Shutdown();
        base.OnExit(e);
    }
}
