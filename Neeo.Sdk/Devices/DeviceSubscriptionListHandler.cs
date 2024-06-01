using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Callback function used on startup once the SDK can reach the Brain, this is called on startup with the current
/// subscriptions removing the need to save them in the driver.
/// </summary>
/// <param name="deviceIds">Array of identifiers for the devices of this type currently on the Brain.</param>
/// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
public delegate Task DeviceSubscriptionListHandler(string[] deviceIds);
