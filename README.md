# Plugin.Maui.SecureStoragePlus

Better secure storage for .NET MAUI on **iOS** and **Android**. Values are encrypted with AES-256-GCM, can expire automatically, and can be migrated from MAUI `SecureStorage` or a custom legacy store.

[![NuGet](https://img.shields.io/nuget/v/Plugin.Maui.SecureStoragePlus.svg?label=NuGet)](https://www.nuget.org/packages/Plugin.Maui.SecureStoragePlus)

## Why this package

MAUI `SecureStorage` already uses Keychain (iOS) and EncryptedSharedPreferences (Android). SecureStoragePlus adds:

- An extra **AES-256-GCM** layer with a device-bound data-encryption key
- **Integrity** via GCM authentication tags, bound to the key name
- **Expiry** (`ExpiresIn` / `ExpiresAt`) with automatic purge on read
- **Migration** from MAUI `SecureStorage` or any `ILegacyStorageSource`
- Key listing, metadata, typed JSON get/set, and DI registration

## Install

```bash
dotnet add package Plugin.Maui.SecureStoragePlus
```

Register the plugin in `MauiProgram.cs`:

```csharp
builder
    .UseMauiApp<App>()
    .UseSecureStoragePlus();
```

Then inject `ISecureStoragePlus` or call `SecureStoragePlus.Default`.

## Usage

### Store and read

```csharp
await SecureStoragePlus.Default.SetAsync("oauth_token", accessToken);

var token = await SecureStoragePlus.Default.GetAsync("oauth_token");
```

### Expiry

```csharp
await SecureStoragePlus.Default.SetAsync(
    "session",
    sessionJson,
    SecureStorageOptions.ExpireIn(TimeSpan.FromHours(8)));

await SecureStoragePlus.Default.SetAsync(
    "otp",
    code,
    SecureStorageOptions.ExpireAt(DateTimeOffset.UtcNow.AddMinutes(5)));
```

Expired values are removed on the next read and `GetAsync` returns `null`.

```csharp
var result = await SecureStoragePlus.Default.TryGetAsync("session");
if (result.Expired)
{
    // prompt the user to sign in again
}
```

### Typed values

```csharp
await SecureStoragePlus.Default.SetAsync("profile", new UserProfile("Ada", 36));
var profile = await SecureStoragePlus.Default.GetAsync<UserProfile>("profile");
```

Strings are stored as-is. Other types are JSON-serialized.

### Inspect and clean up

```csharp
var keys = await SecureStoragePlus.Default.GetKeysAsync();
var metadata = await SecureStoragePlus.Default.GetMetadataAsync("session");

await SecureStoragePlus.Default.RemoveExpiredAsync();
await SecureStoragePlus.Default.RemoveAsync("oauth_token");
await SecureStoragePlus.Default.RemoveAllAsync();
```

### Dependency injection

```csharp
public sealed class AuthService(ISecureStoragePlus storage)
{
    public Task SaveTokenAsync(string token) =>
        storage.SetAsync("oauth_token", token, SecureStorageOptions.ExpireIn(TimeSpan.FromDays(14)));
}
```

## Migration

### From MAUI SecureStorage

```csharp
var result = await SecureStoragePlus.Default.MigrateFromMauiSecureStorageAsync(
    ["oauth_token", "refresh_token"],
    new MigrationOptions
    {
        RemoveSource = true,
        OverwriteExisting = false,
        StorageOptions = SecureStorageOptions.ExpireIn(TimeSpan.FromDays(14))
    });
```

Call this once during startup after an app upgrade. Successfully migrated keys are copied into the encrypted envelope and, by default, removed from MAUI `SecureStorage`.

### From Xamarin.Essentials or another store

Use `DelegateLegacyStorageSource` with your existing reader (for example `LegacySecureStorage` from [Plugin.Maui.FormsMigration](https://github.com/jfversluis/Plugin.Maui.FormsMigration)):

```csharp
var source = new DelegateLegacyStorageSource(
    key => LegacySecureStorage.GetAsync(key),
    key => Task.FromResult(LegacySecureStorage.Remove(key)));

await SecureStoragePlus.Default.MigrateAsync(source, ["oauth_token"]);
```

## Platform notes

### iOS

Add a Keychain entitlement so values persist correctly. In `Entitlements.plist`:

```xml
<key>keychain-access-groups</key>
<array>
    <string>$(AppIdentifierPrefix)$(CFBundleIdentifier)</string>
</array>
```

Set that entitlements file on the iOS bundle signing settings.

### Android

Secure storage is backed by EncryptedSharedPreferences. If Android Auto Backup restores preferences onto a new device without the original key, reads can fail. Wrap first-run reads in try/catch and call `RemoveAllAsync(resetEncryptionKey: true)` if decryption fails after restore.

Minimum versions:

- iOS 15.0
- Android API 21
- .NET 10 / .NET MAUI 10

## How it works

1. A 256-bit data-encryption key is created once and stored in MAUI `SecureStorage` (Keychain / Android Keystore-backed storage).
2. Each value is encrypted with AES-256-GCM. The key name is used as associated data so a blob cannot be copied under another key.
3. The ciphertext is wrapped in a versioned envelope that also stores `createdAt` and optional `expiresAt`.
4. An internal index tracks keys so the plugin can list, expire, and remove only its own values.

Envelope version `1` is the current format. Future versions can migrate on read without changing the public API.

## Sample

See [`samples/Plugin.Maui.SecureStoragePlus.Sample`](samples/Plugin.Maui.SecureStoragePlus.Sample) for a MAUI app that saves, reads, expires, and migrates values.

## Pack locally

```bash
dotnet pack src/Plugin.Maui.SecureStoragePlus/Plugin.Maui.SecureStoragePlus.csproj -c Release
```

The nupkg is written to `artifacts/`.

## License

MIT
