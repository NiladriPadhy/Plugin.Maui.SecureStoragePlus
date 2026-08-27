namespace Plugin.Maui.SecureStoragePlus;

/// <summary>
/// Registers <see cref="ISecureStoragePlus"/> with the MAUI dependency injection container.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="SecureStoragePlus.Default"/> as a singleton <see cref="ISecureStoragePlus"/>.
    /// </summary>
    public static MauiAppBuilder UseSecureStoragePlus(this MauiAppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSingleton(SecureStoragePlus.Default);
        return builder;
    }
}
