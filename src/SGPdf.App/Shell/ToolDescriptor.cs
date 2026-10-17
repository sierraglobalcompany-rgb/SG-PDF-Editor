namespace SGPdf.App.Shell;

internal sealed record ToolDescriptor(
    string Id,
    string Title,
    string Category,
    string Description,
    string IconKey,
    string? ActionId,
    bool RequiresPdf,
    string? DisabledReason = null)
{
    public string HomeIconPath =>
        $"/SGPdf.App;component/Assets/HomeTools/{Id}.png";

    public string? DocumentIconPath => Id switch
    {
        "read" or "comment" or "forms" => null,
        "edit" => "/SGPdf.App;component/Assets/HomeTools/edit-pdf.png",
        "sign" => "/SGPdf.App;component/Assets/HomeTools/sign.png",
        "organize" => "/SGPdf.App;component/Assets/HomeTools/organize-pages.png",
        "ocr" => "/SGPdf.App;component/Assets/HomeTools/ocr.png",
        "redact" => "/SGPdf.App;component/Assets/HomeTools/redact.png",
        "compare" => "/SGPdf.App;component/Assets/HomeTools/compare-pdf.png",
        "convert" => "/SGPdf.App;component/Assets/HomeTools/pdf-to-png.png",
        "protect" => "/SGPdf.App;component/Assets/HomeTools/protect.png",
        _ => null
    };
}
