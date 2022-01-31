using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

public delegate Task DirectoryActionHandler(string deviceId, string actionIdentifier);