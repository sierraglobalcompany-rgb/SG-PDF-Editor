using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfRenderSchedulerTests
{
    private static Type SchedulerType
    {
        get
        {
            var type = typeof(PdfDocumentSession).Assembly.GetType("SGPdf.App.Pdf.PdfRenderScheduler");
            Assert.NotNull(type);
            return type!;
        }
    }

    [Fact]
    public void Begin_MarksFirstRequestAsCurrent()
    {
        using dynamic scheduler = Activator.CreateInstance(SchedulerType)!;

        dynamic request = scheduler.Begin();

        Assert.False((bool)request.CancellationToken.IsCancellationRequested);
        Assert.True((bool)scheduler.IsCurrent(request));
    }

    [Fact]
    public void Begin_CancelsPreviousRequestAndMakesNewestCurrent()
    {
        using dynamic scheduler = Activator.CreateInstance(SchedulerType)!;

        dynamic first = scheduler.Begin();
        dynamic second = scheduler.Begin();

        Assert.True((bool)first.CancellationToken.IsCancellationRequested);
        Assert.False((bool)scheduler.IsCurrent(first));
        Assert.False((bool)second.CancellationToken.IsCancellationRequested);
        Assert.True((bool)scheduler.IsCurrent(second));
    }

    [Fact]
    public void CancelCurrent_CancelsAndInvalidatesCurrentRequest()
    {
        using dynamic scheduler = Activator.CreateInstance(SchedulerType)!;

        dynamic request = scheduler.Begin();
        scheduler.CancelCurrent();

        Assert.True((bool)request.CancellationToken.IsCancellationRequested);
        Assert.False((bool)scheduler.IsCurrent(request));
    }

    [Fact]
    public void Dispose_CancelsCurrentRequest()
    {
        dynamic scheduler = Activator.CreateInstance(SchedulerType)!;
        dynamic request = scheduler.Begin();

        ((IDisposable)scheduler).Dispose();

        Assert.True((bool)request.CancellationToken.IsCancellationRequested);
    }
}
