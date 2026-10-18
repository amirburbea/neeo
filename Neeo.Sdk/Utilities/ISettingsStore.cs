using System;
using System.Collections.Generic;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// A persistent, encrypted key-value store for driver and SDK settings.
/// Mutations automatically persist to disk.
/// </summary>
public interface ISettingsStore : IDictionary<string, string>
{
    /// <summary>
    /// Gets the value associated with the specified key, or <paramref name="defaultValue"/> if the key is not found.
    /// </summary>
    string? Get(string key, string? defaultValue = default);

    /// <summary>
    /// Executes a batch update action on the dictionary, writing to disk only once upon completion of the batch.
    /// </summary>
    void Update(Action<IDictionary<string, string>> action);
}

/// <summary>
/// A persistent, encrypted key-value store associated with the type <typeparamref name="T"/>.
/// </summary>
public interface ISettingsStore<T> : ISettingsStore where T : class;
