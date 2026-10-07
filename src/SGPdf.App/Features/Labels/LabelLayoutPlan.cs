namespace SGPdf.App.Features.Labels;

public sealed record LabelPlacement(
    long OutputIndex,
    int DesignIndex,
    double XMm,
    double YMm,
    double WidthMm,
    double HeightMm,
    LabelRotation Rotation);

public sealed record LabelLayoutPage(
    long PageIndex,
    IReadOnlyList<LabelPlacement> Placements);

public sealed class LabelLayoutPlan
{
    private readonly LabelOutputSequence _sequence;
    private readonly double _labelWidthMm;
    private readonly double _labelHeightMm;
    private readonly double _horizontalMarginMm;
    private readonly double _verticalMarginMm;
    private readonly double _horizontalGapMm;
    private readonly double _verticalGapMm;
    private readonly LabelRotation _rotation;

    internal LabelLayoutPlan(
        LabelOutputSequence sequence,
        double pageWidthMm,
        double pageHeightMm,
        int rows,
        int columns,
        double labelWidthMm,
        double labelHeightMm,
        double horizontalMarginMm,
        double verticalMarginMm,
        double horizontalGapMm,
        double verticalGapMm,
        LabelRotation rotation)
    {
        _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
        PageWidthMm = pageWidthMm;
        PageHeightMm = pageHeightMm;
        Rows = rows;
        Columns = columns;
        _labelWidthMm = labelWidthMm;
        _labelHeightMm = labelHeightMm;
        _horizontalMarginMm = horizontalMarginMm;
        _verticalMarginMm = verticalMarginMm;
        _horizontalGapMm = horizontalGapMm;
        _verticalGapMm = verticalGapMm;
        _rotation = rotation;

        LabelsPerPage = checked(rows * columns);
        TotalLabels = sequence.TotalCount;
        PageCount = TotalLabels / LabelsPerPage + (TotalLabels % LabelsPerPage == 0 ? 0 : 1);
    }

    public double PageWidthMm { get; }
    public double PageHeightMm { get; }
    public int Rows { get; }
    public int Columns { get; }
    public int LabelsPerPage { get; }
    public int DesignCount => _sequence.DesignCount;
    public long TotalLabels { get; }
    public long PageCount { get; }

    public LabelLayoutPage GetPage(long pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= PageCount)
            throw new ArgumentOutOfRangeException(nameof(pageIndex));

        var firstOutputIndex = checked(pageIndex * LabelsPerPage);
        var remaining = TotalLabels - firstOutputIndex;
        var count = (int)Math.Min((long)LabelsPerPage, remaining);
        var placements = new LabelPlacement[count];

        for (var itemIndex = 0; itemIndex < count; itemIndex++)
        {
            var row = itemIndex / Columns;
            var column = itemIndex % Columns;
            var outputIndex = firstOutputIndex + itemIndex;
            var xMm = _horizontalMarginMm + column * (_labelWidthMm + _horizontalGapMm);
            var yMm = _verticalMarginMm + row * (_labelHeightMm + _verticalGapMm);

            placements[itemIndex] = new LabelPlacement(
                outputIndex,
                _sequence.GetDesignIndexAt(outputIndex),
                xMm,
                yMm,
                _labelWidthMm,
                _labelHeightMm,
                _rotation);
        }

        return new LabelLayoutPage(pageIndex, placements);
    }
}
