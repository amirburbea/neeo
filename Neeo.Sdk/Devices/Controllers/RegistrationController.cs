using System;
using System.Text.Json;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Discovery;

namespace Neeo.Sdk.Devices.Controllers;

public interface IRegistrationController : IController
{
    ControllerType IController.Type => ControllerType.Registration;

    Task<bool> QueryIsRegisteredAsync();

    Task<RegistrationResult> RegisterAsync(JsonElement credentials);
}

internal sealed class RegistrationController : IRegistrationController
{
    private readonly Func<JsonElement, Task<RegistrationResult>> _processor;
    private readonly QueryIsRegistered _queryIsRegistered;

    public RegistrationController(
        QueryIsRegistered queryIsRegistered,
        Func<JsonElement, Task<RegistrationResult>> processor
    ) => (this._queryIsRegistered, this._processor) = (queryIsRegistered, processor);

    public Task<bool> QueryIsRegisteredAsync() => this._queryIsRegistered();

    public Task<RegistrationResult> RegisterAsync(JsonElement credentials) => this._processor(credentials);
}