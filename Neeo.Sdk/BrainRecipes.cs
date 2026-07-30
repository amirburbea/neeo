using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk;

/// <summary>
/// Gets information relating to the recipes on the NEEO Brain.
/// </summary>
public interface IBrainRecipes
{
    /// <summary>
    /// Asynchonously gets the power keys of the currently active recipes.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<string[]> GetActiveRecipeKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchonously gets the currently registered recipes.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<IRecipe[]> GetAllRecipesAsync(CancellationToken cancellationToken = default);
}

internal sealed class BrainRecipes(
    IApiClient client
) : IBrainRecipes
{
    public Task<string[]> GetActiveRecipeKeysAsync(CancellationToken cancellationToken)
    {
        return client.GetAsync<string[]>(BrainUrlPaths.ActiveRecipes, cancellationToken);
    }

    public async Task<IRecipe[]> GetAllRecipesAsync(CancellationToken cancellationToken)
    {
        RecipeDefinition[] definitions = await client
            .GetAsync<RecipeDefinition[]>(BrainUrlPaths.RecipeDefinitions, cancellationToken)
            .ConfigureAwait(false);
        return Array.ConvertAll(definitions, definition => new Recipe(definition, client));
    }

    internal sealed class Recipe(
        RecipeDefinition recipe,
        IApiClient client
    ) : IRecipe
    {
        public bool CanBePoweredOff => recipe.Urls.SetPowerOff is { };

        public bool IsCustom => recipe.IsCustom;

        public bool IsPoweredOn => recipe.IsPoweredOn;

        public string? Manufacturer => recipe.Detail.Manufacturer;

        public string? Model => recipe.Detail.Model;

        public string Name => recipe.Detail.DeviceName;

        public string PowerKey => recipe.PowerKey;

        public string RoomName => recipe.Detail.RoomName;

        public string Type => recipe.Type;

        public async Task<bool> GetPowerStateAsync(CancellationToken cancellationToken)
        {
            JsonElement element = await client
                .GetAsync<JsonElement>(recipe.Urls.GetPowerState, cancellationToken)
                .ConfigureAwait(false);
            return element.GetProperty("active").GetBoolean();
        }

        public Task PowerOffAsync(CancellationToken cancellationToken) => recipe.Urls.SetPowerOff switch
        {
            { } url => client.GetAsync<EmptyObject>(url, cancellationToken),
            _ => throw new NotSupportedException("Recipe can not be powered off."),
        };

        public Task PowerOnAsync(CancellationToken cancellationToken) => client.GetAsync<JsonElement>(recipe.Urls.SetPowerOn, cancellationToken);

        internal readonly struct EmptyObject { }
    }

    internal readonly record struct RecipeDefinition(
        bool IsCustom,
        bool IsPoweredOn,
        string Uid,
        string PowerKey,
        string Type,
        RecipeDetail Detail,
        [property: JsonPropertyName("url")] RecipeUrls Urls
     );

    internal readonly record struct RecipeDetail(
        [property: JsonPropertyName("devicename")] string DeviceName,
        [property: JsonPropertyName("roomname")] string RoomName,
        [property: JsonPropertyName("devicetype")] DeviceType DeviceType,
        string? Model = null,
        string? Manufacturer = null
    );

    internal readonly record struct RecipeUrls(
        string GetPowerState,
        string SetPowerOn,
        string? SetPowerOff = null
    );
}
