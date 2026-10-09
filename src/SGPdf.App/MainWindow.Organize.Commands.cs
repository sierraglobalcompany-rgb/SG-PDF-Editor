using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using SGPdf.App.Features.Organize;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow
{
    private const string OrganizeDragDataFormat = "SGPdf.OrganizePages";
    private static readonly bool OrganizeTask6LoadedHookRegistered = RegisterOrganizeTask6LoadedHook();

    private readonly OrganizeSelection _organizeSelection = new();
    private bool _organizeTask6UiInitialized;
    private bool _organizeMaterializing;
    private Point? _organizeDragStartPoint;

    private Func<string?> _selectOrganizePdfDestination = static () =>
    {
        var dialog = new SaveFileDialog
        {
            Title = "Guardar PDF organizado como",
            Filter = "Archivo PDF (*.pdf)|*.pdf",
            AddExtension = true,
            DefaultExt = ".pdf",
            OverwritePrompt = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    };

    private Func<OrganizePreflightResult, bool> _confirmOrganizeWarnings = static result =>
    {
        var warnings = result.Findings
            .Where(finding => finding.Severity == OrganizeFindingSeverity.Warning)
            .Select(finding => $"• {finding.Message}")
            .ToArray();
        var detail = warnings.Length == 0
            ? "Hay estructuras cuya preservación no está demostrada."
            : string.Join(Environment.NewLine, warnings);
        return MessageBox.Show(
            $"Antes de guardar la copia:\n\n{detail}\n\n¿Deseas continuar?",
            "SG PDF Editor",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
    };

    private Action<OrganizePlan, string, string, bool, CancellationToken> _saveOrganizeCopy =
        static (plan, activeSourcePath, destinationPath, warningsConfirmed, token) =>
            new PdfOrganizeWriter().SaveAsCopy(
                plan,
                activeSourcePath,
                destinationPath,
                warningsConfirmed,
                token);

    private static bool RegisterOrganizeTask6LoadedHook()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OrganizeTask6Host_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void OrganizeTask6Host_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window)
            return;

        window.InitializeOrganizeUi();
        window.InitializeOrganizeTask6Ui();
    }

    private void InitializeOrganizeTask6Ui()
    {
        if (_organizeTask6UiInitialized)
            return;

        _organizeTask6UiInitialized = true;
        _ = OrganizeTask6LoadedHookRegistered;

        var list = _organizePageList
            ?? throw new InvalidOperationException("La superficie ORGANIZAR no está inicializada.");
        var surface = _organizeSurface
            ?? throw new InvalidOperationException("La superficie ORGANIZAR no está inicializada.");

        list.AllowDrop = true;
        list.PreviewMouseLeftButtonDown += OrganizePageList_PreviewMouseLeftButtonDown;
        list.PreviewMouseMove += OrganizePageList_PreviewMouseMove;
        list.Drop += OrganizePageList_Drop;
        PreviewKeyDown += OrganizeTask6_PreviewKeyDown;

        Button("OrganizeRotateLeftButton").Click += (_, _) => RotateOrganizeSelection(-1);
        Button("OrganizeRotateRightButton").Click += (_, _) => RotateOrganizeSelection(1);
        Button("OrganizeDeleteButton").Click += (_, _) => DeleteOrganizeSelection();
        Button("OrganizeDuplicateButton").Click += (_, _) => DuplicateOrganizeSelection();
        Button("OrganizeSaveAsButton").Click += (_, _) => TrySaveOrganizePlan();

        var visibilityDescriptor = DependencyPropertyDescriptor.FromProperty(
            UIElement.VisibilityProperty,
            typeof(UIElement));
        visibilityDescriptor?.AddValueChanged(surface, (_, _) =>
        {
            if (surface.Visibility != Visibility.Visible)
                _organizeSelection.Clear();
            SyncOrganizeListSelection();
            UpdateOrganizeCommandAvailability();
        });

        UpdateOrganizeCommandAvailability();

        Button Button(string name)
            => FindName(name) as Button
               ?? throw new InvalidOperationException($"No se encontró el comando {name}.");
    }

    private void OrganizePageList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_organizeModeActive || _organizeMaterializing || _organizePlan is null)
            return;

        var container = FindOrganizeListBoxItem(e.OriginalSource as DependencyObject);
        if (container?.DataContext is not OrganizePageItem item)
            return;

        ApplyOrganizeSelectionClick(item.ItemId, Keyboard.Modifiers);
        _organizeDragStartPoint = e.GetPosition(_organizePageList);
        _organizePageList?.Focus();
        e.Handled = true;
    }

    private void OrganizePageList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_organizeModeActive || _organizeMaterializing ||
            e.LeftButton != MouseButtonState.Pressed ||
            _organizeDragStartPoint is not Point start ||
            _organizeSelection.SelectedItemIds.Count == 0 ||
            _organizePageList is null)
        {
            return;
        }

        var current = e.GetPosition(_organizePageList);
        if (Math.Abs(current.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        _organizeDragStartPoint = null;
        var data = new DataObject();
        data.SetData(OrganizeDragDataFormat, true);
        DragDrop.DoDragDrop(_organizePageList, data, DragDropEffects.Move);
        e.Handled = true;
    }

    private void OrganizePageList_Drop(object sender, DragEventArgs e)
    {
        _organizeDragStartPoint = null;
        if (!_organizeModeActive || _organizeMaterializing ||
            _organizePageList is null ||
            _organizePlan is null ||
            !e.Data.GetDataPresent(OrganizeDragDataFormat))
        {
            return;
        }

        var insertionIndex = _organizePlan.Pages.Count;
        var container = FindOrganizeListBoxItem(e.OriginalSource as DependencyObject);
        if (container is not null)
        {
            var targetIndex = _organizePageList.ItemContainerGenerator.IndexFromContainer(container);
            if (targetIndex >= 0)
            {
                var position = e.GetPosition(container);
                insertionIndex = position.Y > container.ActualHeight / 2d
                    ? targetIndex + 1
                    : targetIndex;
            }
        }

        MoveOrganizeSelection(insertionIndex);
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OrganizeTask6_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (HandleOrganizeKey(e.Key, Keyboard.Modifiers))
            e.Handled = true;
    }

    private void ApplyOrganizeSelectionClick(Guid itemId, ModifierKeys modifiers)
    {
        if (!_organizeModeActive || _organizeMaterializing || _organizePlan is null)
            return;

        if ((modifiers & ModifierKeys.Shift) != 0)
            _organizeSelection.SelectRange(_organizePlan, itemId);
        else if ((modifiers & ModifierKeys.Control) != 0)
            _organizeSelection.Toggle(itemId);
        else
            _organizeSelection.SelectSingle(itemId);

        SyncOrganizeListSelection();
    }

    private bool MoveOrganizeSelection(int insertionIndex)
    {
        if (!CanMutateOrganizePlan() || _organizePlan is null || _organizeSelection.SelectedItemIds.Count == 0)
            return false;

        var next = OrganizePlanOperations.MoveSelection(
            _organizePlan,
            _organizeSelection.SelectedItemIds,
            insertionIndex);
        if (ReferenceEquals(next, _organizePlan))
            return false;

        ApplyTask6OrganizePlan(next);
        StatusText.Text = "Páginas reordenadas. Usa Guardar como... para materializar los cambios.";
        return true;
    }

    private bool RotateOrganizeSelection(int deltaQuarterTurns)
    {
        if (!CanMutateOrganizePlan() || _organizePlan is null || _organizeSelection.SelectedItemIds.Count == 0)
            return false;

        var next = OrganizePlanOperations.Rotate(
            _organizePlan,
            _organizeSelection.SelectedItemIds,
            deltaQuarterTurns);
        if (ReferenceEquals(next, _organizePlan))
            return false;

        ApplyTask6OrganizePlan(next);
        StatusText.Text = "Rotación aplicada al plan. Usa Guardar como... para materializar los cambios.";
        return true;
    }

    private bool DeleteOrganizeSelection()
    {
        if (!CanMutateOrganizePlan() || _organizePlan is null || _organizeSelection.SelectedItemIds.Count == 0)
            return false;

        if (_organizeSelection.SelectedItemIds.Count == _organizePlan.Pages.Count)
        {
            StatusText.Text = "No se pueden eliminar todas las páginas del documento.";
            return false;
        }

        try
        {
            var next = OrganizePlanOperations.Delete(
                _organizePlan,
                _organizeSelection.SelectedItemIds);
            _organizeSelection.Clear();
            ApplyTask6OrganizePlan(next);
            StatusText.Text = "Páginas eliminadas del plan. Usa Guardar como... para materializar los cambios.";
            return true;
        }
        catch (InvalidOperationException ex)
        {
            StatusText.Text = ex.Message;
            return false;
        }
    }

    private bool DuplicateOrganizeSelection()
    {
        if (!CanMutateOrganizePlan() || _organizePlan is null || _organizeSelection.SelectedItemIds.Count == 0)
            return false;

        var existingIds = _organizePlan.Pages.Select(page => page.ItemId).ToHashSet();
        var next = OrganizePlanOperations.Duplicate(
            _organizePlan,
            _organizeSelection.SelectedItemIds);
        var duplicateIds = next.Pages
            .Select(page => page.ItemId)
            .Where(itemId => !existingIds.Contains(itemId))
            .ToArray();
        if (duplicateIds.Length == 0)
            return false;

        _organizeSelection.Clear();
        _organizeSelection.SelectSingle(duplicateIds[0]);
        for (var index = 1; index < duplicateIds.Length; index++)
            _organizeSelection.Toggle(duplicateIds[index]);

        ApplyTask6OrganizePlan(next);
        StatusText.Text = "Páginas duplicadas en el plan. Usa Guardar como... para materializar los cambios.";
        return true;
    }

    private bool HandleOrganizeKey(Key key, ModifierKeys modifiers)
    {
        if (!_organizeModeActive || _organizeMaterializing || _organizePlan is null)
            return false;

        if (key == Key.A && (modifiers & ModifierKeys.Control) != 0)
        {
            _organizeSelection.SelectAll(_organizePlan);
            SyncOrganizeListSelection();
            return true;
        }

        if (key == Key.Escape && modifiers == ModifierKeys.None)
        {
            _organizeSelection.Clear();
            SyncOrganizeListSelection();
            return true;
        }

        if (key == Key.Delete && modifiers == ModifierKeys.None)
            return DeleteOrganizeSelection();

        return false;
    }

    private bool TrySaveOrganizePlan()
    {
        if (!_organizeModeActive || _organizeMaterializing || _organizePlan is null || _session is null)
            return false;

        var destinationPath = _selectOrganizePdfDestination();
        if (string.IsNullOrWhiteSpace(destinationPath))
            return false;

        _organizeMaterializing = true;
        UpdateOrganizeCommandAvailability();
        try
        {
            var preflight = InspectOrganizePlanBeforeSave(_organizePlan, CancellationToken.None);
            if (!preflight.CanProceed)
            {
                StatusText.Text = "No se guardó la copia: el plan contiene una estructura bloqueada para ORGANIZAR.";
                return false;
            }

            var warningsConfirmed = false;
            if (preflight.RequiresWarningConfirmation)
            {
                if (!_confirmOrganizeWarnings(preflight))
                {
                    StatusText.Text = "Guardado cancelado: no se confirmó la advertencia de preservación.";
                    return false;
                }
                warningsConfirmed = true;
            }

            _saveOrganizeCopy(
                _organizePlan,
                _session.FilePath,
                destinationPath,
                warningsConfirmed,
                CancellationToken.None);

            StatusText.Text = $"Copia organizada guardada: {destinationPath}";
            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo guardar la copia organizada: {ex.Message}";
            return false;
        }
        finally
        {
            _organizeMaterializing = false;
            UpdateOrganizeCommandAvailability();
        }
    }

    private OrganizePreflightResult InspectOrganizePlanBeforeSave(
        OrganizePlan plan,
        CancellationToken cancellationToken)
    {
        var findings = new List<OrganizeFinding>();
        foreach (var source in plan.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_session is not null &&
                string.Equals(source.Path, _session.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                findings.AddRange(_inspectCurrentOrganizePreflight(_session, cancellationToken).Findings);
                continue;
            }

            try
            {
                using var session = PdfDocumentSession.Open(source.Path);
                findings.AddRange(_inspectCurrentOrganizePreflight(session, cancellationToken).Findings);
            }
            catch (PdfDocumentOpenException ex)
                when (ex.Error == PdfDocumentOpenError.PasswordRequiredOrIncorrect)
            {
                findings.Add(new OrganizeFinding(
                    OrganizeFindingKind.PasswordProtectedSource,
                    OrganizeFindingSeverity.Block,
                    $"El PDF de origen protegido no se puede materializar: {source.Path}",
                    OrganizePreservationStatus.Unknown));
            }
        }

        return new OrganizePreflightResult(findings.AsReadOnly());
    }

    private void ApplyTask6OrganizePlan(OrganizePlan plan)
    {
        ApplyOrganizePlanSnapshot(plan);
        _organizeSelection.Reconcile(plan);
        SyncOrganizeListSelection();
        UpdateOrganizeCommandAvailability();
    }

    private void SyncOrganizeListSelection()
    {
        if (_organizePageList is null)
            return;

        _organizePageList.SelectedItems.Clear();
        foreach (var item in _organizePages)
        {
            if (_organizeSelection.SelectedItemIds.Contains(item.ItemId))
                _organizePageList.SelectedItems.Add(item);
        }
    }

    private void UpdateOrganizeCommandAvailability()
    {
        if (!_organizeUiInitialized)
            return;

        var task6Enabled = _organizeModeActive && !_organizeMaterializing;
        SetEnabled("OrganizeRotateLeftButton", task6Enabled);
        SetEnabled("OrganizeRotateRightButton", task6Enabled);
        SetEnabled("OrganizeDeleteButton", task6Enabled);
        SetEnabled("OrganizeDuplicateButton", task6Enabled);
        SetEnabled("OrganizeSaveAsButton", task6Enabled);

        SetEnabled("OrganizeInsertButton", false);
        SetEnabled("OrganizeMergeButton", false);
        SetEnabled("OrganizeExtractButton", false);
        SetEnabled("OrganizeSplitButton", false);

        void SetEnabled(string name, bool enabled)
        {
            if (FindName(name) is Button button)
                button.IsEnabled = enabled;
        }
    }

    private bool CanMutateOrganizePlan()
        => _organizeModeActive && !_organizeMaterializing && _organizePlan is not null;

    private ListBoxItem? FindOrganizeListBoxItem(DependencyObject? source)
    {
        for (var current = source; current is not null; current = GetVisualOrLogicalParent(current))
        {
            if (current is ListBoxItem item &&
                ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(item), _organizePageList))
            {
                return item;
            }
        }

        return null;
    }

    private static DependencyObject? GetVisualOrLogicalParent(DependencyObject current)
    {
        if (current is Visual || current is System.Windows.Media.Media3D.Visual3D)
            return VisualTreeHelper.GetParent(current);
        return LogicalTreeHelper.GetParent(current);
    }
}
