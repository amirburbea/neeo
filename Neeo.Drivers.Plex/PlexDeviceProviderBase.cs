using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neeo.Sdk.Devices;

namespace Neeo.Drivers.Plex;

public abstract class PlexDeviceProviderBase : IDeviceProvider
{
    private readonly string _deviceName;
    private readonly DeviceType _deviceType;
    private string _uriPrefix = string.Empty;

    protected PlexDeviceProviderBase(string deviceName, DeviceType deviceType)
    {
        this.DeviceBuilder = this.CreateDevice();
        this._deviceName = deviceName;
        this._deviceType = deviceType;
    }

    public IDeviceBuilder DeviceBuilder { get; }

    protected virtual IDeviceBuilder CreateDevice() => Device.Create(this._deviceName, this._deviceType)
        .SetManufacturer(nameof(Plex))
        .SetSpecificName(this._deviceName)
        .AddAdditionalSearchTokens(nameof(Plex))
        .AddButtonHandler(this.HandleButtonAsync)
        .RegisterInitializer(this.Initialize)
        .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAdded, this.OnDeviceRemoved, this.InitializeDeviceList)
        .EnableDeviceRoute(this.SetUriPrefix, this.HandleRouteAsync);

    private Task HandleButtonAsync(string deviceId, string buttonName)
    {
        throw new NotImplementedException();
    }

    private Task<ActionResult> HandleRouteAsync(HttpRequest request, string path, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private Task Initialize(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private Task InitializeDeviceList(string[] deviceIds)
    {
        throw new NotImplementedException();
    }

    private Task OnDeviceAdded(string deviceId)
    {
        throw new NotImplementedException();
    }

    private Task OnDeviceRemoved(string deviceId)
    {
        throw new NotImplementedException();
    }

    private void SetUriPrefix(string prefix) => this._uriPrefix = prefix;
}
