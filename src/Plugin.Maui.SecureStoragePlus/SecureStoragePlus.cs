namespace Plugin.Maui.SecureStoragePlus;

/// <summary>
/// Entry point for static usage of <see cref="ISecureStoragePlus"/>.
/// </summary>
public static class SecureStoragePlus
{
    static ISecureStoragePlus? defaultImplementation;

    /// <summary>
    /// Provides the default implementation for static usage of this API.
    /// </summary>
    public static ISecureStoragePlus Default =>
        defaultImplementation ??= new SecureStoragePlusImplementation();

    internal static void SetDefault(ISecureStoragePlus? implementation) =>
        defaultImplementation = implementation;
}
