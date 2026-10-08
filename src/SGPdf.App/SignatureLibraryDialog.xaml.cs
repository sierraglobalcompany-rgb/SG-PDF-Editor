using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Sign;

namespace SGPdf.App;

internal sealed record SignatureLibraryDialogItem(
    Guid Id,
    string DisplayName,
    BitmapSource? Thumbnail,
    bool IsAvailable,
    string? UnavailableText);

public partial class SignatureLibraryDialog : Window
{
    private readonly SignatureLibraryStore _store;
    private readonly SignatureAsset? _selectedPlacementAsset;
    private bool _libraryAvailable = true;
    private bool _accepted;
    private Func<string, string?, string?> _promptName;
    private Func<string, bool> _confirmDelete;

    internal SignatureLibraryDialog(
        SignatureLibraryStore store,
        SignatureAsset? selectedPlacementAsset)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _selectedPlacementAsset = selectedPlacementAsset;

        InitializeComponent();

        _promptName = (title, initial) => SignatureNameDialog.Prompt(this, title, initial);
        _confirmDelete = displayName =>
            MessageBox.Show(
                this,
                $"¿Eliminar la firma '{displayName}' de la biblioteca local?",
                "Eliminar firma",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes;

        SignatureLibraryItemsList.SelectionChanged += (_, _) => UpdateButtonStates();
        SignatureLibrarySaveSelectedButton.Click += (_, _) => SaveSelectedPlacementAsset();
        SignatureLibraryRenameButton.Click += (_, _) => RenameSelected();
        SignatureLibraryDeleteButton.Click += (_, _) => DeleteSelected();
        SignatureLibraryUseButton.Click += (_, _) => UseSelected();
        SignatureLibraryCloseButton.Click += (_, _) => CloseWithoutSelection();

        RefreshItems();
    }

    internal SignatureAsset? SelectedAsset { get; private set; }

    internal static SignatureAsset? Open(
        Window owner,
        SignatureAsset? selectedPlacementAsset)
    {
        ArgumentNullException.ThrowIfNull(owner);
        var dialog = new SignatureLibraryDialog(
            new SignatureLibraryStore(),
            selectedPlacementAsset)
        {
            Owner = owner
        };

        return dialog.ShowDialog() == true ? dialog.SelectedAsset : null;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_accepted)
            SelectedAsset = null;
        base.OnClosing(e);
    }

    private void RefreshItems(Guid? selectId = null)
    {
        _libraryAvailable = true;

        try
        {
            var metadata = _store.Load();
            var rows = new List<SignatureLibraryDialogItem>(metadata.Count);
            foreach (var item in metadata)
            {
                try
                {
                    var asset = SignaturePngLoader.Load(Path.Combine(_store.RootDirectory, item.FileName));
                    rows.Add(new SignatureLibraryDialogItem(
                        item.Id,
                        item.DisplayName,
                        CreateThumbnail(asset),
                        true,
                        null));
                }
                catch (Exception ex) when (IsItemLoadFailure(ex))
                {
                    rows.Add(new SignatureLibraryDialogItem(
                        item.Id,
                        item.DisplayName,
                        null,
                        false,
                        $"No disponible: {ex.Message}"));
                }
            }

            SignatureLibraryItemsList.ItemsSource = rows;
            SignatureLibraryEmptyText.Text = "No hay firmas guardadas.";
            SignatureLibraryEmptyText.Visibility = rows.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (selectId is Guid id)
            {
                SignatureLibraryItemsList.SelectedItem = rows.FirstOrDefault(row => row.Id == id);
            }
            else
            {
                SignatureLibraryItemsList.SelectedIndex = -1;
            }
        }
        catch (SignatureLibraryUnavailableException ex)
        {
            _libraryAvailable = false;
            SignatureLibraryItemsList.ItemsSource = Array.Empty<SignatureLibraryDialogItem>();
            SignatureLibraryEmptyText.Text = "La biblioteca no está disponible.";
            SignatureLibraryEmptyText.Visibility = Visibility.Visible;
            SignatureLibraryStatusText.Text = ex.Message;
        }

        UpdateButtonStates();
    }

    private void SaveSelectedPlacementAsset()
    {
        if (!_libraryAvailable || _selectedPlacementAsset is null)
            return;

        var name = _promptName("Guardar firma", null);
        if (name is null)
            return;

        try
        {
            var item = _store.Add(name, _selectedPlacementAsset);
            RefreshItems(item.Id);
            SignatureLibraryStatusText.Text = "Firma guardada.";
        }
        catch (Exception ex) when (IsMutationFailure(ex))
        {
            SignatureLibraryStatusText.Text = ex.Message;
            UpdateButtonStates();
        }
    }

    private void RenameSelected()
    {
        if (!_libraryAvailable || SelectedRow() is not { } row)
            return;

        var name = _promptName("Renombrar firma", row.DisplayName);
        if (name is null)
            return;

        try
        {
            var renamed = _store.Rename(row.Id, name);
            RefreshItems(renamed.Id);
            SignatureLibraryStatusText.Text = "Firma renombrada.";
        }
        catch (Exception ex) when (IsMutationFailure(ex))
        {
            SignatureLibraryStatusText.Text = ex.Message;
            UpdateButtonStates();
        }
    }

    private void DeleteSelected()
    {
        if (!_libraryAvailable || SelectedRow() is not { } row)
            return;
        if (!_confirmDelete(row.DisplayName))
            return;

        try
        {
            var result = _store.Delete(row.Id);
            RefreshItems();
            SignatureLibraryStatusText.Text = result.FileCleanupSucceeded
                ? "Firma eliminada."
                : result.Warning ?? "La firma se eliminó, pero el archivo PNG no pudo limpiarse.";
        }
        catch (Exception ex) when (IsMutationFailure(ex))
        {
            SignatureLibraryStatusText.Text = ex.Message;
            UpdateButtonStates();
        }
    }

    private void UseSelected()
    {
        if (!_libraryAvailable || SelectedRow() is not { IsAvailable: true } row)
            return;

        try
        {
            SelectedAsset = _store.LoadAsset(row.Id);
            _accepted = true;
            SignatureLibraryStatusText.Text = string.Empty;
            if (IsVisible)
                DialogResult = true;
        }
        catch (Exception ex) when (IsItemLoadFailure(ex) || ex is KeyNotFoundException)
        {
            SelectedAsset = null;
            _accepted = false;
            RefreshItems(row.Id);
            SignatureLibraryStatusText.Text = ex.Message;
        }
    }

    private void CloseWithoutSelection()
    {
        _accepted = false;
        SelectedAsset = null;
        if (IsVisible)
            DialogResult = false;
    }

    private SignatureLibraryDialogItem? SelectedRow()
        => SignatureLibraryItemsList.SelectedItem as SignatureLibraryDialogItem;

    private void UpdateButtonStates()
    {
        var row = SelectedRow();
        SignatureLibraryUseButton.IsEnabled = _libraryAvailable && row?.IsAvailable == true;
        SignatureLibrarySaveSelectedButton.IsEnabled = _libraryAvailable && _selectedPlacementAsset is not null;
        SignatureLibraryRenameButton.IsEnabled = _libraryAvailable && row is not null;
        SignatureLibraryDeleteButton.IsEnabled = _libraryAvailable && row is not null;
        SignatureLibraryCloseButton.IsEnabled = true;
    }

    private static BitmapSource CreateThumbnail(SignatureAsset asset)
    {
        var bitmap = BitmapSource.Create(
            asset.PixelWidth,
            asset.PixelHeight,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            asset.BgraPixels.ToArray(),
            asset.Stride);
        bitmap.Freeze();
        return bitmap;
    }

    private static bool IsItemLoadFailure(Exception ex)
        => ex is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or OverflowException
            or OutOfMemoryException;

    private static bool IsMutationFailure(Exception ex)
        => ex is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or KeyNotFoundException
            or OverflowException;
}
