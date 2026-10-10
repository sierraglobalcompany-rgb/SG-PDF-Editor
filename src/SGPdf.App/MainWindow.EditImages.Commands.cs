using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow
{
    private const double MinimumImageSizePdf = 2d;

    private ImageEditGestureKind _imageEditGestureKind;
    private ImageObjectKey? _imageEditGestureKey;
    private ImageEditState? _imageEditGestureStartState;
    private PdfPoint? _imageEditGestureStartPdfPoint;
    private ImageResizeCorner? _imageEditResizeCorner;
    private PdfObjectMatrix? _imageEditPreviewMatrix;

    private enum ImageEditGestureKind
    {
        None,
        Move,
        Resize
    }

    private enum ImageZOrderAction
    {
        Back,
        Backward,
        Forward,
        Front
    }

    private bool BeginImageMoveAtDevicePoint(double deviceX, double deviceY)
    {
        if (!TryGetSelectedImageState(out var state) ||
            _currentPdfDeviceTransform is not PdfPageDeviceTransform transform)
        {
            return false;
        }

        var start = ImageHitTester.DeviceToPdf(deviceX, deviceY, transform);
        _imageEditGestureKind = ImageEditGestureKind.Move;
        _imageEditGestureKey = state.ObjectRef.Key;
        _imageEditGestureStartState = state;
        _imageEditGestureStartPdfPoint = start;
        _imageEditResizeCorner = null;
        _imageEditPreviewMatrix = state.CurrentMatrix;
        return true;
    }

    private bool UpdateImageMoveAtDevicePoint(double deviceX, double deviceY)
    {
        if (_imageEditGestureKind != ImageEditGestureKind.Move ||
            _imageEditGestureStartState is null ||
            _imageEditGestureStartPdfPoint is not PdfPoint start ||
            _currentPdfDeviceTransform is not PdfPageDeviceTransform transform)
        {
            return false;
        }

        var current = ImageHitTester.DeviceToPdf(deviceX, deviceY, transform);
        _imageEditPreviewMatrix = ImageTransformMath.Translate(
            _imageEditGestureStartState.CurrentMatrix,
            current.X - start.X,
            current.Y - start.Y);
        RefreshImageEditOverlay();
        return true;
    }

    private bool CompleteImageMove()
    {
        if (_imageEditGestureKind != ImageEditGestureKind.Move ||
            _imageEditWorkspace is null ||
            _imageEditGestureStartState is null ||
            _imageEditPreviewMatrix is not PdfObjectMatrix preview)
        {
            CancelImageEditGesture();
            return false;
        }

        var start = _imageEditGestureStartState;
        var changed = preview != start.CurrentMatrix;
        if (changed)
            _imageEditWorkspace.Commit(ImageEditOperationKind.Move, start with { CurrentMatrix = preview });

        CancelImageEditGesture();
        RefreshImageEditOverlay();
        return changed;
    }

    private bool BeginImageResize(
        ImageResizeCorner corner,
        double deviceX,
        double deviceY)
    {
        if (!TryGetSelectedImageState(out var state) ||
            _currentPdfDeviceTransform is not PdfPageDeviceTransform transform)
        {
            return false;
        }

        _ = ImageHitTester.DeviceToPdf(deviceX, deviceY, transform);
        _imageEditGestureKind = ImageEditGestureKind.Resize;
        _imageEditGestureKey = state.ObjectRef.Key;
        _imageEditGestureStartState = state;
        _imageEditGestureStartPdfPoint = null;
        _imageEditResizeCorner = corner;
        _imageEditPreviewMatrix = state.CurrentMatrix;
        return true;
    }

    private bool UpdateImageResizeAtDevicePoint(
        double deviceX,
        double deviceY,
        bool shiftPressed)
    {
        if (_imageEditGestureKind != ImageEditGestureKind.Resize ||
            _imageEditGestureStartState is null ||
            _imageEditResizeCorner is not ImageResizeCorner corner ||
            _currentPdfDeviceTransform is not PdfPageDeviceTransform transform)
        {
            return false;
        }

        try
        {
            var target = ImageHitTester.DeviceToPdf(deviceX, deviceY, transform);
            _imageEditPreviewMatrix = ImageTransformMath.ResizeFromCorner(
                _imageEditGestureStartState.CurrentMatrix,
                corner,
                target,
                preserveAspectRatio: !shiftPressed,
                MinimumImageSizePdf);
            RefreshImageEditOverlay();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private bool CompleteImageResize()
    {
        if (_imageEditGestureKind != ImageEditGestureKind.Resize ||
            _imageEditWorkspace is null ||
            _imageEditGestureStartState is null ||
            _imageEditPreviewMatrix is not PdfObjectMatrix preview)
        {
            CancelImageEditGesture();
            return false;
        }

        var start = _imageEditGestureStartState;
        var changed = preview != start.CurrentMatrix;
        if (changed)
            _imageEditWorkspace.Commit(ImageEditOperationKind.Resize, start with { CurrentMatrix = preview });

        CancelImageEditGesture();
        RefreshImageEditOverlay();
        return changed;
    }

    private bool UpdateActiveImageGestureAtDevicePoint(
        double deviceX,
        double deviceY,
        bool shiftPressed)
        => _imageEditGestureKind switch
        {
            ImageEditGestureKind.Move => UpdateImageMoveAtDevicePoint(deviceX, deviceY),
            ImageEditGestureKind.Resize => UpdateImageResizeAtDevicePoint(deviceX, deviceY, shiftPressed),
            _ => false
        };

    private bool CompleteActiveImageGesture()
        => _imageEditGestureKind switch
        {
            ImageEditGestureKind.Move => CompleteImageMove(),
            ImageEditGestureKind.Resize => CompleteImageResize(),
            _ => false
        };

    private bool RotateSelectedImage(double degrees)
    {
        if (_imageEditWorkspace is null || !TryGetSelectedImageState(out var state))
            return false;

        PdfObjectMatrix rotated;
        try
        {
            rotated = ImageTransformMath.RotateAroundCenter(state.CurrentMatrix, degrees);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (rotated == state.CurrentMatrix)
            return false;

        CancelImageEditGesture();
        _imageEditWorkspace.Commit(
            ImageEditOperationKind.Rotate,
            state with { CurrentMatrix = rotated });
        RefreshImageEditOverlay();
        return true;
    }

    private bool DeleteSelectedImage()
    {
        if (_imageEditWorkspace is null || !TryGetSelectedImageState(out var state))
            return false;
        if (state.Deleted)
            return false;

        CancelImageEditGesture();
        _imageEditWorkspace.Commit(
            ImageEditOperationKind.Delete,
            state with { Deleted = true });
        _selectedImageKey = null;
        RefreshImageEditOverlay();
        return true;
    }

    private bool SetSelectedImageOpacityPercent(int percent)
    {
        if (percent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "La opacidad debe estar entre 0 y 100.");
        if (_imageEditWorkspace is null || !TryGetSelectedImageState(out var state))
            return false;

        byte? opacity = percent == 100
            ? null
            : checked((byte)Math.Round(percent * 255d / 100d, MidpointRounding.AwayFromZero));
        if (state.Opacity == opacity)
            return false;

        CancelImageEditGesture();
        _imageEditWorkspace.Commit(
            ImageEditOperationKind.SetOpacity,
            state with { Opacity = opacity });
        RefreshImageEditOverlay();
        return true;
    }

    private bool MoveSelectedImageForward()
        => ChangeSelectedImageZOrder(ImageZOrderAction.Forward);

    private bool MoveSelectedImageBackward()
        => ChangeSelectedImageZOrder(ImageZOrderAction.Backward);

    private bool BringSelectedImageToFront()
        => ChangeSelectedImageZOrder(ImageZOrderAction.Front);

    private bool SendSelectedImageToBack()
        => ChangeSelectedImageZOrder(ImageZOrderAction.Back);

    private bool ChangeSelectedImageZOrder(ImageZOrderAction action)
    {
        if (_imageEditWorkspace is null || _session is null || !TryGetSelectedImageState(out var state))
            return false;

        int objectCount;
        try
        {
            objectCount = _session.GetPageObjectCount(state.ObjectRef.Key.PageIndex);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or ObjectDisposedException)
        {
            StatusText.Text = $"No se pudo cambiar el orden de la imagen: {ex.Message}";
            return false;
        }

        if (objectCount <= 0)
            return false;

        var originalIndex = state.ObjectRef.Key.PageObjectIndex;
        var currentIndex = state.TargetObjectIndex ?? originalIndex;
        var lastIndex = objectCount - 1;
        var targetIndex = action switch
        {
            ImageZOrderAction.Back => 0,
            ImageZOrderAction.Backward => Math.Max(0, currentIndex - 1),
            ImageZOrderAction.Forward => Math.Min(lastIndex, currentIndex + 1),
            ImageZOrderAction.Front => lastIndex,
            _ => currentIndex
        };

        if (targetIndex == currentIndex)
            return false;

        CancelImageEditGesture();
        _imageEditWorkspace.Commit(
            ImageEditOperationKind.ChangeZOrder,
            state with
            {
                TargetObjectIndex = targetIndex == originalIndex
                    ? null
                    : targetIndex
            });
        RefreshImageEditOverlay();
        return true;
    }

    private bool UndoImageEdit()
    {
        if (!_editModeActive || _imageEditWorkspace?.Undo() != true)
            return false;

        CancelImageEditGesture();
        ClearDeletedSelection();
        RefreshImageEditOverlay();
        return true;
    }

    private bool RedoImageEdit()
    {
        if (!_editModeActive || _imageEditWorkspace?.Redo() != true)
            return false;

        CancelImageEditGesture();
        ClearDeletedSelection();
        RefreshImageEditOverlay();
        return true;
    }

    private bool TryHandleImageEditShortcut(
        Key key,
        ModifierKeys modifiers,
        DependencyObject? source)
    {
        if (!_editModeActive || IsReaderShortcutEditableSource(source))
            return false;

        if (modifiers == ModifierKeys.None && key == Key.Delete)
            return DeleteSelectedImage();

        if (modifiers == ModifierKeys.Control && key == Key.Z)
            return UndoImageEdit();

        if (modifiers == ModifierKeys.Control && key == Key.Y)
            return RedoImageEdit();

        return false;
    }

    private bool TryGetSelectedImageState(out ImageEditState state)
    {
        state = null!;
        if (!_editModeActive ||
            _imageEditWorkspace is null ||
            _selectedImageKey is not ImageObjectKey key)
        {
            return false;
        }

        try
        {
            state = _imageEditWorkspace.GetState(key);
            return !state.Deleted;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    private void ClearDeletedSelection()
    {
        if (_imageEditWorkspace is null || _selectedImageKey is not ImageObjectKey key)
            return;

        try
        {
            if (_imageEditWorkspace.GetState(key).Deleted)
                _selectedImageKey = null;
        }
        catch (KeyNotFoundException)
        {
            _selectedImageKey = null;
        }
    }

    private void CancelImageEditGesture()
    {
        _imageEditGestureKind = ImageEditGestureKind.None;
        _imageEditGestureKey = null;
        _imageEditGestureStartState = null;
        _imageEditGestureStartPdfPoint = null;
        _imageEditResizeCorner = null;
        _imageEditPreviewMatrix = null;
        if (_editOverlayCanvas?.IsMouseCaptured == true)
            _editOverlayCanvas.ReleaseMouseCapture();
    }

    private PdfObjectMatrix CurrentDisplayMatrix(ImageEditState state)
    {
        EnsureOptionalCapabilityControls(state);
        return _imageEditGestureKey == state.ObjectRef.Key &&
               _imageEditPreviewMatrix is PdfObjectMatrix preview
            ? preview
            : state.CurrentMatrix;
    }

    private void EnsureOptionalCapabilityControls(ImageEditState state)
    {
        if (_editOverlayCanvas is null ||
            _selectedImageKey != state.ObjectRef.Key ||
            _editOverlayCanvas.Children.OfType<Button>().Any(button => Equals(button.Tag, "ImageOpacityButton")))
        {
            return;
        }

        var opacitySlider = new Slider
        {
            Tag = "ImageOpacitySlider",
            Minimum = 0d,
            Maximum = 100d,
            Value = state.Opacity is byte alpha
                ? Math.Round(alpha * 100d / 255d)
                : 100d,
            TickFrequency = 1d,
            IsSnapToTickEnabled = true,
            Width = 140d,
            Margin = new Thickness(8d)
        };
        opacitySlider.PreviewMouseLeftButtonUp += (_, e) =>
        {
            SetSelectedImageOpacityPercent((int)Math.Round(opacitySlider.Value));
            e.Handled = true;
        };
        opacitySlider.KeyUp += (_, e) =>
        {
            if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End)
                SetSelectedImageOpacityPercent((int)Math.Round(opacitySlider.Value));
        };

        var opacityMenu = new ContextMenu();
        opacityMenu.Items.Add(opacitySlider);
        var opacityButton = new Button
        {
            Content = "Opacidad",
            ToolTip = "Cambiar opacidad de imagen",
            Padding = new Thickness(6, 2, 6, 2),
            Tag = "ImageOpacityButton",
            ContextMenu = opacityMenu
        };
        opacityButton.PreviewMouseLeftButtonDown += (_, e) =>
        {
            opacityMenu.PlacementTarget = opacityButton;
            opacityMenu.IsOpen = true;
            e.Handled = true;
        };

        var orderMenu = new ContextMenu();
        AddOrderMenuItem(orderMenu, "Enviar al fondo", "ImageOrderBack", SendSelectedImageToBack);
        AddOrderMenuItem(orderMenu, "Retroceder", "ImageOrderBackward", MoveSelectedImageBackward);
        AddOrderMenuItem(orderMenu, "Adelantar", "ImageOrderForward", MoveSelectedImageForward);
        AddOrderMenuItem(orderMenu, "Traer al frente", "ImageOrderFront", BringSelectedImageToFront);
        var orderButton = new Button
        {
            Content = "Orden",
            ToolTip = "Cambiar orden de apilado",
            Padding = new Thickness(6, 2, 6, 2),
            Tag = "ImageOrderButton",
            ContextMenu = orderMenu
        };
        orderButton.PreviewMouseLeftButtonDown += (_, e) =>
        {
            orderMenu.PlacementTarget = orderButton;
            orderMenu.IsOpen = true;
            e.Handled = true;
        };

        var quad = ImageHitTester.GetQuad(state.CurrentMatrix);
        if (_currentPdfDeviceTransform is not PdfPageDeviceTransform transform)
            return;
        var devicePoints = quad.Select(point => ImageHitTester.PdfToDevice(point, transform)).ToArray();
        var minX = devicePoints.Min(point => point.X);
        var minY = devicePoints.Min(point => point.Y);
        var rowY = Math.Max(0d, minY - 60d);

        Canvas.SetLeft(opacityButton, minX);
        Canvas.SetTop(opacityButton, rowY);
        _editOverlayCanvas.Children.Add(opacityButton);
        Canvas.SetLeft(orderButton, minX + 76d);
        Canvas.SetTop(orderButton, rowY);
        _editOverlayCanvas.Children.Add(orderButton);
    }

    private static void AddOrderMenuItem(
        ContextMenu menu,
        string header,
        string tag,
        Func<bool> action)
    {
        var item = new MenuItem
        {
            Header = header,
            Tag = tag
        };
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }

    private IReadOnlyList<PdfImageObjectInfo> CurrentSelectableImageObjects()
    {
        if (_imageEditWorkspace is null)
            return _activeImageObjects;

        var result = new List<PdfImageObjectInfo>(_activeImageObjects.Count);
        foreach (var source in _activeImageObjects)
        {
            var key = new ImageObjectKey(source.PageIndex, source.PageObjectIndex);
            ImageEditState state;
            try
            {
                state = _imageEditWorkspace.GetState(key);
            }
            catch (KeyNotFoundException)
            {
                continue;
            }

            if (state.Deleted)
                continue;

            var matrix = CurrentDisplayMatrix(state);
            var quad = ImageHitTester.GetQuad(matrix);
            var bounds = new PdfObjectBounds(
                quad.Min(point => point.X),
                quad.Min(point => point.Y),
                quad.Max(point => point.X),
                quad.Max(point => point.Y));
            result.Add(source with { Matrix = matrix, Bounds = bounds });
        }

        return result;
    }
}
