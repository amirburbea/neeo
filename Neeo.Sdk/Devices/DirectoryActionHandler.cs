using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

/// <summary>
/// 
/// </summary>
/// <param name="deviceId"></param>
/// <param name="actionIdentifier"></param>
/// <returns></returns>
public delegate Task DirectoryActionHandler(string deviceId, string actionIdentifier);