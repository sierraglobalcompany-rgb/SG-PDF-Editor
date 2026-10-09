using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using SGPdf.App.Features.Organize;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OrganizeBatchPublisherTests
{
    [Fact]
    public void SplitPublish_AnyExistingTarget_BlocksBeforeFirstWrite()
    {
        using var fixture = BatchFixture.Create();
        var calls = 0;
        Action<OrganizePlan, string, string, bool, CancellationToken> writer = (_, _, _, _, _) => calls++;
        var publisher = CreatePublisher(writer);
        var existing = fixture.Path("existing.pdf");
        File.WriteAllText(existing, "keep-me");
        var outputs = CreateOutputs(
            (fixture.Plan, fixture.Path("first.pdf")),
            (fixture.Plan, existing));

        Assert.Throws<IOException>(() => Publish(publisher, outputs, warningsConfirmed: false, CancellationToken.None));
        Assert.Equal(0, calls);
        Assert.False(File.Exists(fixture.Path("first.pdf")));
        Assert.Equal("keep-me", File.ReadAllText(existing));
    }

    [Fact]
    public void Publish_DuplicateTarget_BlocksBeforeFirstWrite()
    {
        using var fixture = BatchFixture.Create();
        var calls = 0;
        Action<OrganizePlan, string, string, bool, CancellationToken> writer = (_, _, _, _, _) => calls++;
        var publisher = CreatePublisher(writer);
        var destination = fixture.Path("same.pdf");
        var outputs = CreateOutputs((fixture.Plan, destination), (fixture.Plan, destination));

        Assert.Throws<ArgumentException>(() => Publish(publisher, outputs, warningsConfirmed: false, CancellationToken.None));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Publish_FirstFailurePublishesNothing_LaterFailureKeepsPriorAndStops()
    {
        using var fixture = BatchFixture.Create();
        var firstOutputs = CreateOutputs(
            (fixture.Plan, fixture.Path("a.pdf")),
            (fixture.Plan, fixture.Path("b.pdf")));
        var firstCalls = 0;
        var firstPublisher = CreatePublisher((_, _, _, _, _) =>
        {
            firstCalls++;
            throw new InvalidOperationException("first failure");
        });

        Assert.Throws<InvalidOperationException>(() => Publish(firstPublisher, firstOutputs, false, CancellationToken.None));
        Assert.Equal(1, firstCalls);
        Assert.False(File.Exists(fixture.Path("a.pdf")));
        Assert.False(File.Exists(fixture.Path("b.pdf")));

        var laterOutputs = CreateOutputs(
            (fixture.Plan, fixture.Path("one.pdf")),
            (fixture.Plan, fixture.Path("two.pdf")),
            (fixture.Plan, fixture.Path("three.pdf")));
        var laterCalls = 0;
        var laterPublisher = CreatePublisher((_, _, destination, confirmed, _) =>
        {
            laterCalls++;
            Assert.True(confirmed);
            if (laterCalls == 2)
                throw new InvalidOperationException("later failure");
            File.WriteAllText(destination, $"published-{laterCalls}");
        });

        Assert.Throws<InvalidOperationException>(() => Publish(laterPublisher, laterOutputs, true, CancellationToken.None));
        Assert.Equal(2, laterCalls);
        Assert.Equal("published-1", File.ReadAllText(fixture.Path("one.pdf")));
        Assert.False(File.Exists(fixture.Path("two.pdf")));
        Assert.False(File.Exists(fixture.Path("three.pdf")));
    }

    [Fact]
    public void Publish_CancellationStopsBeforeNextWriter()
    {
        using var fixture = BatchFixture.Create();
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        var publisher = CreatePublisher((_, _, destination, _, _) =>
        {
            calls++;
            File.WriteAllText(destination, "ok");
            cancellation.Cancel();
        });
        var outputs = CreateOutputs(
            (fixture.Plan, fixture.Path("one.pdf")),
            (fixture.Plan, fixture.Path("two.pdf")));

        Assert.Throws<OperationCanceledException>(() => Publish(publisher, outputs, false, cancellation.Token));
        Assert.Equal(1, calls);
        Assert.True(File.Exists(fixture.Path("one.pdf")));
        Assert.False(File.Exists(fixture.Path("two.pdf")));
    }

    private static object CreatePublisher(Action<OrganizePlan, string, string, bool, CancellationToken> writer)
    {
        var type = typeof(OrganizePlan).Assembly.GetType(
            "SGPdf.App.Features.Organize.OrganizeBatchPublisher",
            throwOnError: true)!;
        return Activator.CreateInstance(
                   type,
                   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                   binder: null,
                   args: new object?[] { writer },
                   culture: null)
               ?? throw new InvalidOperationException("No se pudo crear OrganizeBatchPublisher.");
    }

    private static Array CreateOutputs(params (OrganizePlan Plan, string Destination)[] items)
    {
        var assembly = typeof(OrganizePlan).Assembly;
        var outputType = assembly.GetType(
            "SGPdf.App.Features.Organize.OrganizePlannedOutput",
            throwOnError: true)!;
        var array = Array.CreateInstance(outputType, items.Length);
        for (var index = 0; index < items.Length; index++)
        {
            var item = Activator.CreateInstance(
                outputType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { items[index].Plan, items[index].Destination },
                culture: null);
            array.SetValue(item, index);
        }
        return array;
    }

    private static void Publish(object publisher, Array outputs, bool warningsConfirmed, CancellationToken token)
    {
        var method = publisher.GetType().GetMethod("Publish", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(publisher.GetType().FullName, "Publish");
        try
        {
            method.Invoke(publisher, new object[] { outputs, warningsConfirmed, token });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    private sealed class BatchFixture : IDisposable
    {
        private BatchFixture(string root, OrganizePlan plan)
        {
            Root = root;
            Plan = plan;
        }

        internal string Root { get; }
        internal OrganizePlan Plan { get; }
        internal string Path(string fileName) => System.IO.Path.Combine(Root, fileName);

        internal static BatchFixture Create()
        {
            var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-batch-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var sourcePath = System.IO.Path.Combine(root, "source.pdf");
            File.WriteAllText(sourcePath, "source");
            var source = OrganizeSource.Capture(sourcePath, 2);
            return new BatchFixture(root, OrganizePlan.FromPrimarySource(source));
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, true);
        }
    }
}
