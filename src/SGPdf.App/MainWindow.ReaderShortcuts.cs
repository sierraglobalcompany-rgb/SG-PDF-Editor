using System.IO;
using System.Windows;
using System.Windows.Controls;
using SGPdf.App.Features.Reader;

namespace SGPdf.App;

public partial class MainWindow
{
    private RecentPdfStore _recentPdfStore = new();

    private void RecentFilesMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        => RebuildRecentFilesMenu();

    private void RebuildRecentFilesMenu()
    {
        var entries = _recentPdfStore.Load();
        RecentFilesMenuItem.Items.Clear();

        if (entries.Count == 0)
        {
            RecentFilesMenuItem.Items.Add(new MenuItem
            {
                Header = "(Sin archivos recientes)",
                IsEnabled = false
            });
        }
        else
        {
            foreach (var entry in entries)
            {
                var item = new MenuItem
                {
                    Header = Path.GetFileName(entry.FullPath),
                    ToolTip = entry.FullPath,
                    Tag = entry.FullPath
                };
                item.Click += RecentFileMenuItem_Click;
                RecentFilesMenuItem.Items.Add(item);
            }

            RecentFilesMenuItem.Items.Add(new Separator());
        }

        ClearRecentFilesMenuItem.IsEnabled = entries.Count > 0;
        RecentFilesMenuItem.Items.Add(ClearRecentFilesMenuItem);
    }

    private async void RecentFileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string path })
            return;

        var opened = await TryOpenPdfPathAsync(path);
        if (!opened)
        {
            try
            {
                if (!File.Exists(path))
                    _recentPdfStore.Remove(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                // The user explicitly selected the path. A failed cleanup must never disturb the open document.
            }
        }

        RebuildRecentFilesMenu();
    }

    private void ClearRecentFilesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _recentPdfStore.Clear();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = "No se pudo limpiar la lista de archivos recientes.";
        }

        RebuildRecentFilesMenu();
    }

    private void TryRecordRecentPdf(string path)
    {
        try
        {
            _recentPdfStore.RecordSuccessfulOpen(path, DateTimeOffset.UtcNow);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            // Recents are optional metadata. A successful PDF open remains successful if persistence fails.
        }
    }
}
