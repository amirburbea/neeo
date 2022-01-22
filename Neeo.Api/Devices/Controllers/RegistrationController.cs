using System;
using System.Text.Json;
using System.Threading.Tasks;
using Neeo.Api.Devices.Discovery;

namespace Neeo.Api.Devices.Controllers;

public interface IRegistrationController : IController
{
    ControllerType IController.Type => ControllerType.Registration;

    Task<IsRegisteredResponse> QueryIsRegisteredAsync();

    Task<SuccessResult> RegisterAsync(JsonElement credentials);
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

    public async Task<IsRegisteredResponse> QueryIsRegisteredAsync() => await this._queryIsRegistered().ConfigureAwait(false);

    public async Task<SuccessResult> RegisterAsync(JsonElement credentials)
    {
        await this._registrationProcess(credentials).ConfigureAwait(false);
        return true;
    }
}
