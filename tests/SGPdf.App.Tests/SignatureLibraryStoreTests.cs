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
}
