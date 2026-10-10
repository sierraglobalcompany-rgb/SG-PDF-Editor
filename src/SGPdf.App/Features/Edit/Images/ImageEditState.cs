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
