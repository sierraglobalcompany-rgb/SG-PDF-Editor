namespace SGPdf.App.Features.Labels;

public sealed record ZplDesign(
    int Index,
    int SourceBlockIndex,
    string OriginalBlock,
    int QuantityFromFile);
