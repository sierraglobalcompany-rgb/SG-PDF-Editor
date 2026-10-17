using SGPdf.App.Features.Comments;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Redaction;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Features.Organize;
using SGPdf.App.Features.Sign;
using SGPdf.App.Pdf;

namespace SGPdf.App.Shell;

internal enum PdfDocumentPendingMode
{
    Reader,
    Edit,
    Sign,
    Comment,
    Organize,
    AcroForm
}

internal sealed class PdfDocumentPendingState
{
    private readonly Dictionary<string, string> _acroFormOriginalValues = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _acroFormCurrentValues = new(StringComparer.Ordinal);

    internal PdfDocumentPendingMode Mode { get; set; } = PdfDocumentPendingMode.Reader;

    internal ImageEditWorkspace? ImageEditWorkspace { get; set; }
    internal TextEditWorkspace? TextEditWorkspace { get; set; }
    internal RedactionWorkspace? RedactionWorkspace { get; set; }
    internal IReadOnlyList<PdfImageObjectInfo> ActiveImageObjects { get; set; } = Array.Empty<PdfImageObjectInfo>();
    internal IReadOnlyList<PdfTextObjectInfo> ActiveTextObjects { get; set; } = Array.Empty<PdfTextObjectInfo>();
    internal ImageObjectKey? SelectedImageKey { get; set; }
    internal TextObjectKey? SelectedTextKey { get; set; }

    internal SignatureEditState? SignatureEditState { get; set; }

    internal CommentWorkspace? CommentWorkspace { get; set; }
    internal CommentTool CommentTool { get; set; } = CommentTool.Select;
    internal CommentKey? SelectedCommentKey { get; set; }
    internal IReadOnlyList<CommentKey> CommentKnownKeys { get; set; } = Array.Empty<CommentKey>();
    internal IReadOnlyDictionary<CommentKey, PdfCommentInfo> CommentSourceInfoByKey { get; set; }
        = new Dictionary<CommentKey, PdfCommentInfo>();
    internal IReadOnlyDictionary<int, IReadOnlyList<PdfCommentInfo>> CommentReadOnlyByPage { get; set; }
        = new Dictionary<int, IReadOnlyList<PdfCommentInfo>>();
    internal IReadOnlySet<int> CommentLoadedPages { get; set; } = new HashSet<int>();
    internal CommentColor CommentDefaultColor { get; set; } = new(255, 220, 0, 180);
    internal double CommentDefaultBorderWidth { get; set; } = 2d;

    internal OrganizePlan? OrganizePlan { get; set; }
    internal bool OrganizeDirty { get; set; }
    internal IReadOnlyDictionary<Guid, IReadOnlyList<PdfPageSize>> OrganizeSourcePageSizes { get; set; }
        = new Dictionary<Guid, IReadOnlyList<PdfPageSize>>();

    internal bool AcroFormActive { get; private set; }
    internal IReadOnlyDictionary<string, string> AcroFormOriginalValues => _acroFormOriginalValues;
    internal IReadOnlyDictionary<string, string> AcroFormCurrentValues => _acroFormCurrentValues;

    internal bool IsDirty =>
        ImageEditWorkspace?.IsDirty == true ||
        TextEditWorkspace?.IsDirty == true ||
        RedactionWorkspace?.IsDirty == true ||
        SignatureEditState?.IsDirty == true ||
        CommentWorkspace?.IsDirty == true ||
        OrganizeDirty ||
        IsAcroFormDirty();

    internal void SetAcroFormState(
        IReadOnlyDictionary<string, string> originalValues,
        IReadOnlyDictionary<string, string> currentValues,
        bool active)
    {
        ArgumentNullException.ThrowIfNull(originalValues);
        ArgumentNullException.ThrowIfNull(currentValues);

        _acroFormOriginalValues.Clear();
        foreach (var pair in originalValues)
            _acroFormOriginalValues[pair.Key] = pair.Value;

        _acroFormCurrentValues.Clear();
        foreach (var pair in currentValues)
            _acroFormCurrentValues[pair.Key] = pair.Value;

        AcroFormActive = active;
    }

    internal void ClearAcroFormState()
    {
        _acroFormOriginalValues.Clear();
        _acroFormCurrentValues.Clear();
        AcroFormActive = false;
    }

    private bool IsAcroFormDirty()
    {
        if (_acroFormOriginalValues.Count != _acroFormCurrentValues.Count)
            return true;

        foreach (var pair in _acroFormCurrentValues)
        {
            if (!_acroFormOriginalValues.TryGetValue(pair.Key, out var original) ||
                !string.Equals(original, pair.Value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
