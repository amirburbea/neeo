using System.Threading.Tasks;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Sdk.Devices;

public delegate Task DeviceDirectoryPopulator(string deviceId, IListBuilder builder);