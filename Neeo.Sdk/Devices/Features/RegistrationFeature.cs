using System;
using System.Text.Json;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Discovery;

namespace Neeo.Sdk.Devices.Features;

public interface IRegistrationFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Registration;

    Task<IsRegisteredResponse> QueryIsRegisteredAsync();

    Task<RegistrationResult> RegisterAsync(JsonElement credentials);
}

internal sealed class RegistrationFeature : IRegistrationFeature
{
    private readonly Func<JsonElement, Task<RegistrationResult>> _processor;
    private readonly QueryIsRegistered _queryIsRegistered;

    public RegistrationFeature(
        QueryIsRegistered queryIsRegistered,
        RegistrationType registrationType,
        Func<JsonElement, Task<RegistrationResult>> processor
    ) => (this._queryIsRegistered, this.RegistrationType, this._processor) = (queryIsRegistered, registrationType, processor);

    public RegistrationType RegistrationType { get; }

    public async Task<IsRegisteredResponse> QueryIsRegisteredAsync() => new(await this._queryIsRegistered().ConfigureAwait(false));

    public Task<RegistrationResult> RegisterAsync(JsonElement credentials) => this._processor(credentials);
}