namespace SGPdf.App.Features.Organize;

internal sealed record OrganizePage
{
    internal OrganizePage(
        Guid itemId,
        Guid sourceId,
        int sourcePageIndex,
        int rotationDeltaQuarterTurns)
    {
        if (itemId == Guid.Empty)
            throw new ArgumentException("El identificador de página es obligatorio.", nameof(itemId));
        if (sourceId == Guid.Empty)
            throw new ArgumentException("El identificador de origen es obligatorio.", nameof(sourceId));
        if (sourcePageIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(sourcePageIndex));

        ItemId = itemId;
        SourceId = sourceId;
        SourcePageIndex = sourcePageIndex;
        RotationDeltaQuarterTurns = Normalize(rotationDeltaQuarterTurns);
    }

    internal Guid ItemId { get; }
    internal Guid SourceId { get; }
    internal int SourcePageIndex { get; }
    internal int RotationDeltaQuarterTurns { get; }

    private static int Normalize(int quarterTurns)
    {
        var normalized = quarterTurns % 4;
        return normalized < 0 ? normalized + 4 : normalized;
    }
}
