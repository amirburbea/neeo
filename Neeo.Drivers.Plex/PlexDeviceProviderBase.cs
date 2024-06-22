using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Devices.Setup;

namespace Neeo.Drivers.Plex;

public class PlexRemoteDeviceProvider(
    IPlexServerDiscovery serverDiscovery,
    ILogger<PlexRemoteDeviceProvider> logger
) : PlexDeviceProviderBase("Remote", DeviceType.TV, serverDiscovery, logger)
{
    protected override IDeviceBuilder CreateDevice()
    {
        return base.CreateDevice()
            .AddButtonGroup(ButtonGroups.Power | ButtonGroups.Transport | ButtonGroups.Volume | ButtonGroups.ControlPad | ButtonGroups.MenuAndBack)
            .AddDirectory("clients", null, null, BrowseClients, SelectClient)
            // SDK requires an input for a TV so just pretend there's a generic input.
            .AddButton("INPUT PLEX", "Plex");
    }

    private async Task BrowseClients(string deviceId, DirectoryBuilder list, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task SelectClient(string deviceId, string actionIdentifier, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

public abstract class PlexDeviceProviderBase(
    string deviceName,
    DeviceType deviceType,
    IPlexServerDiscovery serverDiscovery,
    ILogger logger
) : IDeviceProvider
{
    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    private IDeviceBuilder? _deviceBuilder;
    private IPlexServer? _server;
    private string _uriPrefix = string.Empty;

    public IDeviceBuilder DeviceBuilder => this._deviceBuilder ??= this.CreateDevice();

    protected ILogger Logger => logger;

    protected virtual IDeviceBuilder CreateDevice() => Device.Create(deviceName, deviceType)
        .SetManufacturer(nameof(Plex))
        .SetSpecificName(deviceName)
        .AddAdditionalSearchTokens(nameof(Plex), "pms")
        .AddButtonHandler(this.HandleButtonAsync)
        .AddPowerStateSensor(this.IsPoweredOnAsync)
        .EnableDeviceRoute(this.SetUriPrefix, this.HandleRouteAsync)
        .EnableDiscovery("Plex Discovery", "Select Plex Server", this.DiscoverServersAsync)
        .EnableRegistration("Plex Registration", "Enter Plex Credentials", this.IsRegisteredAsync, this.RegisterAsync)
        .RegisterInitializer(this.GetServerAsync)
        .RegisterDeviceSubscriptionCallbacks(this.HandleDeviceAddedAsync, this.HandleDeviceRemovedAsync, this.InitializeDeviceListAsync);

    private static string GetContentType(string fileName)
    {
        return PlexDeviceProviderBase._contentTypeProvider.TryGetContentType(fileName, out string? contentType)
            ? contentType
            : "application/octet-stream";
    }

    private async Task<DiscoveredDevice[]> DiscoverServersAsync(string? optionalDeviceId, CancellationToken cancellationToken)
    {
        return await this.GetServerAsync(cancellationToken).ConfigureAwait(false) is { DeviceDescriptor: { } descriptor }
            ? [descriptor]
            : [];
    }

    private async Task<IPlexServer?> GetServerAsync(CancellationToken cancellationToken)
    {
        return this._server ??= await serverDiscovery.DiscoverAsync(cancellationToken).ConfigureAwait(false);
    }

    private Task HandleButtonAsync(string deviceId, string buttonName, CancellationToken cancellationToken)
    {
        this.Logger.LogInformation("DeviceId:{deviceId} - {buttonName}", deviceId, buttonName);
        return Task.CompletedTask;
    }

    private async Task HandleDeviceAddedAsync(string deviceId, CancellationToken cancellationToken)
    {
        this.Logger.LogInformation("Device added: {deviceId}", deviceId);
        if (await this.GetServerAsync(cancellationToken).ConfigureAwait(false) is { } server && server.DeviceId == deviceId)
        {
            await server.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleDeviceRemovedAsync(string deviceId, CancellationToken cancellationToken)
    {
        this.Logger.LogInformation("Device removed: {deviceId}", deviceId);
        if (await this.GetServerAsync(cancellationToken).ConfigureAwait(false) is { } server && server.DeviceId == deviceId)
        {
            server.Dispose();
        }
    }

    private Task<ActionResult> HandleRouteAsync(HttpRequest request, string path, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task InitializeDeviceListAsync(string[] deviceIds, CancellationToken cancellationToken)
    {
        this.Logger.LogInformation("Initialized with [{deviceIds}]", string.Join(',', deviceIds));
        if (deviceIds is [string deviceId] && await this.GetServerAsync(cancellationToken).ConfigureAwait(false) is { } server && server.DeviceId == deviceId)
        {
            await server.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private Task<bool> IsPoweredOnAsync(string deviceId, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    private async Task<bool> IsRegisteredAsync(CancellationToken cancellationToken)
    {
        // If there is a server and it has a non-null token, try connecting with it.
        if (await this.GetServerAsync(cancellationToken).ConfigureAwait(false) is { AuthToken: { } } server)
        {
            return await server.GetStatusCodeAsync(cancellationToken).ConfigureAwait(false) is HttpStatusCode.OK;
        }
        return false;
    }

    private async Task<RegistrationResult> RegisterAsync(string userName, string password, CancellationToken cancellationToken)
    {
        try
        {
            if (await this.GetServerAsync(cancellationToken).ConfigureAwait(false) is not { } server)
            {
                return RegistrationResult.Failed("Server not found, pinging the server could refresh the ARP table.");
            }
            await server.TryLoginAsync(userName, password, cancellationToken).ConfigureAwait(false);
            if (await server.GetStatusCodeAsync(cancellationToken).ConfigureAwait(false) is HttpStatusCode.OK)
            {
                return RegistrationResult.Success;
            }
            return RegistrationResult.Failed("Something went wrong, Plex token did not work on server.");
        }
        catch (Exception e)
        {
            return RegistrationResult.Failed(e.Message);
        }
    }

    private void SetUriPrefix(string prefix) => this._uriPrefix = prefix;
}
