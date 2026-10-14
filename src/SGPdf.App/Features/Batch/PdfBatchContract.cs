namespace SGPdf.App.Features.Batch;

internal enum PdfBatchOperation
{
    TextWatermark
}

internal sealed record PdfBatchRequest(
    IReadOnlyList<string> InputPaths,
    string OutputDirectory,
    PdfBatchOperation Operation);

internal sealed record PdfBatchItemResult(
    string InputPath,
    string OutputPath,
    bool Success,
    string? ErrorMessage);
