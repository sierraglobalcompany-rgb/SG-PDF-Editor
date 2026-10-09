using System.Text.Json;
using SGPdf.App.Features.Reader;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class RecentPdfStoreTests
{
    [Fact]
    public void MissingManifest_LoadReturnsEmptyWithoutCreatingRoot()
    {
        using var fixture = TempRoot.Create();
        var store = new RecentPdfStore(fixture.RootPath);

        var entries = store.Load();

        Assert.Empty(entries);
        Assert.False(Directory.Exists(fixture.RootPath));
    }

    [Fact]
    public void RecordSuccessfulOpen_NormalizesDeduplicatesMovesToTopAndCapsTen()
    {
        using var fixture = TempRoot.Create();
        var store = new RecentPdfStore(fixture.RootPath);
        var start = new DateTimeOffset(2026, 10, 8, 20, 0, 0, TimeSpan.Zero);

        for (var index = 0; index < 11; index++)
        {
            var path = Path.Combine(fixture.SourcePath, $"doc-{index}.pdf");
            store.RecordSuccessfulOpen(path, start.AddMinutes(index));
        }

        var duplicate = Path.Combine(fixture.SourcePath, ".", "DOC-5.PDF");
        var latest = start.AddHours(2);
        var entries = store.RecordSuccessfulOpen(duplicate, latest);

        Assert.Equal(10, entries.Count);
        Assert.Equal(Path.GetFullPath(duplicate), entries[0].FullPath);
        Assert.Equal(latest, entries[0].LastOpenedUtc);
        Assert.Single(entries, item => string.Equals(item.FullPath, Path.GetFullPath(duplicate), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(entries, item => item.FullPath.EndsWith("doc-0.pdf", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Manifest_StoresOnlyVersionPathAndUtcTimestamp()
    {
        using var fixture = TempRoot.Create();
        var store = new RecentPdfStore(fixture.RootPath);
        var opened = new DateTimeOffset(2026, 10, 8, 15, 30, 0, TimeSpan.FromHours(-5));
        var path = Path.Combine(fixture.SourcePath, "sample.pdf");

        store.RecordSuccessfulOpen(path, opened);

        var manifestPath = Path.Combine(fixture.RootPath, "recent-files.json");
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        var item = Assert.Single(root.GetProperty("items").EnumerateArray());
        Assert.Equal(new[] { "fullPath", "lastOpenedUtc" }, item.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
        Assert.Equal(Path.GetFullPath(path), item.GetProperty("fullPath").GetString());
        Assert.Equal(opened.ToUniversalTime(), item.GetProperty("lastOpenedUtc").GetDateTimeOffset());
    }

    [Fact]
    public void CorruptManifest_LoadIsNonDestructive_ThenSuccessfulOpenRebuildsValidV1()
    {
        using var fixture = TempRoot.Create(createRoot: true);
        var manifestPath = Path.Combine(fixture.RootPath, "recent-files.json");
        const string corrupt = "{ definitely-not-json";
        File.WriteAllText(manifestPath, corrupt);
        var store = new RecentPdfStore(fixture.RootPath);

        Assert.Empty(store.Load());
        Assert.Equal(corrupt, File.ReadAllText(manifestPath));

        var opened = new DateTimeOffset(2026, 10, 8, 21, 0, 0, TimeSpan.Zero);
        var path = Path.Combine(fixture.SourcePath, "recovered.pdf");
        var entries = store.RecordSuccessfulOpen(path, opened);

        var entry = Assert.Single(entries);
        Assert.Equal(Path.GetFullPath(path), entry.FullPath);
        using var rebuilt = JsonDocument.Parse(File.ReadAllText(manifestPath));
        Assert.Equal(1, rebuilt.RootElement.GetProperty("version").GetInt32());
    }

    [Fact]
    public void PublicationFailure_LeavesPreviousManifestUntouched()
    {
        using var fixture = TempRoot.Create();
        var firstStore = new RecentPdfStore(fixture.RootPath);
        var firstPath = Path.Combine(fixture.SourcePath, "first.pdf");
        firstStore.RecordSuccessfulOpen(firstPath, DateTimeOffset.UtcNow.AddMinutes(-1));
        var manifestPath = Path.Combine(fixture.RootPath, "recent-files.json");
        var previous = File.ReadAllText(manifestPath);

        var fileOps = new RecentPdfFileOps();
        fileOps.ReplaceFile = (_, _) => throw new IOException("synthetic replace failure");
        var failingStore = new RecentPdfStore(fixture.RootPath, fileOps);

        Assert.Throws<IOException>(() => failingStore.RecordSuccessfulOpen(
            Path.Combine(fixture.SourcePath, "second.pdf"),
            DateTimeOffset.UtcNow));
        Assert.Equal(previous, File.ReadAllText(manifestPath));
    }

    [Fact]
    public void Clear_RemovesOnlyRecentMetadata()
    {
        using var fixture = TempRoot.Create();
        var pdfPath = Path.Combine(fixture.SourcePath, "keep.pdf");
        Directory.CreateDirectory(fixture.SourcePath);
        File.WriteAllText(pdfPath, "not-a-real-pdf");
        var store = new RecentPdfStore(fixture.RootPath);
        store.RecordSuccessfulOpen(pdfPath, DateTimeOffset.UtcNow);

        store.Clear();

        Assert.True(File.Exists(pdfPath));
        Assert.False(File.Exists(Path.Combine(fixture.RootPath, "recent-files.json")));
    }

    [Fact]
    public void Load_ReturnsMissingAndUncLookingPathsWithoutTargetProbeOrFiltering()
    {
        using var fixture = TempRoot.Create(createRoot: true);
        var localMissing = Path.Combine(fixture.SourcePath, "missing.pdf");
        const string uncMissing = @"\\server-that-should-not-be-contacted\share\missing.pdf";
        var json = $$"""
        {
          "version": 1,
          "items": [
            { "fullPath": "{{JsonEncodedText.Encode(localMissing)}}", "lastOpenedUtc": "2026-10-08T20:00:00Z" },
            { "fullPath": "\\\\server-that-should-not-be-contacted\\share\\missing.pdf", "lastOpenedUtc": "2026-10-08T19:00:00Z" }
          ]
        }
        """;
        File.WriteAllText(Path.Combine(fixture.RootPath, "recent-files.json"), json);
        var store = new RecentPdfStore(fixture.RootPath);

        var entries = store.Load();

        Assert.Equal(2, entries.Count);
        Assert.Equal(Path.GetFullPath(localMissing), entries[0].FullPath);
        Assert.Equal(uncMissing, entries[1].FullPath);
    }

    private sealed class TempRoot : IDisposable
    {
        private TempRoot(string containerPath, string rootPath, string sourcePath)
        {
            ContainerPath = containerPath;
            RootPath = rootPath;
            SourcePath = sourcePath;
        }

        internal string ContainerPath { get; }
        internal string RootPath { get; }
        internal string SourcePath { get; }

        internal static TempRoot Create(bool createRoot = false)
        {
            var container = Path.Combine(Path.GetTempPath(), $"sgpdf-recents-{Guid.NewGuid():N}");
            Directory.CreateDirectory(container);
            var root = Path.Combine(container, "recent-store");
            var source = Path.Combine(container, "source");
            if (createRoot)
                Directory.CreateDirectory(root);
            return new TempRoot(container, root, source);
        }

        public void Dispose()
        {
            if (Directory.Exists(ContainerPath))
                Directory.Delete(ContainerPath, true);
        }
    }
}
