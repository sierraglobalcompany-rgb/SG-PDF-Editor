using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SGPdf.App.Features.Conversions;

public partial class ImageToPdfOrderDialog : Window
{
    private readonly ObservableCollection<ImageOrderItem> _items;

    internal ImageToPdfOrderDialog(IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (paths.Count == 0)
            throw new ArgumentException("Selecciona al menos una imagen.", nameof(paths));

        InitializeComponent();
        _items = new ObservableCollection<ImageOrderItem>(paths.Select(path =>
            new ImageOrderItem(path, Path.GetFileName(path), Path.GetDirectoryName(path) ?? string.Empty)));
        ImageOrderListBox.ItemsSource = _items;
        ImageOrderListBox.SelectedIndex = 0;
        Loaded += (_, _) => ImageOrderListBox.Focus();
        UpdateMoveButtons();
    }

    internal IReadOnlyList<string> OrderedPaths => _items.Select(item => item.FullPath).ToArray();

    private void MoveUp_Click(object sender, RoutedEventArgs e)
        => MoveSelected(-1);

    private void MoveDown_Click(object sender, RoutedEventArgs e)
        => MoveSelected(1);

    private void ImageOrderListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => UpdateMoveButtons();

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count == 0)
            return;
        DialogResult = true;
    }

    private void MoveSelected(int offset)
    {
        int current = ImageOrderListBox.SelectedIndex;
        int next = current + offset;
        if (current < 0 || next < 0 || next >= _items.Count)
            return;

        _items.Move(current, next);
        ImageOrderListBox.SelectedIndex = next;
        ImageOrderListBox.ScrollIntoView(ImageOrderListBox.SelectedItem);
        UpdateMoveButtons();
    }

    private void UpdateMoveButtons()
    {
        int selected = ImageOrderListBox.SelectedIndex;
        MoveUpButton.IsEnabled = selected > 0;
        MoveDownButton.IsEnabled = selected >= 0 && selected < _items.Count - 1;
    }

    private sealed record ImageOrderItem(string FullPath, string FileName, string Directory);
}
