using System;
using System.Text.Json;
using System.Threading.Tasks;
using Neeo.Api.Devices.Discovery;
using Neeo.Api.Json;

namespace Neeo.Api.Devices.Controllers;

public interface IRegistrationController : IController
{
    ControllerType IController.Type => ControllerType.Registration;

    Task<bool> QueryIsRegisteredAsync();

    Task RegisterAsync(JsonElement credentials);
}

internal sealed class RegistrationController : IRegistrationController
{
    private readonly QueryIsRegistered _queryIsRegistered;
    private readonly Func<JsonElement, Task> _registrationProcess;

    public RegistrationController(QueryIsRegistered queryIsRegistered, Func<JsonElement, Task> registrationProcess)
    {
        this._queryIsRegistered = queryIsRegistered;
        this._registrationProcess = registrationProcess;
    }

    public Task<bool> QueryIsRegisteredAsync() => this._queryIsRegistered();

    public Task RegisterAsync(JsonElement credentials) => this._registrationProcess(credentials);
}
