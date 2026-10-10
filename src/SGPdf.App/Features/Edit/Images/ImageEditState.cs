using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Edit.Images;

internal enum ImageEditOperationKind
{
    Move,
    Resize,
    Rotate,
    Replace,
    Delete,
    SetOpacity,
    ChangeZOrder
}

// Forward contract only. Task 5 adds the immutable replacement payload and loader.
internal sealed record ImageReplacementAsset;

internal sealed record ImageEditState(
    ImageObjectRef ObjectRef,
    PdfObjectMatrix CurrentMatrix,
    ImageReplacementAsset? ReplacementAsset,
    byte? Opacity,
    int? TargetObjectIndex,
    bool Deleted);

internal sealed record ImageEditMutation(
    ImageEditOperationKind Kind,
    ImageObjectKey Key,
    ImageEditState Before,
    ImageEditState After);
