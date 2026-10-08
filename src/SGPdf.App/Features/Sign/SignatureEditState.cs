namespace SGPdf.App.Features.Sign;

internal sealed class SignatureEditState
{
    private const double InitialMaxWidthPoints = 144d;
    private const double InitialWidthFraction = 0.35d;
    private const double InitialHeightFraction = 0.25d;
    private const double MinimumWidthPoints = 12d;
    private const double DuplicateOffsetPoints = 12d;

    private readonly List<SignaturePlacement> _placements = new();
    private readonly PdfRect _pageBounds;

    internal SignatureEditState(int pageIndex, PdfRect pageBounds)
    {
        if (pageIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(pageIndex));
        ValidateBounds(pageBounds, nameof(pageBounds));

        PageIndex = pageIndex;
        _pageBounds = pageBounds;
    }

    internal int PageIndex { get; }
    internal IReadOnlyList<SignaturePlacement> Placements => _placements;
    internal Guid? SelectedId { get; private set; }
    internal bool IsDirty { get; private set; }

    internal SignaturePlacement AddCentered(SignatureAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        var width = Math.Min(InitialMaxWidthPoints, _pageBounds.Width * InitialWidthFraction);
        var height = width / asset.AspectRatio;
        var maxHeight = _pageBounds.Height * InitialHeightFraction;
        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * asset.AspectRatio;
        }

        var bounds = new PdfRect(
            _pageBounds.Left + ((_pageBounds.Width - width) / 2d),
            _pageBounds.Bottom + ((_pageBounds.Height - height) / 2d),
            width,
            height);

        var placement = new SignaturePlacement(Guid.NewGuid(), PageIndex, bounds, asset);
        _placements.Add(placement);
        SelectedId = placement.Id;
        IsDirty = true;
        return placement;
    }

    internal bool Select(Guid id)
    {
        if (_placements.All(placement => placement.Id != id))
            return false;

        SelectedId = id;
        return true;
    }

    internal SignaturePlacement SetSelectedBounds(PdfRect proposedBounds)
    {
        var index = GetSelectedIndex();
        var current = _placements[index];
        ValidateFinitePosition(proposedBounds);

        var width = Math.Max(MinimumWidthPoints, proposedBounds.Width);
        width = Math.Min(width, _pageBounds.Width);
        var height = width / current.Asset.AspectRatio;

        if (height > _pageBounds.Height)
        {
            height = _pageBounds.Height;
            width = height * current.Asset.AspectRatio;
        }

        var left = Math.Clamp(proposedBounds.Left, _pageBounds.Left, _pageBounds.Right - width);
        var bottom = Math.Clamp(proposedBounds.Bottom, _pageBounds.Bottom, _pageBounds.Top - height);
        var updated = current with { Bounds = new PdfRect(left, bottom, width, height) };

        _placements[index] = updated;
        IsDirty = true;
        return updated;
    }

    internal SignaturePlacement? DuplicateSelected()
    {
        if (SelectedId is null)
            return null;

        var index = GetSelectedIndex();
        var source = _placements[index];
        var left = Math.Clamp(
            source.Bounds.Left + DuplicateOffsetPoints,
            _pageBounds.Left,
            _pageBounds.Right - source.Bounds.Width);
        var bottom = Math.Clamp(
            source.Bounds.Bottom - DuplicateOffsetPoints,
            _pageBounds.Bottom,
            _pageBounds.Top - source.Bounds.Height);

        var duplicate = source with
        {
            Id = Guid.NewGuid(),
            Bounds = source.Bounds with { Left = left, Bottom = bottom }
        };
        _placements.Add(duplicate);
        SelectedId = duplicate.Id;
        IsDirty = true;
        return duplicate;
    }

    internal bool DeleteSelected()
    {
        if (SelectedId is null)
            return false;

        var index = _placements.FindIndex(placement => placement.Id == SelectedId.Value);
        if (index < 0)
        {
            SelectedId = null;
            return false;
        }

        _placements.RemoveAt(index);
        SelectedId = null;
        IsDirty = true;
        return true;
    }

    internal void MarkSaved() => IsDirty = false;

    internal void DiscardAll()
    {
        _placements.Clear();
        SelectedId = null;
        IsDirty = false;
    }

    private int GetSelectedIndex()
    {
        if (SelectedId is null)
            throw new InvalidOperationException("No signature placement is selected.");

        var index = _placements.FindIndex(placement => placement.Id == SelectedId.Value);
        if (index < 0)
            throw new InvalidOperationException("The selected signature placement no longer exists.");

        return index;
    }

    private static void ValidateBounds(PdfRect bounds, string parameterName)
    {
        ValidateFinitePosition(bounds);
        if (bounds.Width <= 0d || bounds.Height <= 0d)
            throw new ArgumentOutOfRangeException(parameterName);
    }

    private static void ValidateFinitePosition(PdfRect bounds)
    {
        if (!double.IsFinite(bounds.Left) || !double.IsFinite(bounds.Bottom) ||
            !double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height))
        {
            throw new ArgumentException("Signature bounds must be finite.");
        }
    }
}
