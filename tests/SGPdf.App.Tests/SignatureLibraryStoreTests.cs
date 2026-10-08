using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Sign;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class SignatureLibraryStoreTests
{
    [Fact]
    public void DefaultRoot_UsesCurrentUsersLocalApplicationData()
    {
        var store = new SignatureLibraryStore();

        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SG PDF Editor",
            "Signatures");

        Assert.Equal(expected, store.RootDirectory);
    }

    [Fact]
    public void Load_MissingRoot_ReturnsEmptyWithoutCreatingDirectory()
    {
        var root = NewRoot();
        Assert.False(Directory.Exists(root));

        var items = new SignatureLibraryStore(root).Load();

        Assert.Empty(items);
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public void Load_MissingManifest_ReturnsEmptyWithoutCreatingManifest()
    {
        var root = NewRoot();
        Directory.CreateDirectory(root);
        try
        {
            var items = new SignatureLibraryStore(root).Load();

            Assert.Empty(items);
            Assert.False(File.Exists(Path.Combine(root, "library.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Load_ValidV1_PreservesManifestOrderAndMetadata()
    {
        var root = NewRoot();
        Directory.CreateDirectory(root);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        try
        {
            WriteManifest(root, new
            {
                version = 1,
                items = new object[]
                {
                    new { id = first, displayName = "Firma B", fileName = $"{first:N}.png" },
                    new { id = second, displayName = "Firma A", fileName = $"{second:N}.png" }
                }
            });

            var items = new SignatureLibraryStore(root).Load();

            Assert.Collection(
                items,
                item =>
                {
                    Assert.Equal(first, item.Id);
                    Assert.Equal("Firma B", item.DisplayName);
                    Assert.Equal($"{first:N}.png", item.FileName);
                },
                item =>
                {
                    Assert.Equal(second, item.Id);
                    Assert.Equal("Firma A", item.DisplayName);
                    Assert.Equal($"{second:N}.png", item.FileName);
                });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Load_InvalidJson_ThrowsControlledUnavailableAndDoesNotRewriteFile()
    {
        var root = NewRoot();
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "library.json");
        var original = "{ definitely not json"u8.ToArray();
        File.WriteAllBytes(path, original);
        try
        {
            Assert.Throws<SignatureLibraryUnavailableException>(() => new SignatureLibraryStore(root).Load());
            Assert.Equal(original, File.ReadAllBytes(path));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Load_UnsupportedVersion_ThrowsControlledUnavailableAndDoesNotRewriteFile()
    {
        var root = NewRoot();
        Directory.CreateDirectory(root);
        WriteManifest(root, new { version = 2, items = Array.Empty<object>() });
        var path = Path.Combine(root, "library.json");
        var original = File.ReadAllBytes(path);
        try
        {
            Assert.Throws<SignatureLibraryUnavailableException>(() => new SignatureLibraryStore(root).Load());
            Assert.Equal(original, File.ReadAllBytes(path));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Load_DuplicateGuid_ThrowsControlledUnavailable()
    {
        var root = NewRoot();
        Directory.CreateDirectory(root);
        var id = Guid.NewGuid();
        try
        {
            WriteManifest(root, new
            {
                version = 1,
                items = new object[]
                {
                    new { id, displayName = "Uno", fileName = $"{id:N}.png" },
                    new { id, displayName = "Dos", fileName = $"other-{id:N}.png" }
                }
            });

            Assert.Throws<SignatureLibraryUnavailableException>(() => new SignatureLibraryStore(root).Load());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("../escape.png")]
    [InlineData("sub/firma.png")]
    [InlineData("sub\\firma.png")]
    [InlineData("C:\\firma.png")]
    [InlineData("firma.jpg")]
    public void Load_UnsafeFileName_RejectsTraversalRootedPathAndNonPng(string fileName)
    {
        var root = NewRoot();
        Directory.CreateDirectory(root);
        var id = Guid.NewGuid();
        try
        {
            WriteManifest(root, new
            {
                version = 1,
                items = new[] { new { id, displayName = "Firma", fileName } }
            });

            Assert.Throws<SignatureLibraryUnavailableException>(() => new SignatureLibraryStore(root).Load());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Load_OrphanPng_IsIgnoredAndNotDeleted()
    {
        var root = NewRoot();
        Directory.CreateDirectory(root);
        var orphan = Path.Combine(root, $"{Guid.NewGuid():N}.png");
        File.WriteAllBytes(orphan, new byte[] { 1, 2, 3, 4 });
        WriteManifest(root, new { version = 1, items = Array.Empty<object>() });
        try
        {
            var items = new SignatureLibraryStore(root).Load();

            Assert.Empty(items);
            Assert.True(File.Exists(orphan));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, File.ReadAllBytes(orphan));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LoadAsset_UsesValidatedManifestEntryAndExistingPngLoader()
    {
        var root = NewRoot();
        Directory.CreateDirectory(root);
        var id = Guid.NewGuid();
        var fileName = $"{id:N}.png";
        try
        {
            WriteManifest(root, new
            {
                version = 1,
                items = new[] { new { id, displayName = "Firma", fileName } }
            });
            CreatePng(Path.Combine(root, fileName), opaque: true);

            var error = Assert.Throws<InvalidDataException>(() => new SignatureLibraryStore(root).LoadAsset(id));
            Assert.Contains("transpar", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Add_TrimsNameAndGeneratesGuidBasedPngName()
    {
        var root = NewRoot();
        try
        {
            var item = new SignatureLibraryStore(root).Add("  Firma principal  ", CreateAsset());

            Assert.Equal("Firma principal", item.DisplayName);
            Assert.NotEqual(Guid.Empty, item.Id);
            Assert.Equal($"{item.Id:N}.png", item.FileName);
            Assert.True(File.Exists(Path.Combine(root, item.FileName)));
            Assert.Single(new SignatureLibraryStore(root).Load());
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public void Add_RejectsWhitespaceWithoutCreatingRoot()
    {
        var root = NewRoot();

        Assert.Throws<ArgumentException>(() => new SignatureLibraryStore(root).Add("   ", CreateAsset()));
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public void Add_RejectsCaseInsensitiveDuplicateWithoutMutation()
    {
        var root = NewRoot();
        try
        {
            var store = new SignatureLibraryStore(root);
            var first = store.Add("Firma Única", CreateAsset());
            var manifestBefore = File.ReadAllBytes(Path.Combine(root, "library.json"));
            var pngBefore = File.ReadAllBytes(Path.Combine(root, first.FileName));

            Assert.Throws<ArgumentException>(() => store.Add("  firma única  ", CreateAsset()));

            Assert.Equal(manifestBefore, File.ReadAllBytes(Path.Combine(root, "library.json")));
            Assert.Equal(pngBefore, File.ReadAllBytes(Path.Combine(root, first.FileName)));
            Assert.Single(store.Load());
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public void Add_PublishesPngBeforeManifestReferencesIt()
    {
        var root = NewRoot();
        var observedPngBeforeManifest = false;
        var ops = DefaultOps();
        ops.PublishNewFile = (source, destination) =>
        {
            if (Path.GetFileName(destination).Equals("library.json", StringComparison.OrdinalIgnoreCase))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(source));
                var fileName = document.RootElement.GetProperty("items")[0].GetProperty("fileName").GetString()!;
                observedPngBeforeManifest = File.Exists(Path.Combine(root, fileName));
            }

            File.Move(source, destination);
        };

        try
        {
            new SignatureLibraryStore(root, ops).Add("Firma", CreateAsset());
            Assert.True(observedPngBeforeManifest);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public void Add_RoundTrip_PreservesDimensionsAndAlphaSemantics()
    {
        var root = NewRoot();
        var source = CreateAsset();
        try
        {
            var store = new SignatureLibraryStore(root);
            var item = store.Add("Firma", source);
            var loaded = store.LoadAsset(item.Id);

            Assert.Equal(source.PixelWidth, loaded.PixelWidth);
            Assert.Equal(source.PixelHeight, loaded.PixelHeight);
            Assert.Equal(source.Stride, loaded.Stride);
            Assert.Equal(AlphaBytes(source), AlphaBytes(loaded));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public void Add_DisplayNameCharactersNeverAffectPhysicalFilename()
    {
        var root = NewRoot();
        try
        {
            var item = new SignatureLibraryStore(root).Add("Firma: Andrés / Cliente? ★", CreateAsset());

            Assert.Equal("Firma: Andrés / Cliente? ★", item.DisplayName);
            Assert.Equal($"{item.Id:N}.png", item.FileName);
            Assert.DoesNotContain("Andrés", item.FileName, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public void Add_PngPublicationFailure_LeavesOldManifestUnchanged()
    {
        var root = NewRoot();
        try
        {
            var seedStore = new SignatureLibraryStore(root);
            seedStore.Add("Existente", CreateAsset());
            var manifestPath = Path.Combine(root, "library.json");
            var manifestBefore = File.ReadAllBytes(manifestPath);

            var ops = DefaultOps();
            ops.PublishNewFile = (source, destination) =>
            {
                if (destination.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    throw new IOException("png publish failed");
                File.Move(source, destination);
            };

            Assert.Throws<IOException>(() => new SignatureLibraryStore(root, ops).Add("Nueva", CreateAsset()));
            Assert.Equal(manifestBefore, File.ReadAllBytes(manifestPath));
            Assert.Single(seedStore.Load());
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public void Add_ManifestPublicationFailure_LeavesOldManifestAuthoritativeAndRemovesCurrentPngBestEffort()
    {
        var root = NewRoot();
        try
        {
            var seedStore = new SignatureLibraryStore(root);
            var existing = seedStore.Add("Existente", CreateAsset());
            var manifestPath = Path.Combine(root, "library.json");
            var manifestBefore = File.ReadAllBytes(manifestPath);

            var ops = DefaultOps();
            ops.ReplaceFile = (source, destination) =>
            {
                if (Path.GetFileName(destination).Equals("library.json", StringComparison.OrdinalIgnoreCase))
                    throw new IOException("manifest replace failed");
                File.Move(source, destination, overwrite: true);
            };

            Assert.Throws<IOException>(() => new SignatureLibraryStore(root, ops).Add("Nueva", CreateAsset()));

            Assert.Equal(manifestBefore, File.ReadAllBytes(manifestPath));
            Assert.Equal(new[] { existing.FileName }, Directory.GetFiles(root, "*.png").Select(Path.GetFileName).ToArray());
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public void Add_ManifestFailureAndRollbackDeleteFailure_LeavesOnlyIgnoredOrphan()
    {
        var root = NewRoot();
        try
        {
            var store = new SignatureLibraryStore(root);
            var existing = store.Add("Existente", CreateAsset());
            var ops = DefaultOps();
            ops.ReplaceFile = (_, destination) => throw new IOException($"replace failed: {destination}");
            ops.DeleteFile = _ => throw new IOException("rollback delete failed");

            Assert.Throws<IOException>(() => new SignatureLibraryStore(root, ops).Add("Nueva", CreateAsset()));

            var loaded = store.Load();
            Assert.Single(loaded);
            Assert.Equal(existing.Id, loaded[0].Id);
            Assert.Equal(2, Directory.GetFiles(root, "*.png").Length);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public void Add_Failure_CleansOnlyFilesCreatedByCurrentOperation()
    {
        var root = NewRoot();
        try
        {
            var store = new SignatureLibraryStore(root);
            store.Add("Existente", CreateAsset());
            var unrelated = Path.Combine(root, $"{Guid.NewGuid():N}.png");
            File.WriteAllBytes(unrelated, new byte[] { 9, 8, 7, 6 });

            var ops = DefaultOps();
            ops.ReplaceFile = (_, _) => throw new IOException("manifest replace failed");

            Assert.Throws<IOException>(() => new SignatureLibraryStore(root, ops).Add("Nueva", CreateAsset()));
            Assert.True(File.Exists(unrelated));
            Assert.Equal(new byte[] { 9, 8, 7, 6 }, File.ReadAllBytes(unrelated));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public void Rename_ChangesOnlyManifestNameAndKeepsGuidFilenameAndPngBytes()
    {
        var root = NewRoot();
        try
        {
            var store = new SignatureLibraryStore(root);
            var item = store.Add("Vieja", CreateAsset());
            var pngPath = Path.Combine(root, item.FileName);
            var pngBefore = File.ReadAllBytes(pngPath);

            var renamed = store.Rename(item.Id, "  Nueva  ");

            Assert.Equal(item.Id, renamed.Id);
            Assert.Equal(item.FileName, renamed.FileName);
            Assert.Equal("Nueva", renamed.DisplayName);
            Assert.Equal(pngBefore, File.ReadAllBytes(pngPath));
            Assert.Equal("Nueva", Assert.Single(store.Load()).DisplayName);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public void Rename_ManifestPublicationFailure_LeavesOldNameAuthoritative()
    {
        var root = NewRoot();
        try
        {
            var store = new SignatureLibraryStore(root);
            var item = store.Add("Vieja", CreateAsset());
            var manifestBefore = File.ReadAllBytes(Path.Combine(root, "library.json"));
            var ops = DefaultOps();
            ops.ReplaceFile = (_, _) => throw new IOException("replace failed");

            Assert.Throws<IOException>(() => new SignatureLibraryStore(root, ops).Rename(item.Id, "Nueva"));

            Assert.Equal(manifestBefore, File.ReadAllBytes(Path.Combine(root, "library.json")));
            Assert.Equal("Vieja", Assert.Single(store.Load()).DisplayName);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public void Delete_ManifestPublicationFailure_KeepsEntryAndPng()
    {
        var root = NewRoot();
        try
        {
            var store = new SignatureLibraryStore(root);
            var item = store.Add("Firma", CreateAsset());
            var pngPath = Path.Combine(root, item.FileName);
            var ops = DefaultOps();
            ops.ReplaceFile = (_, _) => throw new IOException("replace failed");

            Assert.Throws<IOException>(() => new SignatureLibraryStore(root, ops).Delete(item.Id));

            Assert.True(File.Exists(pngPath));
            Assert.Equal(item.Id, Assert.Single(store.Load()).Id);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public void Delete_SuccessfulManifestThenPngDeleteFailure_RemainsLogicallyDeletedAndReturnsWarning()
    {
        var root = NewRoot();
        try
        {
            var store = new SignatureLibraryStore(root);
            var item = store.Add("Firma", CreateAsset());
            var pngPath = Path.Combine(root, item.FileName);
            var ops = DefaultOps();
            ops.DeleteFile = _ => throw new IOException("delete failed");

            var result = new SignatureLibraryStore(root, ops).Delete(item.Id);

            Assert.False(result.FileCleanupSucceeded);
            Assert.False(string.IsNullOrWhiteSpace(result.Warning));
            Assert.Empty(store.Load());
            Assert.True(File.Exists(pngPath));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public void Delete_Success_RemovesMetadataThenBackingPng()
    {
        var root = NewRoot();
        try
        {
            var store = new SignatureLibraryStore(root);
            var first = store.Add("Uno", CreateAsset());
            var second = store.Add("Dos", CreateAsset());

            var result = store.Delete(first.Id);

            Assert.True(result.FileCleanupSucceeded);
            Assert.Null(result.Warning);
            Assert.False(File.Exists(Path.Combine(root, first.FileName)));
            Assert.True(File.Exists(Path.Combine(root, second.FileName)));
            Assert.Equal(second.Id, Assert.Single(store.Load()).Id);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    private static SignatureAsset CreateAsset()
    {
        var pixels = new byte[]
        {
            10, 20, 30, 0,
            40, 50, 60, 64,
            70, 80, 90, 128,
            100, 110, 120, 255
        };
        return new SignatureAsset(2, 2, 8, pixels, "test-source");
    }

    private static byte[] AlphaBytes(SignatureAsset asset)
    {
        var pixels = asset.BgraPixels.ToArray();
        return Enumerable.Range(0, asset.PixelWidth * asset.PixelHeight)
            .Select(index => pixels[(index * 4) + 3])
            .ToArray();
    }

    private static SignatureLibraryFileOps DefaultOps()
        => new()
        {
            PublishNewFile = static (source, destination) => File.Move(source, destination),
            ReplaceFile = static (source, destination) => File.Move(source, destination, overwrite: true),
            DeleteFile = static path => File.Delete(path)
        };

    private static string NewRoot()
        => Path.Combine(Path.GetTempPath(), $"sgpdf-signature-library-{Guid.NewGuid():N}");

    private static void WriteManifest(string root, object manifest)
        => File.WriteAllText(
            Path.Combine(root, "library.json"),
            JsonSerializer.Serialize(manifest));

    private static void CreatePng(string path, bool opaque)
    {
        var pixels = new byte[]
        {
            0, 0, 0, opaque ? (byte)255 : (byte)0,
            0, 0, 0, 255
        };
        var bitmap = BitmapSource.Create(
            2,
            1,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            8);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        encoder.Save(stream);
    }

    private static void DeleteRoot(string root)
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}