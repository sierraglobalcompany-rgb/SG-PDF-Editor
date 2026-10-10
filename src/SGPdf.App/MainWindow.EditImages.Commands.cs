using System.Windows;
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

    private bool UndoImageEdit()
    {
        if (!_imageEditModeActive || _imageEditWorkspace?.Undo() != true)
            return false;

        CancelImageEditGesture();
        ClearDeletedSelection();
        RefreshImageEditOverlay();
        return true;
    }

    private bool RedoImageEdit()
    {
        if (!_imageEditModeActive || _imageEditWorkspace?.Redo() != true)
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
        if (!_imageEditModeActive || IsReaderShortcutEditableSource(source))
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
        if (!_imageEditModeActive ||
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
        if (_imageEditOverlayCanvas?.IsMouseCaptured == true)
            _imageEditOverlayCanvas.ReleaseMouseCapture();
    }

    private PdfObjectMatrix CurrentDisplayMatrix(ImageEditState state)
        => _imageEditGestureKey == state.ObjectRef.Key &&
           _imageEditPreviewMatrix is PdfObjectMatrix preview
            ? preview
            : state.CurrentMatrix;

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
