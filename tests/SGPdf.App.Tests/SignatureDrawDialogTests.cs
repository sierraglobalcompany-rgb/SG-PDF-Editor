using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using SGPdf.App.Features.Sign;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class SignatureDrawDialogTests
{
    [Fact]
    public void Dialog_OpensEmptyWithBlackMediumDefaults()
    {
        RunInSta(() =>
        {
            var dialog = new SignatureDrawDialog();
            var canvas = Assert.IsType<InkCanvas>(dialog.FindName("SignatureDrawInkCanvas"));
            Assert.Equal(InkCanvasEditingMode.Ink, canvas.EditingMode);
            Assert.Equal(SignatureStrokeStyle.GetColor(SignatureInkColor.Black), canvas.DefaultDrawingAttributes.Color);
            Assert.Equal(SignatureStrokeStyle.GetWidthDip(SignatureInkWidth.Medium), canvas.DefaultDrawingAttributes.Width);
            Assert.Equal(SignatureInkWidth.Medium, Assert.IsType<ComboBox>(dialog.FindName("SignatureDrawWidthComboBox")).SelectedItem);
            Assert.False(Assert.IsType<Button>(dialog.FindName("SignatureDrawApplyButton")).IsEnabled);
            Assert.False(Assert.IsType<Button>(dialog.FindName("SignatureDrawUndoButton")).IsEnabled);
            Assert.False(Assert.IsType<Button>(dialog.FindName("SignatureDrawRedoButton")).IsEnabled);
            Assert.False(Assert.IsType<Button>(dialog.FindName("SignatureDrawClearButton")).IsEnabled);
            dialog.Close();
        });
    }

    [Fact]
    public void Dialog_ApplyDisabledUntilUsefulInkExists()
    {
        RunInSta(() =>
        {
            var dialog = new SignatureDrawDialog();
            var apply = Assert.IsType<Button>(dialog.FindName("SignatureDrawApplyButton"));
            Assert.False(apply.IsEnabled);
            dialog.AddStrokeForTest(CreateStroke(0));
            Assert.True(apply.IsEnabled);
            dialog.Close();
        });
    }

    [Fact]
    public void StrokeCollected_UpdatesHistoryAndButtonStates()
    {
        RunInSta(() =>
        {
            var dialog = new SignatureDrawDialog();
            dialog.AddStrokeForTest(CreateStroke(0));
            Assert.True(Assert.IsType<Button>(dialog.FindName("SignatureDrawUndoButton")).IsEnabled);
            Assert.False(Assert.IsType<Button>(dialog.FindName("SignatureDrawRedoButton")).IsEnabled);
            Assert.True(Assert.IsType<Button>(dialog.FindName("SignatureDrawClearButton")).IsEnabled);
            dialog.Close();
        });
    }

    [Fact]
    public void ChangingColorOrWidth_ChangesOnlyFutureDefaultDrawingAttributes()
    {
        RunInSta(() =>
        {
            var dialog = new SignatureDrawDialog();
            var canvas = Assert.IsType<InkCanvas>(dialog.FindName("SignatureDrawInkCanvas"));
            var first = CreateStroke(0, canvas.DefaultDrawingAttributes.Clone());
            dialog.AddStrokeForTest(first);

            Assert.IsType<Button>(dialog.FindName("SignatureDrawBlueButton"))
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.IsType<ComboBox>(dialog.FindName("SignatureDrawWidthComboBox")).SelectedItem = SignatureInkWidth.Thick;
            var second = CreateStroke(30, canvas.DefaultDrawingAttributes.Clone());
            dialog.AddStrokeForTest(second);

            Assert.Equal(SignatureStrokeStyle.GetColor(SignatureInkColor.Black), first.DrawingAttributes.Color);
            Assert.Equal(SignatureStrokeStyle.GetWidthDip(SignatureInkWidth.Medium), first.DrawingAttributes.Width);
            Assert.Equal(SignatureStrokeStyle.GetColor(SignatureInkColor.Blue), second.DrawingAttributes.Color);
            Assert.Equal(SignatureStrokeStyle.GetWidthDip(SignatureInkWidth.Thick), second.DrawingAttributes.Width);
            dialog.Close();
        });
    }

    [Fact]
    public void UndoRedoClear_OperateOnInkCanvasWithoutGlobalState()
    {
        RunInSta(() =>
        {
            var dialog = new SignatureDrawDialog();
            var canvas = Assert.IsType<InkCanvas>(dialog.FindName("SignatureDrawInkCanvas"));
            dialog.AddStrokeForTest(CreateStroke(0));
            dialog.AddStrokeForTest(CreateStroke(30));

            Click(dialog, "SignatureDrawUndoButton");
            Assert.Single(canvas.Strokes);
            Assert.True(Assert.IsType<Button>(dialog.FindName("SignatureDrawRedoButton")).IsEnabled);

            Click(dialog, "SignatureDrawRedoButton");
            Assert.Equal(2, canvas.Strokes.Count);

            Click(dialog, "SignatureDrawClearButton");
            Assert.Empty(canvas.Strokes);
            Assert.False(Assert.IsType<Button>(dialog.FindName("SignatureDrawUndoButton")).IsEnabled);
            Assert.False(Assert.IsType<Button>(dialog.FindName("SignatureDrawRedoButton")).IsEnabled);
            Assert.False(Assert.IsType<Button>(dialog.FindName("SignatureDrawApplyButton")).IsEnabled);
            dialog.Close();
        });
    }

    [Fact]
    public void Cancel_LeavesPreparedAssetNull()
    {
        RunInSta(() =>
        {
            var dialog = new SignatureDrawDialog();
            dialog.AddStrokeForTest(CreateStroke(0));
            Click(dialog, "SignatureDrawCancelButton");
            Assert.Null(dialog.PreparedAsset);
            if (dialog.IsLoaded)
                dialog.Close();
        });
    }

    [Fact]
    public void Apply_ValidInkReturnsSignatureAsset()
    {
        RunInSta(() =>
        {
            var dialog = new SignatureDrawDialog();
            dialog.AddStrokeForTest(CreateStroke(0));
            Assert.True(dialog.TryApplyCurrentInk());
            Assert.NotNull(dialog.PreparedAsset);
            dialog.Close();
        });
    }

    [Fact]
    public void Apply_RenderFailure_PreservesStrokesHistoryAndDialogState()
    {
        RunInSta(() =>
        {
            var dialog = new SignatureDrawDialog();
            var canvas = Assert.IsType<InkCanvas>(dialog.FindName("SignatureDrawInkCanvas"));
            dialog.AddStrokeForTest(CreateStroke(0));
            SetField(dialog, "_renderInk", (Func<StrokeCollection, SignatureAsset>)(_ => throw new InvalidDataException("forced")));

            Assert.False(dialog.TryApplyCurrentInk());
            Assert.Null(dialog.PreparedAsset);
            Assert.Single(canvas.Strokes);
            Assert.True(Assert.IsType<Button>(dialog.FindName("SignatureDrawUndoButton")).IsEnabled);
            Assert.Contains("forced", Assert.IsType<TextBlock>(dialog.FindName("SignatureDrawStatusText")).Text, StringComparison.OrdinalIgnoreCase);
            dialog.Close();
        });
    }

    [Fact]
    public void ClosingWindow_IsEquivalentToCancel()
    {
        RunInSta(() =>
        {
            var dialog = new SignatureDrawDialog();
            dialog.AddStrokeForTest(CreateStroke(0));
            Assert.Null(dialog.PreparedAsset);
            dialog.Close();
            Assert.Null(dialog.PreparedAsset);
        });
    }

    private static Stroke CreateStroke(double offset, DrawingAttributes? attributes = null)
        => new(
            new StylusPointCollection
            {
                new StylusPoint(10 + offset, 20),
                new StylusPoint(50 + offset, 45),
                new StylusPoint(90 + offset, 20)
            },
            attributes ?? SignatureStrokeStyle.CreateDrawingAttributes(SignatureInkColor.Black, SignatureInkWidth.Medium));

    private static void Click(SignatureDrawDialog dialog, string name)
        => Assert.IsType<Button>(dialog.FindName(name)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void SetField(SignatureDrawDialog dialog, string name, object value)
    {
        var field = typeof(SignatureDrawDialog).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(dialog, value);
    }

    private static void RunInSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
