using System;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Specifies the custom storage file name to use when resolving an <see cref="ISettingsStore{T}"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class SettingsStoreAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name of the store.
    /// </summary>
    public string Name => name;
}
