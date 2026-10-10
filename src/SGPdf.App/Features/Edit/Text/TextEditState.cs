namespace SGPdf.App.Features.Edit.Text;

internal enum TextFontStrategy
{
    OriginalFont,
    FallbackTtf
}

internal sealed record TextEditState(
    PdfTextObjectInfo Original,
    string Text,
    double FontSize,
    PdfTextFillColor FillColor,
    TextFontStrategy FontStrategy)
{
    internal TextObjectKey Key => Original.Key;
}

internal sealed record TextEditCandidate(
    TextObjectKey Key,
    TextEditState ExpectedState,
    string Text,
    double FontSize,
    PdfTextFillColor FillColor,
    TextFontStrategy FontStrategy)
{
    internal TextEditState ToState() => ExpectedState with
    {
        Text = Text,
        FontSize = FontSize,
        FillColor = FillColor,
        FontStrategy = FontStrategy
    };
}

internal sealed record TextEditPolicyResult(
    TextEditCandidate? Candidate,
    string? Error,
    bool IsReadOnly)
{
    internal bool IsValid => Candidate is not null;
}
