using TakeTool.Core.Configuration;
using TakeTool.Core.Security;

namespace TakeTool.Core.Tests;

public sealed class JsonConfigurationStoreTests
{
    [Fact]
    public async Task Settings_RoundTrip()
    {
        using var temp = new TempAppData();
        var store = new JsonConfigurationStore(temp, new PassthroughSecretProtector());

        var settings = new AppSettings
        {
            OverlayOpacity = 0.7,
            ExpandOnHover = false,
            EnabledUtilityIds = ["imgbb-uploader"],
            HasInitializedEnabledUtilities = true
        };

        await store.SaveSettingsAsync(settings);
        var loaded = await store.LoadSettingsAsync();

        Assert.Equal(0.7, loaded.OverlayOpacity);
        Assert.False(loaded.ExpandOnHover);
        Assert.Contains("imgbb-uploader", loaded.EnabledUtilityIds);
        Assert.True(loaded.HasInitializedEnabledUtilities);
    }

    [Fact]
    public async Task UtilityConfig_RoundTrip()
    {
        using var temp = new TempAppData();
        var store = new JsonConfigurationStore(temp, new PassthroughSecretProtector());

        await store.SaveUtilityConfigAsync("imgbb-uploader", new Dictionary<string, string>
        {
            ["ApiKey"] = "secret-value"
        });

        var loaded = await store.LoadUtilityConfigAsync("imgbb-uploader");
        Assert.Equal("secret-value", loaded["ApiKey"]);
    }

    [Fact]
    public async Task UtilityConfig_RejectsUnsafeIds()
    {
        using var temp = new TempAppData();
        var store = new JsonConfigurationStore(temp, new PassthroughSecretProtector());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            store.SaveUtilityConfigAsync("../evil", new Dictionary<string, string> { ["a"] = "b" }));
    }

    [Fact]
    public async Task UtilityConfig_PersistsProtectedPayload_WhenProtectorEncrypts()
    {
        using var temp = new TempAppData();
        var protector = new PrefixSecretProtector();
        var store = new JsonConfigurationStore(temp, protector);

        await store.SaveUtilityConfigAsync("imgbb-uploader", new Dictionary<string, string>
        {
            ["ApiKey"] = "secret-value"
        });

        var onDisk = await File.ReadAllTextAsync(
            Path.Combine(temp.GetRootDirectory(), "utilities", "imgbb-uploader.json"));
        Assert.DoesNotContain("secret-value", onDisk);
        Assert.Contains("enc.v1:", onDisk);

        var loaded = await store.LoadUtilityConfigAsync("imgbb-uploader");
        Assert.Equal("secret-value", loaded["ApiKey"]);
    }

    private sealed class TempAppData : IAppDataPathProvider, IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "TakeToolTests-" + Guid.NewGuid().ToString("N"));

        public string GetRootDirectory() => _root;

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }

    private sealed class PrefixSecretProtector : ISecretProtector
    {
        public const string Prefix = "enc.v1:";

        public string Protect(string plaintext)
        {
            if (plaintext.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return plaintext;
            }

            return Prefix + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plaintext));
        }

        public string Unprotect(string storedValue)
        {
            if (!storedValue.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return storedValue;
            }

            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(storedValue[Prefix.Length..]));
        }
    }
}
