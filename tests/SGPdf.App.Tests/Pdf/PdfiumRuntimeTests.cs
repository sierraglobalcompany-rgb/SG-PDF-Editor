using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests.Pdf;

public sealed class PdfiumRuntimeTests
{
    [Fact]
    public async Task RunExclusive_serializes_concurrent_actions()
    {
        var runtimeType = typeof(PdfDocumentSession).Assembly.GetType("SGPdf.App.Pdf.PdfiumRuntime");
        Assert.NotNull(runtimeType);

        var method = runtimeType.GetMethod(
            "RunExclusive",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(Action)],
            modifiers: null);
        Assert.NotNull(method);

        var sync = new object();
        var active = 0;
        var maxActive = 0;

        Action criticalSection = () =>
        {
            lock (sync)
            {
                active++;
                maxActive = Math.Max(maxActive, active);
            }

            Thread.Sleep(75);

            lock (sync)
                active--;
        };

        await Task.WhenAll(
            Task.Run(() => method.Invoke(null, [criticalSection])),
            Task.Run(() => method.Invoke(null, [criticalSection])));

        Assert.Equal(1, maxActive);
    }
}
