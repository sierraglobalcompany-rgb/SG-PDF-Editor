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
}
