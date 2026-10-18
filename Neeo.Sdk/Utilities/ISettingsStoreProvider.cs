namespace Neeo.Sdk.Utilities;

/// <summary>
/// Provides access to persistent, encrypted settings stores.
/// </summary>
public interface ISettingsStoreProvider
{
    /// <summary>
    /// Gets or creates the <see cref="ISettingsStore"/> with the specified name.
    /// </summary>
    ISettingsStore GetStore(string name);

    /// <summary>
    /// Gets or creates the <see cref="ISettingsStore{T}"/> associated with type <typeparamref name="T"/>.
    /// </summary>
    ISettingsStore<T> GetStore<T>() where T : class;
}
