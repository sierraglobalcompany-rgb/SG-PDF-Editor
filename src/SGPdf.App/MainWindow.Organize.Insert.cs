using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SGPdf.App.Features.Organize;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow
{
    private Func<string?> _selectOrganizePdfSource = static () =>
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar PDF para insertar",
            Filter = "Archivo PDF (*.pdf)|*.pdf",
            CheckFileExists = true,
            Multiselect = false
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    };

    private bool TryInsertOrganizePdf()
    {
        if (!CanMutateOrganizePlan())
            return false;

        var sourcePath = _selectOrganizePdfSource();
        if (string.IsNullOrWhiteSpace(sourcePath))
            return false;

        return TryAddOrganizeCandidate(sourcePath, candidate =>
        {
            var choice = ShowOrganizeInsertChoice(
                candidate.Source.OriginalPageCount,
                _organizeSelection.SelectedItemIds.Count > 0);
            if (choice is null)
                return null;

            var pageIndices = choice.Value.AllPages
                ? Enumerable.Range(0, candidate.Source.OriginalPageCount).ToArray()
                : OrganizePageRangeParser.Parse(choice.Value.RangeExpression, candidate.Source.OriginalPageCount);
            var insertionIndex = ResolveOrganizeInsertionIndex(choice.Value.Placement);
            return new OrganizeInsertPlanRequest(pageIndices, insertionIndex);
        });
    }

    private bool TryMergeOrganizePdf()
    {
        if (!CanMutateOrganizePlan())
            return false;

        var sourcePath = _selectOrganizePdfSource();
        return string.IsNullOrWhiteSpace(sourcePath)
            ? false
            : TryMergeOrganizePdfCandidate(sourcePath);
    }

    private bool TryInsertOrganizePdfCandidate(
        string sourcePath,
        string? pageExpression,
        int insertionIndex)
        => TryAddOrganizeCandidate(sourcePath, candidate =>
        {
            var pageIndices = pageExpression is null
                ? Enumerable.Range(0, candidate.Source.OriginalPageCount).ToArray()
                : OrganizePageRangeParser.Parse(pageExpression, candidate.Source.OriginalPageCount);
            return new OrganizeInsertPlanRequest(pageIndices, insertionIndex);
        });

    private bool TryMergeOrganizePdfCandidate(string sourcePath)
        => TryAddOrganizeCandidate(sourcePath, candidate =>
            new OrganizeInsertPlanRequest(
                Enumerable.Range(0, candidate.Source.OriginalPageCount).ToArray(),
                _organizePlan?.Pages.Count ?? 0));

    private bool TryAddOrganizeCandidate(
        string sourcePath,
        Func<OrganizeCandidateSource, OrganizeInsertPlanRequest?> buildRequest)
    {
        ArgumentNullException.ThrowIfNull(buildRequest);
        if (!CanMutateOrganizePlan() || _organizePlan is null)
            return false;

        var basePlan = _organizePlan;
        _organizeMaterializing = true;
        UpdateOrganizeCommandAvailability();
        try
        {
            var candidate = PrepareOrganizeCandidateSource(sourcePath, CancellationToken.None);
            if (candidate is null)
                return false;

            var request = buildRequest(candidate);
            if (request is null)
            {
                StatusText.Text = "Inserción cancelada. El documento actual no cambió.";
                return false;
            }

            var next = OrganizePlanOperations.InsertSourcePages(
                basePlan,
                candidate.Source,
                request.Value.SourcePageIndices,
                request.Value.InsertionIndex);

            if (!ReferenceEquals(_organizePlan, basePlan))
                throw new InvalidOperationException("El plan cambió mientras se preparaba el PDF secundario.");

            var previousIds = basePlan.Pages.Select(page => page.ItemId).ToHashSet();
            var insertedIds = next.Pages
                .Select(page => page.ItemId)
                .Where(itemId => !previousIds.Contains(itemId))
                .ToArray();

            _organizeSourcePageSizes[candidate.Source.SourceId] = candidate.PageSizes;
            try
            {
                _organizeSelection.Clear();
                if (insertedIds.Length > 0)
                {
                    _organizeSelection.SelectSingle(insertedIds[0]);
                    for (var index = 1; index < insertedIds.Length; index++)
                        _organizeSelection.Toggle(insertedIds[index]);
                }

                ApplyTask6OrganizePlan(next);
            }
            catch
            {
                _organizeSourcePageSizes.Remove(candidate.Source.SourceId);
                throw;
            }

            StatusText.Text = candidate.HasWarnings
                ? $"Se agregaron {insertedIds.Length} página(s). Las advertencias de preservación se revisarán al guardar."
                : $"Se agregaron {insertedIds.Length} página(s) al plan. Usa Guardar como... para materializar los cambios.";
            return true;
        }
        catch (ArgumentException ex)
        {
            StatusText.Text = $"No se insertó el PDF: {ex.Message}";
            return false;
        }
        catch (PdfDocumentOpenException ex)
            when (ex.Error == PdfDocumentOpenError.PasswordRequiredOrIncorrect)
        {
            StatusText.Text = "No se insertó el PDF: los archivos protegidos con contraseña no son compatibles con ORGANIZAR.";
            return false;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se insertó el PDF: {ex.Message}";
            return false;
        }
        finally
        {
            _organizeMaterializing = false;
            UpdateOrganizeCommandAvailability();
        }
    }

    private OrganizeCandidateSource? PrepareOrganizeCandidateSource(
        string sourcePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("La ruta del PDF es obligatoria.", nameof(sourcePath));

        cancellationToken.ThrowIfCancellationRequested();
        using var candidateSession = PdfDocumentSession.Open(sourcePath);
        cancellationToken.ThrowIfCancellationRequested();

        var pageCount = candidateSession.PageCount;
        var pageSizes = candidateSession.GetPageSizes(cancellationToken).ToArray();
        if (pageSizes.Length != pageCount)
            throw new InvalidOperationException("No se pudo obtener la geometría completa del PDF secundario.");

        var preflight = _inspectCurrentOrganizePreflight(candidateSession, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!preflight.CanProceed)
        {
            StatusText.Text = "No se insertó el PDF: el origen contiene una estructura bloqueada para ORGANIZAR.";
            return null;
        }

        var source = OrganizeSource.Capture(candidateSession.FilePath, pageCount);
        return new OrganizeCandidateSource(
            source,
            pageSizes,
            preflight.RequiresWarningConfirmation);
    }

    private int ResolveOrganizeInsertionIndex(OrganizeInsertPlacement placement)
    {
        if (_organizePlan is null)
            throw new InvalidOperationException("No hay un plan de organización activo.");

        if (_organizeSelection.SelectedItemIds.Count == 0 || placement == OrganizeInsertPlacement.Append)
            return _organizePlan.Pages.Count;

        var selectedSet = _organizeSelection.SelectedItemIds;
        var selectedIndices = _organizePlan.Pages
            .Select((page, index) => (page, index))
            .Where(entry => selectedSet.Contains(entry.page.ItemId))
            .Select(entry => entry.index)
            .ToArray();
        if (selectedIndices.Length == 0)
            return _organizePlan.Pages.Count;

        return placement == OrganizeInsertPlacement.BeforeSelection
            ? selectedIndices[0]
            : selectedIndices[^1] + 1;
    }

    private OrganizeInsertDialogChoice? ShowOrganizeInsertChoice(int pageCount, bool hasSelection)
    {
        var dialog = new Window
        {
            Title = "Insertar páginas",
            Width = 430,
            Height = 285,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = IsVisible ? this : null,
            ShowInTaskbar = false
        };

        var root = new StackPanel { Margin = new Thickness(18) };
        root.Children.Add(new TextBlock
        {
            Text = $"El PDF tiene {pageCount} página(s).",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10)
        });

        var allPages = new CheckBox
        {
            Content = "Todas las páginas",
            IsChecked = true,
            Margin = new Thickness(0, 0, 0, 8)
        };
        root.Children.Add(allPages);

        root.Children.Add(new TextBlock { Text = "Páginas o rangos (ej. 1,3,5-7):" });
        var range = new TextBox
        {
            IsEnabled = false,
            Margin = new Thickness(0, 4, 0, 12)
        };
        allPages.Checked += (_, _) => range.IsEnabled = false;
        allPages.Unchecked += (_, _) => range.IsEnabled = true;
        root.Children.Add(range);

        root.Children.Add(new TextBlock { Text = "Posición:" });
        var placement = new ComboBox { Margin = new Thickness(0, 4, 0, 14) };
        if (hasSelection)
        {
            placement.Items.Add(new ComboBoxItem { Content = "Antes de la selección", Tag = OrganizeInsertPlacement.BeforeSelection });
            placement.Items.Add(new ComboBoxItem { Content = "Después de la selección", Tag = OrganizeInsertPlacement.AfterSelection });
        }
        placement.Items.Add(new ComboBoxItem { Content = "Al final", Tag = OrganizeInsertPlacement.Append });
        placement.SelectedIndex = hasSelection ? 1 : 0;
        root.Children.Add(placement);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var cancel = new Button
        {
            Content = "Cancelar",
            MinWidth = 90,
            Margin = new Thickness(0, 0, 8, 0),
            IsCancel = true
        };
        var accept = new Button
        {
            Content = "Insertar",
            MinWidth = 90,
            IsDefault = true
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(accept);
        root.Children.Add(buttons);
        dialog.Content = root;

        OrganizeInsertDialogChoice? result = null;
        accept.Click += (_, _) =>
        {
            if (allPages.IsChecked != true && string.IsNullOrWhiteSpace(range.Text))
            {
                MessageBox.Show(
                    dialog,
                    "Escribe una página o rango válido, o selecciona Todas las páginas.",
                    "SG PDF Editor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var selectedPlacement = placement.SelectedItem is ComboBoxItem item && item.Tag is OrganizeInsertPlacement value
                ? value
                : OrganizeInsertPlacement.Append;
            result = new OrganizeInsertDialogChoice(
                allPages.IsChecked == true,
                range.Text,
                selectedPlacement);
            dialog.DialogResult = true;
        };

        return dialog.ShowDialog() == true ? result : null;
    }

    private sealed record OrganizeCandidateSource(
        OrganizeSource Source,
        IReadOnlyList<PdfPageSize> PageSizes,
        bool HasWarnings);

    private readonly record struct OrganizeInsertPlanRequest(
        IReadOnlyList<int> SourcePageIndices,
        int InsertionIndex);

    private readonly record struct OrganizeInsertDialogChoice(
        bool AllPages,
        string RangeExpression,
        OrganizeInsertPlacement Placement);

    private enum OrganizeInsertPlacement
    {
        BeforeSelection,
        AfterSelection,
        Append
    }
}
