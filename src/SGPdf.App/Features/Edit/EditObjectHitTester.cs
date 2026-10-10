using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Edit;

internal enum EditObjectKind
{
    Image,
    Text
}

internal readonly record struct EditObjectHit(
    EditObjectKind Kind,
    int PageIndex,
    int PageObjectIndex);

internal static class EditObjectHitTester
{
    internal static EditObjectHit? HitTest(
        IReadOnlyList<PdfImageObjectInfo> images,
        IReadOnlyList<PdfTextObjectInfo> texts,
        PdfPoint point)
    {
        ArgumentNullException.ThrowIfNull(images);
        ArgumentNullException.ThrowIfNull(texts);

        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
            return null;

        var image = ImageHitTester.HitTest(images, point);
        var text = TextHitTester.HitTest(texts, point);

        if (image is null && text is null)
            return null;

        if (text is null)
            return new EditObjectHit(EditObjectKind.Image, image!.Value.PageIndex, image.Value.PageObjectIndex);

        if (image is null)
            return new EditObjectHit(EditObjectKind.Text, text.Value.PageIndex, text.Value.PageObjectIndex);

        return image.Value.PageObjectIndex > text.Value.PageObjectIndex
            ? new EditObjectHit(EditObjectKind.Image, image.Value.PageIndex, image.Value.PageObjectIndex)
            : new EditObjectHit(EditObjectKind.Text, text.Value.PageIndex, text.Value.PageObjectIndex);
    }
}
