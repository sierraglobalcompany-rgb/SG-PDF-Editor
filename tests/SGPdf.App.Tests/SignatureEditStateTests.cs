using SGPdf.App.Features.Sign;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class SignatureEditStateTests
{
    private static SignatureAsset CreateAsset(int width = 200, int height = 100)
        => new(width, height, width * 4, new byte[width * height * 4], "synthetic.png");

    [Fact]
    public void AddCentered_UsesBoundedInitialSizeAndPreservesAspectRatio()
    {
        var state = new SignatureEditState(0, new PdfRect(0d, 0d, 600d, 800d));
        var asset = CreateAsset();

        var placement = state.AddCentered(asset);

        Assert.Equal(144d, placement.Bounds.Width, 3);
        Assert.Equal(72d, placement.Bounds.Height, 3);
        Assert.Equal(asset.AspectRatio, placement.Bounds.Width / placement.Bounds.Height, 3);
        Assert.True(state.IsDirty);
        Assert.Equal(placement.Id, state.SelectedId);
    }

    [Fact]
    public void AddCentered_ReducesProportionallyWhenHeightWouldExceedQuarterPage()
    {
        var state = new SignatureEditState(0, new PdfRect(0d, 0d, 600d, 120d));
        var asset = CreateAsset(width: 100, height: 200);

        var placement = state.AddCentered(asset);

        Assert.Equal(30d, placement.Bounds.Height, 3);
        Assert.Equal(15d, placement.Bounds.Width, 3);
        Assert.Equal(asset.AspectRatio, placement.Bounds.Width / placement.Bounds.Height, 3);
    }

    [Fact]
    public void SetSelectedBounds_ClampsToPageAndEnforcesAspectRatioAndMinimumWidth()
    {
        var state = new SignatureEditState(0, new PdfRect(10d, 20d, 200d, 100d));
        var placement = state.AddCentered(CreateAsset());

        var updated = state.SetSelectedBounds(new PdfRect(500d, 500d, 4d, 80d));

        Assert.Equal(12d, updated.Bounds.Width, 3);
        Assert.Equal(6d, updated.Bounds.Height, 3);
        Assert.InRange(updated.Bounds.Left, 10d, 198d);
        Assert.InRange(updated.Bounds.Bottom, 20d, 114d);
        Assert.True(updated.Bounds.Left + updated.Bounds.Width <= 210.001d);
        Assert.True(updated.Bounds.Bottom + updated.Bounds.Height <= 120.001d);
        Assert.NotEqual(placement.Bounds, updated.Bounds);
        Assert.True(state.IsDirty);
    }

    [Fact]
    public void DuplicateSelected_UsesNewIdSelectsDuplicateAndKeepsItInsidePage()
    {
        var state = new SignatureEditState(0, new PdfRect(0d, 0d, 220d, 120d));
        var original = state.AddCentered(CreateAsset());
        state.MarkSaved();

        var duplicate = state.DuplicateSelected();

        Assert.NotNull(duplicate);
        Assert.NotEqual(original.Id, duplicate!.Id);
        Assert.Equal(duplicate.Id, state.SelectedId);
        Assert.Equal(2, state.Placements.Count);
        Assert.True(duplicate.Bounds.Left >= 0d);
        Assert.True(duplicate.Bounds.Bottom >= 0d);
        Assert.True(duplicate.Bounds.Left + duplicate.Bounds.Width <= 220.001d);
        Assert.True(duplicate.Bounds.Bottom + duplicate.Bounds.Height <= 120.001d);
        Assert.True(state.IsDirty);
    }

    [Fact]
    public void DeleteSelected_RemovesOnlySelectedPlacement()
    {
        var state = new SignatureEditState(0, new PdfRect(0d, 0d, 500d, 500d));
        var first = state.AddCentered(CreateAsset());
        var second = state.DuplicateSelected()!;
        Assert.True(state.Select(first.Id));
        state.MarkSaved();

        var deleted = state.DeleteSelected();

        Assert.True(deleted);
        Assert.Single(state.Placements);
        Assert.Equal(second.Id, state.Placements[0].Id);
        Assert.Null(state.SelectedId);
        Assert.True(state.IsDirty);
    }

    [Fact]
    public void MarkSavedAndDiscardAll_UpdateDirtyStateWithoutAmbiguity()
    {
        var state = new SignatureEditState(0, new PdfRect(0d, 0d, 500d, 500d));
        state.AddCentered(CreateAsset());

        state.MarkSaved();
        Assert.False(state.IsDirty);
        Assert.Single(state.Placements);

        state.DiscardAll();
        Assert.False(state.IsDirty);
        Assert.Empty(state.Placements);
        Assert.Null(state.SelectedId);
    }
}
