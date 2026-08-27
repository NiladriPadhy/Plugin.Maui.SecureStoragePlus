using Microsoft.Extensions.Time.Testing;
using Plugin.Maui.SecureStoragePlus.Encryption;
using Plugin.Maui.SecureStoragePlus.Internal;

namespace Plugin.Maui.SecureStoragePlus.Tests;

public sealed class SecureStoragePlusTests
{
    [Fact]
    public async Task SetAndGet_RoundTripsPlaintext()
    {
        var storage = CreateStorage(out _);

        await storage.SetAsync("token", "abc-123");

        Assert.Equal("abc-123", await storage.GetAsync("token"));
        Assert.True(await storage.ContainsKeyAsync("token"));
    }

    [Fact]
    public async Task Set_StoresEncryptedEnvelope_NotPlaintext()
    {
        var backend = new InMemorySecureStorageBackend();
        var storage = CreateStorage(backend, new FakeTimeProvider());

        await storage.SetAsync("token", "super-secret");

        var stored = backend.Snapshot[StorageKeys.ToBackendKey("token")];
        Assert.DoesNotContain("super-secret", stored, StringComparison.Ordinal);
        Assert.Contains("\"v\":1", stored, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_WhenMissing_ReturnsNull()
    {
        var storage = CreateStorage(out _);

        Assert.Null(await storage.GetAsync("missing"));
        var result = await storage.TryGetAsync("missing");
        Assert.False(result.Found);
        Assert.False(result.Expired);
    }

    [Fact]
    public async Task Get_WhenExpired_RemovesValueAndReturnsNull()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero));
        var storage = CreateStorage(new InMemorySecureStorageBackend(), time);

        await storage.SetAsync("session", "alive", SecureStorageOptions.ExpireIn(TimeSpan.FromMinutes(5)));
        time.Advance(TimeSpan.FromMinutes(6));

        var result = await storage.TryGetAsync("session");

        Assert.False(result.Found);
        Assert.True(result.Expired);
        Assert.Null(result.Value);
        Assert.False(await storage.ContainsKeyAsync("session"));
        Assert.Empty(await storage.GetKeysAsync());
    }

    [Fact]
    public async Task Get_BeforeExpiry_ReturnsValue()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero));
        var storage = CreateStorage(new InMemorySecureStorageBackend(), time);

        await storage.SetAsync("session", "alive", SecureStorageOptions.ExpireAt(time.GetUtcNow().AddMinutes(10)));
        time.Advance(TimeSpan.FromMinutes(9));

        Assert.Equal("alive", await storage.GetAsync("session"));
    }

    [Fact]
    public async Task Set_WithPastExpiry_Throws()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero));
        var storage = CreateStorage(new InMemorySecureStorageBackend(), time);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            storage.SetAsync("token", "x", SecureStorageOptions.ExpireAt(time.GetUtcNow().AddMinutes(-1))));
    }

    [Fact]
    public async Task GetMetadata_ReturnsExpiryWithoutValue()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero));
        var storage = CreateStorage(new InMemorySecureStorageBackend(), time);
        var expires = time.GetUtcNow().AddHours(1);

        await storage.SetAsync("token", "hidden-secret", SecureStorageOptions.ExpireAt(expires));
        var metadata = await storage.GetMetadataAsync("token");

        Assert.NotNull(metadata);
        Assert.Equal("token", metadata.Key);
        Assert.Equal(1, metadata.Version);
        Assert.Equal(expires, metadata.ExpiresAt);
        Assert.False(metadata.IsExpired);
    }

    [Fact]
    public async Task SetAsync_Generic_RoundTripsObject()
    {
        var storage = CreateStorage(out _);
        var profile = new UserProfile("Ada", 36);

        await storage.SetAsync("profile", profile);
        var loaded = await storage.GetAsync<UserProfile>("profile");

        Assert.Equal(profile, loaded);
    }

    [Fact]
    public async Task Remove_DeletesValue()
    {
        var storage = CreateStorage(out _);
        await storage.SetAsync("token", "abc");

        Assert.True(await storage.RemoveAsync("token"));
        Assert.Null(await storage.GetAsync("token"));
        Assert.Empty(await storage.GetKeysAsync());
    }

    [Fact]
    public async Task RemoveAll_KeepsEncryptionKeyUnlessReset()
    {
        var backend = new InMemorySecureStorageBackend();
        var storage = CreateStorage(backend, new FakeTimeProvider());
        await storage.SetAsync("a", "1");
        await storage.SetAsync("b", "2");

        await storage.RemoveAllAsync();

        Assert.Empty(await storage.GetKeysAsync());
        Assert.True(backend.Snapshot.ContainsKey(StorageKeys.MetaDek));

        await storage.RemoveAllAsync(resetEncryptionKey: true);
        Assert.False(backend.Snapshot.ContainsKey(StorageKeys.MetaDek));
    }

    [Fact]
    public async Task RemoveExpired_OnlyDeletesExpiredKeys()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero));
        var storage = CreateStorage(new InMemorySecureStorageBackend(), time);

        await storage.SetAsync("short", "1", SecureStorageOptions.ExpireIn(TimeSpan.FromMinutes(1)));
        await storage.SetAsync("long", "2", SecureStorageOptions.ExpireIn(TimeSpan.FromHours(1)));
        await storage.SetAsync("forever", "3");
        time.Advance(TimeSpan.FromMinutes(2));

        var removed = await storage.RemoveExpiredAsync();
        var keys = await storage.GetKeysAsync();

        Assert.Equal(1, removed);
        Assert.Equal(["forever", "long"], keys.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]
    public async Task MigrateAsync_CopiesAndRemovesSource()
    {
        var storage = CreateStorage(out _);
        var source = new InMemoryLegacySource(new Dictionary<string, string> { ["oauth"] = "legacy-token" });

        var result = await storage.MigrateAsync(source, ["oauth", "absent"]);

        Assert.Equal(["oauth"], result.MigratedKeys);
        Assert.Equal(["absent"], result.SkippedKeys);
        Assert.Equal(0, result.Failed);
        Assert.Equal("legacy-token", await storage.GetAsync("oauth"));
        Assert.False(source.Contains("oauth"));
    }

    [Fact]
    public async Task MigrateAsync_SkipsExistingUnlessOverwrite()
    {
        var storage = CreateStorage(out _);
        await storage.SetAsync("oauth", "new-token");
        var source = new InMemoryLegacySource(new Dictionary<string, string> { ["oauth"] = "legacy-token" });

        var skipped = await storage.MigrateAsync(source, ["oauth"]);
        Assert.Equal(["oauth"], skipped.SkippedKeys);
        Assert.Equal("new-token", await storage.GetAsync("oauth"));

        var overwritten = await storage.MigrateAsync(source, ["oauth"], new MigrationOptions { OverwriteExisting = true });
        Assert.Equal(["oauth"], overwritten.MigratedKeys);
        Assert.Equal("legacy-token", await storage.GetAsync("oauth"));
    }

    [Fact]
    public async Task Set_RejectsReservedAndEmptyKeys()
    {
        var storage = CreateStorage(out _);

        await Assert.ThrowsAsync<ArgumentException>(() => storage.SetAsync(" ", "value"));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.SetAsync("ssp.secret", "value"));
    }

    [Fact]
    public async Task GetKeys_ReturnsManagedKeys()
    {
        var storage = CreateStorage(out _);
        await storage.SetAsync("one", "1");
        await storage.SetAsync("two", "2");

        var keys = await storage.GetKeysAsync();

        Assert.Equal(["one", "two"], keys.OrderBy(k => k, StringComparer.Ordinal));
    }

    static ISecureStoragePlus CreateStorage(out InMemorySecureStorageBackend backend)
    {
        backend = new InMemorySecureStorageBackend();
        return CreateStorage(backend, new FakeTimeProvider());
    }

    static ISecureStoragePlus CreateStorage(InMemorySecureStorageBackend backend, FakeTimeProvider time) =>
        new SecureStoragePlusImplementation(backend, new AesGcmDataEncryptor(), time);

    sealed record UserProfile(string Name, int Age);
}
