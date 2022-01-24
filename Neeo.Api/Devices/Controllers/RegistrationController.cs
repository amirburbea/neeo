using System;
using System.Text.Json;
using System.Threading.Tasks;
using Neeo.Api.Devices.Discovery;

namespace Neeo.Api.Devices.Controllers;

public interface IRegistrationController : IController
{
    ControllerType IController.Type => ControllerType.Registration;

    Task<bool> QueryIsRegisteredAsync();

    Task RegisterAsync(JsonElement credentials);
}

internal sealed class RegistrationController : IRegistrationController
{
    private readonly Func<JsonElement, Task> _processor;
    private readonly QueryIsRegistered _queryIsRegistered;

    public RegistrationController(
        QueryIsRegistered queryIsRegistered,
        Func<JsonElement, Task> processor
    ) => (this._queryIsRegistered, this._processor) = (queryIsRegistered, processor);

    public Task<bool> QueryIsRegisteredAsync() => this._queryIsRegistered();

    public Task RegisterAsync(JsonElement credentials) => this._processor(credentials);
}