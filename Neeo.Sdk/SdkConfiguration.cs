using Neeo.Sdk.Devices;

namespace Neeo.Sdk;

internal sealed record class SdkConfiguration(Brain Brain, IDeviceBuilder[] Devices, string Name);