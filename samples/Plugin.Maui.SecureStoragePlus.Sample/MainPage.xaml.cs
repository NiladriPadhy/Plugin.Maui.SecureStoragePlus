using Microsoft.Maui.Storage;

namespace Plugin.Maui.SecureStoragePlus.Sample;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        ExpiryPicker.SelectedIndex = 0;
    }

    async void OnSaveClicked(object? sender, EventArgs e)
    {
        try
        {
            await SecureStoragePlus.Default.SetAsync(KeyEntry.Text, ValueEntry.Text, SelectedExpiry());
            StatusLabel.Text = $"Saved '{KeyEntry.Text}'.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }

    async void OnGetClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = await SecureStoragePlus.Default.TryGetAsync(KeyEntry.Text);
            if (result.Expired)
            {
                StatusLabel.Text = $"'{KeyEntry.Text}' expired and was removed.";
                return;
            }

            if (!result.Found)
            {
                StatusLabel.Text = $"'{KeyEntry.Text}' was not found.";
                return;
            }

            ValueEntry.Text = result.Value;
            var metadata = await SecureStoragePlus.Default.GetMetadataAsync(KeyEntry.Text);
            var expiry = metadata?.ExpiresAt is { } expiresAt ? expiresAt.ToString("u") : "never";
            StatusLabel.Text = $"Value: {result.Value}{Environment.NewLine}Expires: {expiry}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }

    async void OnRemoveClicked(object? sender, EventArgs e)
    {
        try
        {
            var removed = await SecureStoragePlus.Default.RemoveAsync(KeyEntry.Text);
            StatusLabel.Text = removed ? $"Removed '{KeyEntry.Text}'." : $"'{KeyEntry.Text}' was not stored.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }

    async void OnListClicked(object? sender, EventArgs e)
    {
        try
        {
            var keys = await SecureStoragePlus.Default.GetKeysAsync();
            StatusLabel.Text = keys.Count == 0 ? "No keys stored." : string.Join(Environment.NewLine, keys);
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }

    async void OnRemoveExpiredClicked(object? sender, EventArgs e)
    {
        try
        {
            var removed = await SecureStoragePlus.Default.RemoveExpiredAsync();
            StatusLabel.Text = $"Removed {removed} expired key(s).";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }

    async void OnRemoveAllClicked(object? sender, EventArgs e)
    {
        try
        {
            await SecureStoragePlus.Default.RemoveAllAsync();
            StatusLabel.Text = "Removed all SecureStoragePlus values.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }

    async void OnMigrateClicked(object? sender, EventArgs e)
    {
        try
        {
            await SecureStorage.SetAsync("legacy_token", "migrated-from-maui-secure-storage");
            var result = await SecureStoragePlus.Default.MigrateFromMauiSecureStorageAsync(
                ["legacy_token"],
                new MigrationOptions
                {
                    RemoveSource = true,
                    OverwriteExisting = true,
                    StorageOptions = SecureStorageOptions.ExpireIn(TimeSpan.FromMinutes(2))
                });

            var migratedValue = await SecureStoragePlus.Default.GetAsync("legacy_token");
            StatusLabel.Text =
                $"Migrated: {result.Migrated}, skipped: {result.Skipped}, failed: {result.Failed}{Environment.NewLine}" +
                $"legacy_token = {migratedValue}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }

    SecureStorageOptions? SelectedExpiry() =>
        ExpiryPicker.SelectedItem switch
        {
            "10 seconds" => SecureStorageOptions.ExpireIn(TimeSpan.FromSeconds(10)),
            "1 minute" => SecureStorageOptions.ExpireIn(TimeSpan.FromMinutes(1)),
            "1 hour" => SecureStorageOptions.ExpireIn(TimeSpan.FromHours(1)),
            _ => null
        };
}
