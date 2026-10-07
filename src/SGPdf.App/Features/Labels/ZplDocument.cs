namespace SGPdf.App.Features.Labels;

public sealed class ZplDocument
{
    public ZplDocument(
        string sourcePath,
        string originalSource,
        string normalizedRenderSource,
        IReadOnlyList<ZplDesign> designs)
    {
        SourcePath = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
        OriginalSource = originalSource ?? throw new ArgumentNullException(nameof(originalSource));
        NormalizedRenderSource = normalizedRenderSource ?? throw new ArgumentNullException(nameof(normalizedRenderSource));
        Designs = designs?.ToArray() ?? throw new ArgumentNullException(nameof(designs));

        long total = 0;
        checked
        {
            foreach (var design in Designs)
                total += design.QuantityFromFile;
        }

        TotalQuantityFromFile = total;
    }

    public string SourcePath { get; }

    public string OriginalSource { get; }

    public string NormalizedRenderSource { get; }

    public IReadOnlyList<ZplDesign> Designs { get; }

    public long TotalQuantityFromFile { get; }
}
