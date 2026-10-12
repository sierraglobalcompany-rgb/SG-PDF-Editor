using System.IO;
using SGPdf.App.Features.Edit.Text;

namespace SGPdf.App.Features.Edit.Redaction;

internal sealed class RedactionWorkspace
{
    private readonly Dictionary<TextObjectKey, RedactionTarget> _targets = new();

    private RedactionWorkspace(
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
    internal bool IsDirty => _targets.Count > 0;

    internal IReadOnlyList<RedactionTarget> Targets => _targets.Values
        .OrderBy(target => target.Original.Key.PageIndex)
        .ThenBy(target => target.Original.Key.PageObjectIndex)
        .ToArray();

    internal static RedactionWorkspace Create(
        string sourcePath,
        bool sourceOpenedWithPassword = false)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("La ruta fuente es obligatoria.", nameof(sourcePath));

        var normalizedPath = Path.GetFullPath(sourcePath);
        return new RedactionWorkspace(
            normalizedPath,
            PdfEditSourceFingerprint.Capture(normalizedPath),
            sourceOpenedWithPassword);
    }

    internal void Stage(RedactionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.PageIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(plan), "La página del plan de redacción no es válida.");
        if (plan.Targets is null || plan.Targets.Count == 0)
            throw new ArgumentException("El plan de redacción no contiene objetivos.", nameof(plan));

        foreach (var target in plan.Targets)
        {
            ArgumentNullException.ThrowIfNull(target);
            if (target.Original.Key.PageIndex != plan.PageIndex)
                throw new ArgumentException("Todos los objetivos deben pertenecer a la página del plan.", nameof(plan));
            if (_targets.ContainsKey(target.Original.Key))
                throw new InvalidOperationException("El objeto de texto ya está preparado para redacción.");
        }

        foreach (var target in plan.Targets)
            _targets.Add(target.Original.Key, target);
    }
}
