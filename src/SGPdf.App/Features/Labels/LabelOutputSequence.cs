namespace SGPdf.App.Features.Labels;

public sealed class LabelOutputSequence
{
    private readonly long[] _cumulativeEnds;

    public LabelOutputSequence(
        ZplDocument document,
        ZplQuantitySelection quantitySelection)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(quantitySelection);

        _cumulativeEnds = new long[document.Designs.Count];

        long total = 0;
        checked
        {
            for (var index = 0; index < document.Designs.Count; index++)
            {
                total += quantitySelection.GetQuantity(document.Designs[index]);
                _cumulativeEnds[index] = total;
            }
        }

        TotalCount = total;
    }

    public int DesignCount => _cumulativeEnds.Length;

    public long TotalCount { get; }

    public int GetDesignIndexAt(long outputIndex)
    {
        if (outputIndex < 0 || outputIndex >= TotalCount)
            throw new ArgumentOutOfRangeException(nameof(outputIndex));

        for (var designIndex = 0; designIndex < _cumulativeEnds.Length; designIndex++)
        {
            if (outputIndex < _cumulativeEnds[designIndex])
                return designIndex;
        }

        throw new InvalidOperationException("Output index could not be resolved to a printable design.");
    }
}
