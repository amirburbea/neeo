using System.Collections.Generic;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk;

internal sealed record class SdkConfiguration(Brain Brain, IReadOnlyCollection<IDeviceBuilder> Devices, string Name);