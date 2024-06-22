using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Sdk;

/// <summary>
/// Represents a recipe in the Neeo EUI.
/// </summary>
public interface IRecipe
{
    /// <summary>
    /// Gets a value indicating if the recipe can be powered off.
    /// </summary>
    bool CanBePoweredOff { get; }

    /// <summary>
    /// Gets a value indicating if this is a custom recipe.
    /// </summary>
    bool IsCustom { get; }

    /// <summary>
    /// Gets a value indicating if the recipe is powered on.
    /// </summary>
    bool IsPoweredOn { get; }

    /// <summary>
    /// Gets the manufacturer of the device (when this is a device recipe).
    /// </summary>
    string? Manufacturer { get; }

    /// <summary>
    /// Gets the model of the device (when this is a device recipe).
    /// </summary>
    string? Model { get; }

    /// <summary>
    /// Gets the name of the recipe.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the power key of the recipe.
    /// </summary>
    string PowerKey { get; }

    /// <summary>
    /// Gets the name of the room associated with the recipe.
    /// </summary>
    string RoomName { get; }

    /// <summary>
    /// Gets the type of the recipe.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Asynchronously gets whether the recipe is powered on.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchonous operation.</returns>
    Task<bool> GetPowerStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously instructs the NEEO Brain to turn off the recipe (assuming this recipe <see cref="CanBePoweredOff"/>).
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchonous operation.</returns>
    Task PowerOffAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously instructs the NEEO Brain to turn on the recipe.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchonous operation.</returns>
    Task PowerOnAsync(CancellationToken cancellationToken = default);
}
