using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Discovery;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Features;

public interface IRegistrationFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Registration;

    Task<bool> QueryIsRegisteredAsync();

    Task<RegistrationResult> RegisterAsync(Stream stream);
}

internal sealed class RegistrationFeature : IRegistrationFeature
{
    private readonly Func<Stream, Task<RegistrationResult>> _processor;
    private readonly QueryIsRegistered _queryIsRegistered;

    private RegistrationFeature(QueryIsRegistered queryIsRegistered, Func<Stream, Task<RegistrationResult>> processor)
    {
        (this._queryIsRegistered, this._processor) = (queryIsRegistered ?? throw new ArgumentNullException(nameof(queryIsRegistered)), processor);
    }

    public static RegistrationFeature Create<TPayload>(QueryIsRegistered queryIsRegistered, Func<TPayload, Task<RegistrationResult>> processor)
    {
        return new(queryIsRegistered ?? throw new ArgumentNullException(nameof(queryIsRegistered)), ProcessAsync);

        async Task<RegistrationResult> ProcessAsync(Stream stream)
        {
            TPayload payload = (await JsonSerializer.DeserializeAsync<TPayload>(stream, JsonSerialization.Options).ConfigureAwait(false))!;
            return await processor(payload).ConfigureAwait(false);
        }
    }

    public Task<bool> QueryIsRegisteredAsync() => this._queryIsRegistered();

    public Task<RegistrationResult> RegisterAsync(Stream stream) => this._processor(stream);
}