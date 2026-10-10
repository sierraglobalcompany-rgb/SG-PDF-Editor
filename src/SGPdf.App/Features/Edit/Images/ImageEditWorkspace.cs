using System.IO;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Edit.Images;

internal sealed class ImageEditWorkspace
{
    private readonly Dictionary<ImageObjectKey, ImageEditState> _states = new();
    private readonly Dictionary<ImageObjectKey, ImageEditState> _originalStates = new();
    private readonly Dictionary<ImageObjectKey, ImageEditState> _savedBaseline = new();
    private readonly Stack<ImageEditMutation> _undo = new();
    private readonly Stack<ImageEditMutation> _redo = new();

    private ImageEditWorkspace(
        string sourcePath,
        ImageEditSourceFingerprint sourceFingerprint,
        bool sourceOpenedWithPassword)
    {
        SourcePath = sourcePath;
        SourceFingerprint = sourceFingerprint;
        SourceOpenedWithPassword = sourceOpenedWithPassword;
    }

    internal string SourcePath { get; }
    internal ImageEditSourceFingerprint SourceFingerprint { get; }
    internal bool SourceOpenedWithPassword { get; }

    internal bool IsDirty => _states.Any(pair =>
        !_savedBaseline.TryGetValue(pair.Key, out var baseline)
        || pair.Value != baseline);

    internal bool CanUndo => _undo.Count > 0;
    internal bool CanRedo => _redo.Count > 0;

    internal IReadOnlyList<ImageEditState> EditedStates => _states
        .Where(pair =>
            pair.Value != _originalStates[pair.Key]
            || _savedBaseline[pair.Key] != _originalStates[pair.Key])
        .OrderBy(pair => pair.Key.PageIndex)
        .ThenBy(pair => pair.Key.PageObjectIndex)
        .Select(pair => pair.Value)
        .ToArray();

    internal static ImageEditWorkspace Create(string sourcePath, bool sourceOpenedWithPassword)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("La ruta fuente es obligatoria.", nameof(sourcePath));

        var normalizedPath = Path.GetFullPath(sourcePath);
        var fingerprint = ImageEditSourceFingerprint.Capture(normalizedPath);
        return new ImageEditWorkspace(normalizedPath, fingerprint, sourceOpenedWithPassword);
    }

    internal ImageEditState EnsureObject(PdfImageObjectInfo sourceObject)
    {
        ArgumentNullException.ThrowIfNull(sourceObject);

        var key = new ImageObjectKey(sourceObject.PageIndex, sourceObject.PageObjectIndex);
        if (_states.TryGetValue(key, out var existing))
            return existing;

        var state = new ImageEditState(
            new ImageObjectRef(key, sourceObject),
            sourceObject.Matrix,
            ReplacementAsset: null,
            Opacity: null,
            TargetObjectIndex: null,
            Deleted: false);

        _states.Add(key, state);
        _originalStates.Add(key, state);
        _savedBaseline.Add(key, state);
        return state;
    }

    internal ImageEditState GetState(ImageObjectKey key)
    {
        if (_states.TryGetValue(key, out var state))
            return state;

        throw new KeyNotFoundException($"No existe estado para la imagen {key.PageIndex}:{key.PageObjectIndex}.");
    }

    internal void Commit(ImageEditOperationKind kind, ImageEditState nextState)
    {
        ArgumentNullException.ThrowIfNull(nextState);

        var key = nextState.ObjectRef.Key;
        if (!_states.TryGetValue(key, out var before))
            throw new KeyNotFoundException($"No existe estado para la imagen {key.PageIndex}:{key.PageObjectIndex}.");

        if (before.ObjectRef != nextState.ObjectRef)
            throw new ArgumentException("La mutación no corresponde al objeto registrado.", nameof(nextState));

        if (before == nextState)
            return;

        _states[key] = nextState;
        _undo.Push(new ImageEditMutation(kind, key, before, nextState));
        _redo.Clear();
    }

    internal bool Undo()
    {
        if (_undo.Count == 0)
            return false;

        var mutation = _undo.Pop();
        _states[mutation.Key] = mutation.Before;
        _redo.Push(mutation);
        return true;
    }

    internal bool Redo()
    {
        if (_redo.Count == 0)
            return false;

        var mutation = _redo.Pop();
        _states[mutation.Key] = mutation.After;
        _undo.Push(mutation);
        return true;
    }

    internal void MarkSavedBaseline()
    {
        _savedBaseline.Clear();
        foreach (var pair in _states)
            _savedBaseline.Add(pair.Key, pair.Value);

        _undo.Clear();
        _redo.Clear();
    }
}
