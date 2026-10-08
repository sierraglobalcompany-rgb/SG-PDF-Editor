using System.Windows.Ink;

namespace SGPdf.App.Features.Sign;

internal sealed class SignatureInkHistory
{
    private readonly List<Stroke> _undo = [];
    private readonly Stack<Stroke> _redo = new();

    internal bool CanUndo => _undo.Count > 0;
    internal bool CanRedo => _redo.Count > 0;

    internal void RecordUserStroke(Stroke stroke)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        _undo.Add(stroke);
        _redo.Clear();
    }

    internal Stroke? Undo(StrokeCollection current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (_undo.Count == 0)
            return null;

        var index = _undo.Count - 1;
        var stroke = _undo[index];
        if (!current.Contains(stroke))
            throw new InvalidOperationException("El historial de firma no coincide con los trazos actuales.");

        _undo.RemoveAt(index);
        current.Remove(stroke);
        _redo.Push(stroke);
        return stroke;
    }

    internal Stroke? Redo(StrokeCollection current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (_redo.Count == 0)
            return null;

        var stroke = _redo.Pop();
        if (current.Contains(stroke))
            throw new InvalidOperationException("El trazo ya existe en la firma actual.");

        current.Add(stroke);
        _undo.Add(stroke);
        return stroke;
    }

    internal void Clear(StrokeCollection current)
    {
        ArgumentNullException.ThrowIfNull(current);
        current.Clear();
        _undo.Clear();
        _redo.Clear();
    }
}
