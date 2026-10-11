using System.Reflection;
using System.Text;
using SGPdf.App.Features.Comments;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

[Collection(PdfiumNativeGateCollection.Name)]
public sealed class PdfOptimizeRepairCapabilityTests
{
    [Fact]
    public async Task Optimize_RichF8Fixture_PreservesRepresentativeSemanticsWithoutSizePromise()
    {
        using var fixture = F8PreservationFixture.Create();
        var sourceBytes = File.ReadAllBytes(fixture.Path);
        var destination = fixture.NewOutputPath("f9-optimized.pdf");

        var result = await InvokeCapabilityAsync("OptimizeCopyAsync", fixture.Path, destination, CancellationToken.None);

        Assert.True(ReadBool(result, "Succeeded"), ReadString(result, "Error"));
        Assert.True(File.Exists(destination));
        Assert.Equal(sourceBytes, File.ReadAllBytes(fixture.Path));
        Assert.Equal(sourceBytes.LongLength, ReadLong(result, "SourceLength"));
        Assert.Equal(new FileInfo(destination).Length, ReadLong(result, "OutputLength"));
        Assert.True(ReadLong(result, "OutputLength") > 0);

        using var output = PdfDocumentSession.Open(destination);
        Assert.NotEqual(0, output.GetOrganizeFormType());

        var bookmark = Assert.Single(output.GetBookmarks());
        Assert.Equal("F8 Root", bookmark.Title);
        Assert.Equal(0, bookmark.DestinationPageIndex);

        Assert.True(output.HasOrganizeNamedDestinations());
        var internalLink = Assert.Single(
            output.GetPageLinks(0),
            link => link.ActionKind == PdfLinkActionKind.InternalGoto);
        Assert.Equal(1, internalLink.DestinationPageIndex);

        Assert.True(output.IsOrganizeTagged());
        Assert.True(output.HasOrganizePageLabels());
        Assert.True(output.HasOrganizeAttachments());

        var attachment = NativeAttachmentReader.ReadSingle(destination);
        Assert.Equal("f8-note.bin", attachment.Name);
        Assert.Equal("f8-exact-payload-2026", Encoding.UTF8.GetString(attachment.Payload));

        Assert.Equal("SG PDF F8 preservation fixture", output.GetOrganizeMetadataText("Title"));
        Assert.Equal("SG PDF Task 11 F8", output.GetOrganizeMetadataText("Author"));
        Assert.Equal(1, output.GetPageRotation(1));

        var comments = output.GetCommentsOnPage(0);
        Assert.Contains(comments, item =>
            item.Subtype == CommentSubtype.Text &&
            item.Contents == "Existing supported F8");
        Assert.Contains(comments, item => item.Subtype is null);
    }

    [Fact]
    public async Task Optimize_PreCanceled_DoesNotCreateOutputOrLeakJobFile()
    {
        using var fixture = F8PreservationFixture.Create();
        var destination = fixture.NewOutputPath("cancelled.pdf");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await InvokeCapabilityAsync("OptimizeCopyAsync", fixture.Path, destination, cts.Token));

        Assert.False(File.Exists(destination));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.sgpdf-qpdf-job-*.json"));
    }

    [Fact]
    public async Task Repair_BrokenStartXref_IsCharacterizedAsRecoveredWithWarnings()
    {
        using var fixture = F8PreservationFixture.Create();
        var damaged = fixture.NewOutputPath("broken-startxref.pdf");
        var destination = fixture.NewOutputPath("broken-startxref-rewritten.pdf");
        var original = Encoding.Latin1.GetString(File.ReadAllBytes(fixture.Path));
        var startXref = original.LastIndexOf("startxref\n", StringComparison.Ordinal);
        Assert.True(startXref >= 0);
        var numberStart = startXref + "startxref\n".Length;
        var numberEnd = original.IndexOf('\n', numberStart);
        Assert.True(numberEnd > numberStart);
        var damagedText = string.Concat(original.AsSpan(0, numberStart), "0", original.AsSpan(numberEnd));
        File.WriteAllBytes(damaged, Encoding.Latin1.GetBytes(damagedText));
        var damagedBytes = File.ReadAllBytes(damaged);

        var result = await InvokeCapabilityAsync("RepairCopyAsync", damaged, destination, CancellationToken.None);

        Assert.True(ReadBool(result, "OutputCreated"));
        Assert.True(ReadBool(result, "HadWarnings"));
        Assert.Equal(3, ReadInt(result, "ExitCode"));
        Assert.Equal(damagedBytes, File.ReadAllBytes(damaged));
        using var reopened = PdfDocumentSession.Open(destination);
        Assert.Equal(2, reopened.PageCount);
    }

    [Fact]
    public async Task Repair_SeverelyTruncatedInput_IsCharacterizedAsUnrecoverable()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f9-repair-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var damaged = Path.Combine(directory, "severely-truncated.pdf");
            var destination = Path.Combine(directory, "repair-output.pdf");
            var damagedBytes = Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n<< /Type /Catalog");
            File.WriteAllBytes(damaged, damagedBytes);

            var result = await InvokeCapabilityAsync("RepairCopyAsync", damaged, destination, CancellationToken.None);

            Assert.False(ReadBool(result, "OutputCreated"));
            Assert.Equal(2, ReadInt(result, "ExitCode"));
            Assert.Equal(damagedBytes, File.ReadAllBytes(damaged));
            Assert.False(File.Exists(destination));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Repair_ProductGate_RemainsNoGoForSyntheticCharacterizationCorpus()
    {
        var adapterType = typeof(PdfDocumentSession).Assembly.GetType(
            "SGPdf.App.Features.Utilities.PdfOptimizeRepairCapabilityAdapter");
        Assert.NotNull(adapterType);
        var property = adapterType.GetProperty(
            "RepairProductDecision",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);

        Assert.Equal("NoGo", property.GetValue(null)?.ToString());
    }

    private static async Task<object> InvokeCapabilityAsync(
        string methodName,
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var adapterType = typeof(PdfDocumentSession).Assembly.GetType(
            "SGPdf.App.Features.Utilities.PdfOptimizeRepairCapabilityAdapter");
        Assert.NotNull(adapterType);
        var method = adapterType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(candidate =>
                candidate.Name == methodName &&
                candidate.GetParameters().Length == 3);
        Assert.NotNull(method);

        var task = Assert.IsAssignableFrom<Task>(method.Invoke(null, [sourcePath, destinationPath, cancellationToken]));
        await task;
        var result = task.GetType().GetProperty("Result")?.GetValue(task);
        Assert.NotNull(result);
        return result;
    }

    private static bool ReadBool(object result, string name)
        => Assert.IsType<bool>(ReadProperty(result, name));

    private static int ReadInt(object result, string name)
        => Assert.IsType<int>(ReadProperty(result, name));

    private static long ReadLong(object result, string name)
        => Assert.IsType<long>(ReadProperty(result, name));

    private static string ReadString(object result, string name)
        => ReadProperty(result, name) as string ?? string.Empty;

    private static object? ReadProperty(object result, string name)
    {
        var property = result.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return property.GetValue(result);
    }
}
