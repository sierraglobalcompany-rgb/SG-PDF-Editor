namespace SGPdf.App.Features.Labels;

public static class LabelLayoutPlanner
{
    private const double FitToleranceMm = 0.000_001d;

    public static LabelLayoutPlan CreatePlan(
        LabelOutputSequence sequence,
        LabelLayoutSettings settings,
        double labelWidthMm,
        double labelHeightMm)
    {
        ArgumentNullException.ThrowIfNull(sequence);
        ArgumentNullException.ThrowIfNull(settings);
        ValidatePositiveFinite(labelWidthMm, nameof(labelWidthMm));
        ValidatePositiveFinite(labelHeightMm, nameof(labelHeightMm));

        var occupiedWidthMm = settings.Rotation == LabelRotation.Degrees90
            ? labelHeightMm
            : labelWidthMm;
        var occupiedHeightMm = settings.Rotation == LabelRotation.Degrees90
            ? labelWidthMm
            : labelHeightMm;

        if (settings.MediaKind == LabelMediaKind.Thermal)
        {
            return new LabelLayoutPlan(
                sequence,
                occupiedWidthMm,
                occupiedHeightMm,
                rows: 1,
                columns: 1,
                occupiedWidthMm,
                occupiedHeightMm,
                horizontalMarginMm: 0,
                verticalMarginMm: 0,
                horizontalGapMm: 0,
                verticalGapMm: 0,
                settings.Rotation);
        }

        var (portraitWidthMm, portraitHeightMm) = settings.MediaKind switch
        {
            LabelMediaKind.A4 => (210d, 297d),
            LabelMediaKind.Letter => (215.9d, 279.4d),
            LabelMediaKind.Custom => (settings.CustomPageWidthMm, settings.CustomPageHeightMm),
            _ => throw new InvalidOperationException("Unsupported label media kind.")
        };

        var pageWidthMm = settings.Orientation == LabelPageOrientation.Landscape
            ? portraitHeightMm
            : portraitWidthMm;
        var pageHeightMm = settings.Orientation == LabelPageOrientation.Landscape
            ? portraitWidthMm
            : portraitHeightMm;

        var usableWidthMm = pageWidthMm - 2d * settings.HorizontalMarginMm;
        var usableHeightMm = pageHeightMm - 2d * settings.VerticalMarginMm;
        if (usableWidthMm <= 0 || usableHeightMm <= 0)
            throw DoesNotFit();

        int rows;
        int columns;
        if (settings.Rows is not null && settings.Columns is not null)
        {
            rows = settings.Rows.Value;
            columns = settings.Columns.Value;
            EnsureFits(
                rows,
                columns,
                occupiedWidthMm,
                occupiedHeightMm,
                usableWidthMm,
                usableHeightMm,
                settings.HorizontalGapMm,
                settings.VerticalGapMm);
        }
        else
        {
            (rows, columns) = SelectPresetGrid(
                settings.LabelsPerPage,
                occupiedWidthMm,
                occupiedHeightMm,
                usableWidthMm,
                usableHeightMm,
                settings.HorizontalGapMm,
                settings.VerticalGapMm);
        }

        return new LabelLayoutPlan(
            sequence,
            pageWidthMm,
            pageHeightMm,
            rows,
            columns,
            occupiedWidthMm,
            occupiedHeightMm,
            settings.HorizontalMarginMm,
            settings.VerticalMarginMm,
            settings.HorizontalGapMm,
            settings.VerticalGapMm,
            settings.Rotation);
    }

    private static (int Rows, int Columns) SelectPresetGrid(
        int labelsPerPage,
        double labelWidthMm,
        double labelHeightMm,
        double usableWidthMm,
        double usableHeightMm,
        double horizontalGapMm,
        double verticalGapMm)
    {
        var usableAspect = usableWidthMm / usableHeightMm;
        var found = false;
        var bestRows = 0;
        var bestColumns = 0;
        var bestScore = double.PositiveInfinity;

        for (var rows = 1; rows <= labelsPerPage; rows++)
        {
            if (labelsPerPage % rows != 0)
                continue;

            var columns = labelsPerPage / rows;
            if (!Fits(
                    rows,
                    columns,
                    labelWidthMm,
                    labelHeightMm,
                    usableWidthMm,
                    usableHeightMm,
                    horizontalGapMm,
                    verticalGapMm))
            {
                continue;
            }

            var gridAspect = (double)columns / rows;
            var score = Math.Abs(gridAspect - usableAspect);
            if (!found || score < bestScore - double.Epsilon)
            {
                found = true;
                bestRows = rows;
                bestColumns = columns;
                bestScore = score;
            }
        }

        if (!found)
            throw DoesNotFit();

        return (bestRows, bestColumns);
    }

    private static void EnsureFits(
        int rows,
        int columns,
        double labelWidthMm,
        double labelHeightMm,
        double usableWidthMm,
        double usableHeightMm,
        double horizontalGapMm,
        double verticalGapMm)
    {
        if (!Fits(
                rows,
                columns,
                labelWidthMm,
                labelHeightMm,
                usableWidthMm,
                usableHeightMm,
                horizontalGapMm,
                verticalGapMm))
        {
            throw DoesNotFit();
        }
    }

    private static bool Fits(
        int rows,
        int columns,
        double labelWidthMm,
        double labelHeightMm,
        double usableWidthMm,
        double usableHeightMm,
        double horizontalGapMm,
        double verticalGapMm)
    {
        var requiredWidthMm = columns * labelWidthMm + (columns - 1) * horizontalGapMm;
        var requiredHeightMm = rows * labelHeightMm + (rows - 1) * verticalGapMm;

        return requiredWidthMm <= usableWidthMm + FitToleranceMm &&
               requiredHeightMm <= usableHeightMm + FitToleranceMm;
    }

    private static InvalidOperationException DoesNotFit()
        => new("Las etiquetas no caben físicamente en el medio y layout seleccionados.");

    private static void ValidatePositiveFinite(double value, string paramName)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(paramName);
    }
}
