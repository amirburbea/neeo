﻿using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Components;

/// <summary>
/// Describes a device component.
/// </summary>
[JsonDirectSerialization<IComponent>]
public interface IComponent
{
    /// <summary>
    /// Gets the (optional) name of the component.
    /// </summary>
    string? Label { get; }

    /// <summary>
    /// Gets the name of the component.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the API path to interact with the component.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Gets the type of the component.
    /// </summary>
    ComponentType Type { get; }
}

internal record class Component(
    ComponentType Type,
    string Name,
    string? Label,
    string Path
) : IComponent;
