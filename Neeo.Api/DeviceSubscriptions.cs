namespace Neeo.Api;

public interface IDeviceSubscriptions
{
}

internal sealed class DeviceSubscriptions : IDeviceSubscriptions
{
    private readonly IApiClient _client;

    public DeviceSubscriptions(IApiClient client)
    {
        this._client = client;
    }
}