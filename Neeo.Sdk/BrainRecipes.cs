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
        return client.GetAsync(UrlPaths.ActiveRecipes, static (string[] keys) => keys, cancellationToken);
    }

    public async Task<IRecipe[]> GetAllRecipesAsync(CancellationToken cancellationToken)
    {
        return await client.GetAsync(
            UrlPaths.RecipeDefinitions,
            (RecipeDefinition[] definitions) => Array.ConvertAll(
                definitions,
                definition => new Recipe(definition, client)
            ),
            cancellationToken
        );
    }

    private sealed class Recipe(
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

        public Task<bool> GetPowerStateAsync(CancellationToken cancellationToken) => client.GetAsync(
            recipe.Urls.GetPowerState,
            (JsonElement element) => element.GetProperty("active").GetBoolean(),
            cancellationToken
        );

        public Task PowerOffAsync(CancellationToken cancellationToken) => recipe.Urls.SetPowerOff switch
        {
            { } url => client.GetAsync(url, EmptyObject.Transform, cancellationToken),
            _ => throw new NotSupportedException("Recipe can not be powered off."),
        };

        public Task PowerOnAsync(CancellationToken cancellationToken) => client.GetAsync(
            recipe.Urls.SetPowerOn,
            EmptyObject.Transform,
            cancellationToken
        );

        private readonly struct EmptyObject
        {
            public static readonly Func<EmptyObject, object?> Transform = _ => null;
        }
    }

    private readonly record struct RecipeDefinition(
        bool IsCustom,
        bool IsPoweredOn,
        string Uid,
        string PowerKey,
        string Type,
        RecipeDetail Detail,
        [property: JsonPropertyName("url")] RecipeUrls Urls
     );

    private readonly record struct RecipeDetail(
        [property: JsonPropertyName("devicename")] string DeviceName,
        [property: JsonPropertyName("roomname")] string RoomName,
        [property: JsonPropertyName("devicetype")] DeviceType DeviceType,
        string? Model = null,
        string? Manufacturer = null
    );

    private readonly record struct RecipeUrls(
        string GetPowerState,
        string SetPowerOn,
        string? SetPowerOff = null
    );
}
