namespace Plugin.Maui.SecureStoragePlus;

/// <summary>
/// Thrown when a stored value cannot be decrypted or the envelope is corrupt.
/// </summary>
public sealed class SecureStoragePlusException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecureStoragePlusException"/> class.
    /// </summary>
    public SecureStoragePlusException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecureStoragePlusException"/> class.
    /// </summary>
    public SecureStoragePlusException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
