using System.Windows.Ink;
using System.Windows.Input;
using SGPdf.App.Features.Sign;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class SignatureInkHistoryTests
{
    [Fact]
    public void RecordUserStroke_EnablesUndoAndClearsRedo()
    {
        var history = new SignatureInkHistory();
        var current = new StrokeCollection();
        var first = CreateStroke(0);
        current.Add(first);
        history.RecordUserStroke(first);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);

        history.Undo(current);
        Assert.True(history.CanRedo);

        var second = CreateStroke(20);
        current.Add(second);
        history.RecordUserStroke(second);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Undo_RemovesOnlyNewestStrokeAndEnablesRedo()
    {
        var history = new SignatureInkHistory();
        var current = new StrokeCollection();
        var first = CreateStroke(0);
        var second = CreateStroke(20);
        AddUserStroke(history, current, first);
        AddUserStroke(history, current, second);

        var removed = history.Undo(current);

        Assert.Same(second, removed);
        Assert.Single(current);
        Assert.Same(first, current[0]);
        Assert.True(history.CanUndo);
        Assert.True(history.CanRedo);
    }

    [Fact]
    public void Redo_RestoresSameStrokeAndAttributesInOrder()
    {
        var history = new SignatureInkHistory();
        var current = new StrokeCollection();
        var first = CreateStroke(0, SignatureInkColor.Black, SignatureInkWidth.Thin);
        var second = CreateStroke(20, SignatureInkColor.Blue, SignatureInkWidth.Thick);
        AddUserStroke(history, current, first);
        AddUserStroke(history, current, second);
        history.Undo(current);

        var restored = history.Redo(current);

        Assert.Same(second, restored);
        Assert.Equal(2, current.Count);
        Assert.Same(first, current[0]);
        Assert.Same(second, current[1]);
        Assert.Equal(SignatureStrokeStyle.GetColor(SignatureInkColor.Blue), restored!.DrawingAttributes.Color);
        Assert.Equal(SignatureStrokeStyle.GetWidthDip(SignatureInkWidth.Thick), restored.DrawingAttributes.Width);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void RepeatedUndoRedo_PreservesStrokeOrder()
    {
        var history = new SignatureInkHistory();
        var current = new StrokeCollection();
        var first = CreateStroke(0);
        var second = CreateStroke(20);
        var third = CreateStroke(40);
        AddUserStroke(history, current, first);
        AddUserStroke(history, current, second);
        AddUserStroke(history, current, third);

        Assert.Same(third, history.Undo(current));
        Assert.Same(second, history.Undo(current));
        Assert.Same(second, history.Redo(current));
        Assert.Same(third, history.Redo(current));

        Assert.Equal(new[] { first, second, third }, current.ToArray());
    }

    [Fact]
    public void NewUserStrokeAfterUndo_ClearsRedo()
    {
        var history = new SignatureInkHistory();
        var current = new StrokeCollection();
        AddUserStroke(history, current, CreateStroke(0));
        AddUserStroke(history, current, CreateStroke(20));
        history.Undo(current);
        Assert.True(history.CanRedo);

        AddUserStroke(history, current, CreateStroke(40));

        Assert.False(history.CanRedo);
        Assert.Null(history.Redo(current));
    }

    [Fact]
    public void Clear_RemovesAllStrokesAndResetsHistory()
    {
        var history = new SignatureInkHistory();
        var current = new StrokeCollection();
        AddUserStroke(history, current, CreateStroke(0));
        AddUserStroke(history, current, CreateStroke(20));
        history.Undo(current);

        history.Clear(current);

        Assert.Empty(current);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Null(history.Undo(current));
        Assert.Null(history.Redo(current));
    }

    private static void AddUserStroke(SignatureInkHistory history, StrokeCollection current, Stroke stroke)
    {
        current.Add(stroke);
        history.RecordUserStroke(stroke);
    }

    private static Stroke CreateStroke(
        double offset,
        SignatureInkColor color = SignatureInkColor.Black,
        SignatureInkWidth width = SignatureInkWidth.Medium)
        => new(
            new StylusPointCollection
            {
                new StylusPoint(10 + offset, 10),
                new StylusPoint(30 + offset, 25),
                new StylusPoint(50 + offset, 10)
            },
            SignatureStrokeStyle.CreateDrawingAttributes(color, width));
}
