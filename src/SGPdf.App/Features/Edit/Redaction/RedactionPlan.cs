using SGPdf.App.Features.Edit.Text;

namespace SGPdf.App.Features.Edit.Redaction;

internal sealed record RedactionTarget(PdfTextObjectInfo Original);

internal sealed record RedactionPlan(
    int PageIndex,
    IReadOnlyList<RedactionTarget> Targets);

internal sealed record RedactionDecision(
    bool Allowed,
    string? Reason,
    RedactionPlan? Plan)
{
    internal static RedactionDecision Allow(RedactionPlan plan) => new(true, null, plan);

    internal static RedactionDecision Reject(string reason) => new(false, reason, null);
}
