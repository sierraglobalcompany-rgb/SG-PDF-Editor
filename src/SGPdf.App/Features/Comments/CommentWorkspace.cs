using System.IO;
using SGPdf.App.Features.Edit;

namespace SGPdf.App.Features.Comments;

internal sealed class CommentWorkspace
{
    private readonly Dictionary<CommentKey, CommentState> _states = new();
    private readonly Dictionary<CommentKey, CommentState> _originalBaseline = new();
    private readonly Dictionary<CommentKey, CommentState> _savedBaseline = new();
    private readonly Stack<CommentMutation> _undo = new();
    private readonly Stack<CommentMutation> _redo = new();

    private CommentWorkspace(
        string sourcePath,
        PdfEditSourceFingerprint sourceFingerprint,
        bool sourceOpenedWithPassword)
    {
        SourcePath = sourcePath;
        SourceFingerprint = sourceFingerprint;
        SourceOpenedWithPassword = sourceOpenedWithPassword;
    }

    internal string SourcePath { get; }
    internal PdfEditSourceFingerprint SourceFingerprint { get; }
    internal bool SourceOpenedWithPassword { get; }
    internal bool SourceMatchesCurrentFile => SourceFingerprint.MatchesCurrentFile(SourcePath);
    internal bool IsDirty => !MapsEqual(_states, _savedBaseline);
    internal bool CanUndo => _undo.Count > 0;
    internal bool CanRedo => _redo.Count > 0;

    internal IReadOnlyList<CommentState> EditedStates => _states.Values
        .Where(HasSourceDelta)
        .OrderBy(state => state.PageIndex)
        .ThenBy(state => state.Key.Value)
        .ToArray();

    internal static CommentWorkspace Create(string sourcePath, bool sourceOpenedWithPassword)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("La ruta fuente es obligatoria.", nameof(sourcePath));

        var normalizedPath = Path.GetFullPath(sourcePath);
        var fingerprint = PdfEditSourceFingerprint.Capture(normalizedPath);
        return new CommentWorkspace(normalizedPath, fingerprint, sourceOpenedWithPassword);
    }

    internal CommentState EnsureBaseline(CommentState sourceState)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        if (sourceState.IsNew)
            throw new ArgumentException("Un snapshot baseline no puede marcarse como comentario nuevo.", nameof(sourceState));
        if (sourceState.Deleted)
            throw new ArgumentException("Un snapshot baseline no puede iniciar eliminado.", nameof(sourceState));

        if (_states.TryGetValue(sourceState.Key, out var existing))
        {
            if (!existing.SemanticallyEquals(sourceState))
                throw new InvalidOperationException("La identidad del comentario ya está asociada a otro snapshot.");
            return existing;
        }

        if (_originalBaseline.ContainsKey(sourceState.Key) || _savedBaseline.ContainsKey(sourceState.Key))
            throw new InvalidOperationException("La identidad del comentario ya fue registrada en otro baseline.");

        _states.Add(sourceState.Key, sourceState);
        _originalBaseline.Add(sourceState.Key, sourceState);
        _savedBaseline.Add(sourceState.Key, sourceState);
        return sourceState;
    }

    internal CommentState GetState(CommentKey key)
    {
        if (_states.TryGetValue(key, out var state))
            return state;

        throw new KeyNotFoundException($"No existe estado para el comentario {key.Value}.");
    }

    internal CommentCandidate PrepareCreate(CommentState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!state.IsNew)
            throw new ArgumentException("PrepareCreate requiere un comentario marcado como nuevo.", nameof(state));
        if (state.Deleted)
            throw new ArgumentException("No se puede crear un comentario ya eliminado.", nameof(state));
        if (_states.ContainsKey(state.Key) || _originalBaseline.ContainsKey(state.Key) || _savedBaseline.ContainsKey(state.Key))
            throw new InvalidOperationException("La identidad del comentario ya existe en este workspace.");

        return new CommentCandidate(CommentOperationKind.Create, state.Key, null, state);
    }

    internal CommentCandidate PrepareUpdate(CommentOperationKind kind, CommentState nextState)
    {
        ArgumentNullException.ThrowIfNull(nextState);
        if (kind is CommentOperationKind.Create or CommentOperationKind.Delete)
            throw new ArgumentOutOfRangeException(nameof(kind));

        var current = GetState(nextState.Key);
        EnsureMutableIdentity(current, nextState);
        if (current.Deleted)
            throw new InvalidOperationException("No se puede modificar un comentario eliminado.");

        return new CommentCandidate(kind, current.Key, current, nextState);
    }

    internal CommentCandidate PrepareMove(CommentKey key, CommentRect rect)
    {
        var current = GetMutableState(key);
        return new CommentCandidate(
            CommentOperationKind.Move,
            key,
            current,
            current with { Rect = rect });
    }

    internal CommentCandidate PrepareResize(CommentKey key, CommentRect rect)
    {
        var current = GetMutableState(key);
        return new CommentCandidate(
            CommentOperationKind.Resize,
            key,
            current,
            current with { Rect = rect });
    }

    internal CommentCandidate PrepareNoteText(CommentKey key, string contents)
    {
        ArgumentNullException.ThrowIfNull(contents);
        var current = GetMutableState(key);
        if (current.Subtype != CommentSubtype.Text)
            throw new InvalidOperationException("Solo una nota TEXT puede editar contenido textual.");

        return new CommentCandidate(
            CommentOperationKind.EditNoteText,
            key,
            current,
            current with { Contents = contents });
    }

    internal CommentCandidate PrepareDelete(CommentKey key)
    {
        var current = GetState(key);
        var next = current.IsNew
            ? null
            : current with { Deleted = true };

        return new CommentCandidate(CommentOperationKind.Delete, key, current, next);
    }

    internal void CommitCandidate(CommentCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ValidateCandidateIdentity(candidate);

        CommentState? before;
        if (candidate.ExpectedState is null)
        {
            if (_states.ContainsKey(candidate.Key))
                throw new InvalidOperationException("El candidato quedó obsoleto: la identidad ya existe.");
            before = null;
        }
        else
        {
            if (!_states.TryGetValue(candidate.Key, out var current) ||
                !ReferenceEquals(current, candidate.ExpectedState))
            {
                throw new InvalidOperationException("El candidato de comentario quedó obsoleto respecto al estado actual.");
            }
            before = current;
        }

        if (StatesEqual(before, candidate.NextState))
            return;

        ApplyState(candidate.Key, candidate.NextState);
        _undo.Push(new CommentMutation(candidate.Kind, candidate.Key, before, candidate.NextState));
        _redo.Clear();
    }

    internal bool Undo()
    {
        if (_undo.Count == 0)
            return false;

        var mutation = _undo.Pop();
        ApplyState(mutation.Key, mutation.Before);
        _redo.Push(mutation);
        return true;
    }

    internal bool Redo()
    {
        if (_redo.Count == 0)
            return false;

        var mutation = _redo.Pop();
        ApplyState(mutation.Key, mutation.After);
        _undo.Push(mutation);
        return true;
    }

    internal void MarkSaved()
    {
        _savedBaseline.Clear();
        foreach (var pair in _states)
            _savedBaseline.Add(pair.Key, pair.Value);

        _undo.Clear();
        _redo.Clear();
    }

    private CommentState GetMutableState(CommentKey key)
    {
        var current = GetState(key);
        if (current.Deleted)
            throw new InvalidOperationException("No se puede modificar un comentario eliminado.");
        return current;
    }

    private bool HasSourceDelta(CommentState state)
    {
        if (!_originalBaseline.TryGetValue(state.Key, out var original))
            return state.IsNew && !state.Deleted;

        return !state.SemanticallyEquals(original);
    }

    private static void EnsureMutableIdentity(CommentState current, CommentState next)
    {
        if (current.Key != next.Key ||
            current.PageIndex != next.PageIndex ||
            current.Subtype != next.Subtype ||
            current.IsNew != next.IsNew)
        {
            throw new ArgumentException("Una mutación no puede cambiar la identidad, página, subtipo u origen del comentario.", nameof(next));
        }
    }

    private static void ValidateCandidateIdentity(CommentCandidate candidate)
    {
        if (candidate.ExpectedState is not null && candidate.ExpectedState.Key != candidate.Key)
            throw new ArgumentException("El estado esperado no corresponde a la identidad del candidato.", nameof(candidate));
        if (candidate.NextState is not null && candidate.NextState.Key != candidate.Key)
            throw new ArgumentException("El estado siguiente no corresponde a la identidad del candidato.", nameof(candidate));
        if (candidate.ExpectedState is null && candidate.NextState is null)
            throw new ArgumentException("Un candidato no puede carecer simultáneamente de estado anterior y siguiente.", nameof(candidate));
    }

    private void ApplyState(CommentKey key, CommentState? state)
    {
        if (state is null)
            _states.Remove(key);
        else
            _states[key] = state;
    }

    private static bool MapsEqual(
        IReadOnlyDictionary<CommentKey, CommentState> left,
        IReadOnlyDictionary<CommentKey, CommentState> right)
    {
        if (left.Count != right.Count)
            return false;

        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var other) || !pair.Value.SemanticallyEquals(other))
                return false;
        }

        return true;
    }

    private static bool StatesEqual(CommentState? left, CommentState? right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;
        return left.SemanticallyEquals(right);
    }
}
