namespace SGPdf.App.Features.Labels;

public enum LabelMediaKind
{
    Thermal,
    A4,
    Letter,
    Custom
}

public enum LabelPageOrientation
{
    Portrait,
    Landscape
}

public enum LabelRotation
{
    Degrees0,
    Degrees90
}

public sealed class LabelLayoutSettings
{
    private static readonly int[] SupportedPresetCapacities = [1, 2, 3, 4, 6, 8, 10, 12];

    public LabelLayoutSettings(
        LabelMediaKind mediaKind,
        int labelsPerPage = 1,
        int? rows = null,
        int? columns = null,
        LabelPageOrientation orientation = LabelPageOrientation.Portrait,
        LabelRotation rotation = LabelRotation.Degrees0,
        double horizontalMarginMm = 0,
        double verticalMarginMm = 0,
        double horizontalGapMm = 0,
        double verticalGapMm = 0,
        double customPageWidthMm = 0,
        double customPageHeightMm = 0)
    {
        if (!Enum.IsDefined(mediaKind))
            throw new ArgumentOutOfRangeException(nameof(mediaKind));
        if (!Enum.IsDefined(orientation))
            throw new ArgumentOutOfRangeException(nameof(orientation));
        if (!Enum.IsDefined(rotation))
            throw new ArgumentOutOfRangeException(nameof(rotation));
        if (labelsPerPage < 1)
            throw new ArgumentOutOfRangeException(nameof(labelsPerPage));
        if (rows.HasValue != columns.HasValue)
            throw new ArgumentException("Rows and columns must be supplied together.");

        if (rows is not null && columns is not null)
        {
            if (rows < 1)
                throw new ArgumentOutOfRangeException(nameof(rows));
            if (columns < 1)
                throw new ArgumentOutOfRangeException(nameof(columns));

            int customCapacity;
            checked
            {
                customCapacity = rows.Value * columns.Value;
            }

            if (customCapacity != labelsPerPage)
                throw new ArgumentException("Rows × columns must equal labels per page.");
        }
        else if (!SupportedPresetCapacities.Contains(labelsPerPage))
        {
            throw new ArgumentOutOfRangeException(nameof(labelsPerPage));
        }

        ValidateNonNegativeFinite(horizontalMarginMm, nameof(horizontalMarginMm));
        ValidateNonNegativeFinite(verticalMarginMm, nameof(verticalMarginMm));
        ValidateNonNegativeFinite(horizontalGapMm, nameof(horizontalGapMm));
        ValidateNonNegativeFinite(verticalGapMm, nameof(verticalGapMm));

        if (mediaKind == LabelMediaKind.Custom)
        {
            ValidatePositiveFinite(customPageWidthMm, nameof(customPageWidthMm));
            ValidatePositiveFinite(customPageHeightMm, nameof(customPageHeightMm));
        }

        if (mediaKind == LabelMediaKind.Thermal && labelsPerPage != 1)
            throw new ArgumentException("Thermal output supports one label per page.", nameof(labelsPerPage));

        MediaKind = mediaKind;
        LabelsPerPage = labelsPerPage;
        Rows = rows;
        Columns = columns;
        Orientation = orientation;
        Rotation = rotation;
        HorizontalMarginMm = horizontalMarginMm;
        VerticalMarginMm = verticalMarginMm;
        HorizontalGapMm = horizontalGapMm;
        VerticalGapMm = verticalGapMm;
        CustomPageWidthMm = customPageWidthMm;
        CustomPageHeightMm = customPageHeightMm;
    }

    public LabelMediaKind MediaKind { get; }
    public int LabelsPerPage { get; }
    public int? Rows { get; }
    public int? Columns { get; }
    public LabelPageOrientation Orientation { get; }
    public LabelRotation Rotation { get; }
    public double HorizontalMarginMm { get; }
    public double VerticalMarginMm { get; }
    public double HorizontalGapMm { get; }
    public double VerticalGapMm { get; }
    public double CustomPageWidthMm { get; }
    public double CustomPageHeightMm { get; }

    private static void ValidateNonNegativeFinite(double value, string paramName)
    {
        if (!double.IsFinite(value) || value < 0)
            throw new ArgumentOutOfRangeException(paramName);
    }

    private static void ValidatePositiveFinite(double value, string paramName)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(paramName);
    }
}
