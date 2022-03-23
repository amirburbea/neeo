using System;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Threading;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Interface for an object responsible for providing an <see cref="IDeviceBuilder"/> for use in starting the REST server.
/// <para/>
/// This class is useful for dependency injection purposes and used with 
/// <see cref="Brain.StartServerAsync(IDeviceProvider[], string?, IPAddress?, ushort, Action{HostBuilderContext, ILoggingBuilder}?, CancellationToken)"/>.
/// </summary>
public interface IDeviceProvider
{
    /// <summary>
    /// Gets the device builder.
    /// </summary>
    IDeviceBuilder DeviceBuilder { get; }
}