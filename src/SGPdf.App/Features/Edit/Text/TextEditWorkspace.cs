using System.IO;
using SGPdf.App.Features.Edit;

namespace SGPdf.App.Features.Edit.Text;

internal sealed class TextEditWorkspace
{
    private readonly Dictionary<TextObjectKey, TextEditState> _states = new();
    private readonly Dictionary<TextObjectKey, TextEditState> _originalStates = new();
    private readonly Dictionary<TextObjectKey, TextEditState> _savedBaseline = new();

    private TextEditWorkspace(
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

    internal bool IsDirty => _states.Any(pair =>
        !_savedBaseline.TryGetValue(pair.Key, out var baseline)
        || pair.Value != baseline);

    internal IReadOnlyList<TextEditState> EditedStates => _states
        .Where(pair => pair.Value != _originalStates[pair.Key])
        .OrderBy(pair => pair.Key.PageIndex)
        .ThenBy(pair => pair.Key.PageObjectIndex)
        .Select(pair => pair.Value)
        .ToArray();

    internal static TextEditWorkspace Create(string sourcePath, bool sourceOpenedWithPassword)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("La ruta fuente es obligatoria.", nameof(sourcePath));

        var normalizedPath = Path.GetFullPath(sourcePath);
        var fingerprint = PdfEditSourceFingerprint.Capture(normalizedPath);
        return new TextEditWorkspace(normalizedPath, fingerprint, sourceOpenedWithPassword);
    }

    internal TextEditState EnsureObject(PdfTextObjectInfo sourceObject)
    {
        ArgumentNullException.ThrowIfNull(sourceObject);

        var key = sourceObject.Key;
        if (_states.TryGetValue(key, out var existing))
        {
            if (existing.Original != sourceObject)
                throw new InvalidOperationException($"El objeto de texto {key.PageIndex}:{key.PageObjectIndex} cambió respecto al snapshot registrado.");

            return existing;
        }

        var state = new TextEditState(
            sourceObject,
            sourceObject.Text,
            sourceObject.FontSize,
            sourceObject.FillColor,
            TextFontStrategy.OriginalFont);

        _states.Add(key, state);
        _originalStates.Add(key, state);
        _savedBaseline.Add(key, state);
        return state;
    }

    internal TextEditState GetState(TextObjectKey key)
    {
        if (_states.TryGetValue(key, out var state))
            return state;

        throw new KeyNotFoundException($"No existe estado para el texto {key.PageIndex}:{key.PageObjectIndex}.");
    }

    internal TextEditPolicyResult PrepareCandidate(
        TextObjectKey key,
        string text,
        double fontSize,
        PdfTextFillColor fillColor)
    {
        var current = GetState(key);
        var result = TextEditPolicy.Evaluate(current.Original, text, fontSize, fillColor);
        if (result.Candidate is null)
            return result;

        return result with
        {
            Candidate = result.Candidate with { ExpectedState = current }
        };
    }

    internal void CommitCandidate(TextEditCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (!_states.TryGetValue(candidate.Key, out var current))
            throw new KeyNotFoundException($"No existe estado para el texto {candidate.Key.PageIndex}:{candidate.Key.PageObjectIndex}.");

        if (current != candidate.ExpectedState)
            throw new InvalidOperationException("El candidato de edición quedó obsoleto respecto al estado actual del texto.");

        var next = candidate.ToState();
        if (next == current)
            return;

        _states[candidate.Key] = next;
    }

    internal void MarkSavedBaseline()
    {
        _savedBaseline.Clear();
        foreach (var pair in _states)
            _savedBaseline.Add(pair.Key, pair.Value);
    }
}
