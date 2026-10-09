using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SGPdf.App.Features.Organize;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool OrganizeTask8LoadedHookRegistered = RegisterOrganizeTask8LoadedHook();
    private bool _organizeTask8UiInitialized;

    private Func<string?> _selectOrganizeExtractDestination = static () =>
    {
        var dialog = new SaveFileDialog
        {
            Title = "Extraer páginas a PDF",
            Filter = "Archivo PDF (*.pdf)|*.pdf",
            AddExtension = true,
            DefaultExt = ".pdf",
            OverwritePrompt = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    };

    private Func<string?> _selectOrganizeSplitBasePath = static () =>
    {
        var dialog = new SaveFileDialog
        {
            Title = "Nombre base para los PDF divididos",
            Filter = "Archivo PDF (*.pdf)|*.pdf",
            AddExtension = true,
            DefaultExt = ".pdf",
            OverwritePrompt = false
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    };

    private Action<IReadOnlyList<OrganizePlannedOutput>, bool, CancellationToken> _publishOrganizeBatch =
        static (outputs, warningsConfirmed, token) =>
            new OrganizeBatchPublisher().Publish(outputs, warningsConfirmed, token);

    private static bool RegisterOrganizeTask8LoadedHook()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OrganizeTask8Host_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void OrganizeTask8Host_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window)
            return;

        window.InitializeOrganizeUi();
        window.InitializeOrganizeTask6Ui();
        window.InitializeOrganizeTask7Ui();
        window.InitializeOrganizeTask8Ui();
    }

    private void InitializeOrganizeTask8Ui()
    {
        if (_organizeTask8UiInitialized)
            return;

        _organizeTask8UiInitialized = true;
        _ = OrganizeTask8LoadedHookRegistered;

        var extractButton = Button("OrganizeExtractButton");
        var splitButton = Button("OrganizeSplitButton");
        var surface = _organizeSurface
            ?? throw new InvalidOperationException("La superficie ORGANIZAR no está inicializada.");
        var list = _organizePageList
            ?? throw new InvalidOperationException("La lista ORGANIZAR no está inicializada.");

        extractButton.Click += (_, _) =>
        {
            TryExtractOrganizeSelection();
            UpdateOrganizeTask8CommandAvailability();
        };
        splitButton.Click += (_, _) =>
        {
            TrySplitOrganizePdf();
            UpdateOrganizeTask8CommandAvailability();
        };

        list.SelectionChanged += (_, _) => UpdateOrganizeTask8CommandAvailability();

        var visibilityDescriptor = DependencyPropertyDescriptor.FromProperty(
            UIElement.VisibilityProperty,
            typeof(UIElement));
        visibilityDescriptor?.AddValueChanged(surface, (_, _) => UpdateOrganizeTask8CommandAvailability());

        var splitEnabledDescriptor = DependencyPropertyDescriptor.FromProperty(
            UIElement.IsEnabledProperty,
            typeof(UIElement));
        splitEnabledDescriptor?.AddValueChanged(splitButton, (_, _) =>
        {
            var shouldEnable = _organizeModeActive && !_organizeMaterializing && _organizePlan is not null;
            if (splitButton.IsEnabled != shouldEnable)
                UpdateOrganizeTask8CommandAvailability();
        });

        UpdateOrganizeTask8CommandAvailability();

        Button Button(string name)
            => FindName(name) as Button
               ?? throw new InvalidOperationException($"No se encontró el comando {name}.");
    }

    private void UpdateOrganizeTask8CommandAvailability()
    {
        if (!_organizeTask8UiInitialized)
            return;

        var enabled = _organizeModeActive && !_organizeMaterializing && _organizePlan is not null;
        if (FindName("OrganizeExtractButton") is Button extractButton)
            extractButton.IsEnabled = enabled && _organizeSelection.SelectedItemIds.Count > 0;
        if (FindName("OrganizeSplitButton") is Button splitButton)
            splitButton.IsEnabled = enabled;
    }

    private bool TryExtractOrganizeSelection()
    {
        if (!CanMutateOrganizePlan() || _organizeSelection.SelectedItemIds.Count == 0)
            return false;

        var destination = _selectOrganizeExtractDestination();
        return string.IsNullOrWhiteSpace(destination)
            ? false
            : TryExtractOrganizeSelectionTo(destination);
    }

    private bool TryExtractOrganizeSelectionTo(string destinationPath)
    {
        if (!CanMutateOrganizePlan() || _organizePlan is null || _session is null ||
            _organizeSelection.SelectedItemIds.Count == 0)
        {
            return false;
        }

        OrganizePlan extracted;
        try
        {
            extracted = OrganizeSplitPlanner.ExtractSelected(
                _organizePlan,
                _organizeSelection.SelectedItemIds);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudieron preparar las páginas a extraer: {ex.Message}";
            return false;
        }

        _organizeMaterializing = true;
        UpdateOrganizeCommandAvailability();
        UpdateOrganizeTask8CommandAvailability();
        try
        {
            if (!TryAuthorizeOrganizeDerivedOutput(extracted, out var warningsConfirmed))
                return false;

            _saveOrganizeCopy(
                extracted,
                _session.FilePath,
                destinationPath,
                warningsConfirmed,
                CancellationToken.None);

            StatusText.Text = $"PDF extraído guardado: {destinationPath}";
            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo extraer el PDF: {ex.Message}";
            return false;
        }
        finally
        {
            _organizeMaterializing = false;
            UpdateOrganizeCommandAvailability();
            UpdateOrganizeTask8CommandAvailability();
        }
    }

    private bool TrySplitOrganizeEvery(int pagesPerOutput, string basePath)
    {
        if (!CanMutateOrganizePlan() || _organizePlan is null)
            return false;

        try
        {
            var outputs = OrganizeSplitPlanner.SplitEvery(_organizePlan, pagesPerOutput);
            return TryPublishOrganizeSplit(outputs, basePath);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo preparar la división: {ex.Message}";
            return false;
        }
    }

    private bool TrySplitOrganizeRanges(string expression, string basePath)
    {
        if (!CanMutateOrganizePlan() || _organizePlan is null)
            return false;

        try
        {
            var outputs = OrganizeSplitPlanner.SplitExplicitRanges(_organizePlan, expression);
            return TryPublishOrganizeSplit(outputs, basePath);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo preparar la división: {ex.Message}";
            return false;
        }
    }

    private bool TryPublishOrganizeSplit(
        IReadOnlyList<OrganizePlan> plans,
        string basePath)
    {
        if (!CanMutateOrganizePlan() || _organizePlan is null || plans.Count == 0)
            return false;

        var currentPositions = _organizePlan.Pages
            .Select((page, index) => (page.ItemId, index))
            .ToDictionary(entry => entry.ItemId, entry => entry.index);
        var outputs = new List<OrganizePlannedOutput>(plans.Count);
        for (var index = 0; index < plans.Count; index++)
        {
            var positions = plans[index].Pages
                .Select(page => currentPositions[page.ItemId])
                .ToArray();
            var first = positions.Min() + 1;
            var last = positions.Max() + 1;
            outputs.Add(new OrganizePlannedOutput(
                plans[index],
                OrganizeOutputNaming.BuildSplitPath(basePath, index + 1, first, last)));
        }

        _organizeMaterializing = true;
        UpdateOrganizeCommandAvailability();
        UpdateOrganizeTask8CommandAvailability();
        try
        {
            if (!TryAuthorizeOrganizeDerivedOutput(_organizePlan, out var warningsConfirmed))
                return false;

            _publishOrganizeBatch(outputs, warningsConfirmed, CancellationToken.None);
            StatusText.Text = $"División completada: {outputs.Count} archivo(s) publicados.";
            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo completar la división: {ex.Message}";
            return false;
        }
        finally
        {
            _organizeMaterializing = false;
            UpdateOrganizeCommandAvailability();
            UpdateOrganizeTask8CommandAvailability();
        }
    }

    private bool TryAuthorizeOrganizeDerivedOutput(
        OrganizePlan plan,
        out bool warningsConfirmed)
    {
        warningsConfirmed = false;
        var preflight = InspectOrganizePlanBeforeSave(plan, CancellationToken.None);
        if (!preflight.CanProceed)
        {
            StatusText.Text = "No se creó la salida: el plan contiene una estructura bloqueada para ORGANIZAR.";
            return false;
        }

        if (!preflight.RequiresWarningConfirmation)
            return true;

        if (!_confirmOrganizeWarnings(preflight))
        {
            StatusText.Text = "Operación cancelada: no se confirmó la advertencia de preservación.";
            return false;
        }

        warningsConfirmed = true;
        return true;
    }

    private bool TrySplitOrganizePdf()
    {
        if (!CanMutateOrganizePlan())
            return false;

        var choice = ShowOrganizeSplitChoice();
        if (choice is null)
            return false;

        var basePath = _selectOrganizeSplitBasePath();
        if (string.IsNullOrWhiteSpace(basePath))
            return false;

        return choice.Value.Mode == OrganizeSplitMode.EveryN
            ? TrySplitOrganizeEvery(choice.Value.PagesPerOutput, basePath)
            : TrySplitOrganizeRanges(choice.Value.RangeExpression, basePath);
    }

    private OrganizeSplitDialogChoice? ShowOrganizeSplitChoice()
    {
        var dialog = new Window
        {
            Title = "Dividir PDF",
            Width = 430,
            Height = 250,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = IsVisible ? this : null,
            ShowInTaskbar = false
        };

        var root = new StackPanel { Margin = new Thickness(18) };
        root.Children.Add(new TextBlock
        {
            Text = "Modo de división:",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });
        var mode = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
        mode.Items.Add(new ComboBoxItem { Content = "Cada N páginas", Tag = OrganizeSplitMode.EveryN });
        mode.Items.Add(new ComboBoxItem { Content = "Rangos explícitos", Tag = OrganizeSplitMode.ExplicitRanges });
        mode.SelectedIndex = 0;
        root.Children.Add(mode);

        var prompt = new TextBlock { Text = "Páginas por archivo:" };
        var value = new TextBox { Text = "10", Margin = new Thickness(0, 4, 0, 14) };
        root.Children.Add(prompt);
        root.Children.Add(value);

        mode.SelectionChanged += (_, _) =>
        {
            var explicitRanges = mode.SelectedItem is ComboBoxItem item &&
                                 item.Tag is OrganizeSplitMode.ExplicitRanges;
            prompt.Text = explicitRanges
                ? "Rangos no superpuestos (ej. 1-3, 4-7, 8-10):"
                : "Páginas por archivo:";
            value.Text = explicitRanges ? string.Empty : "10";
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttons.Children.Add(new Button
        {
            Content = "Cancelar",
            MinWidth = 90,
            Margin = new Thickness(0, 0, 8, 0),
            IsCancel = true
        });
        var accept = new Button { Content = "Continuar", MinWidth = 90, IsDefault = true };
        buttons.Children.Add(accept);
        root.Children.Add(buttons);
        dialog.Content = root;

        OrganizeSplitDialogChoice? result = null;
        accept.Click += (_, _) =>
        {
            var selectedMode = mode.SelectedItem is ComboBoxItem item && item.Tag is OrganizeSplitMode splitMode
                ? splitMode
                : OrganizeSplitMode.EveryN;
            if (selectedMode == OrganizeSplitMode.EveryN)
            {
                if (!int.TryParse(value.Text, out var pagesPerOutput) || pagesPerOutput <= 0)
                {
                    MessageBox.Show(dialog, "Indica una cantidad de páginas mayor que cero.", "SG PDF Editor",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                result = new OrganizeSplitDialogChoice(selectedMode, pagesPerOutput, string.Empty);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(value.Text))
                {
                    MessageBox.Show(dialog, "Indica al menos un rango.", "SG PDF Editor",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                result = new OrganizeSplitDialogChoice(selectedMode, 0, value.Text);
            }

            dialog.DialogResult = true;
        };

        return dialog.ShowDialog() == true ? result : null;
    }

    private readonly record struct OrganizeSplitDialogChoice(
        OrganizeSplitMode Mode,
        int PagesPerOutput,
        string RangeExpression);

    private enum OrganizeSplitMode
    {
        EveryN,
        ExplicitRanges
    }
}
