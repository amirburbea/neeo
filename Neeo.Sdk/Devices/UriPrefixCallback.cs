using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

/// <summary>
/// When device routes are enabled (via a call to <see cref="IDeviceBuilder.EnableDeviceRoute"/>), 
/// the device will be able to handle all requests to URLs that begin with the specified <paramref name="prefix" />.
/// <para/>
/// The prefix is notified to the device adapter upon the start of the REST server.
/// </summary>
/// <param name="prefix">The URI prefix for requests that would be handled by the device.</param>
public delegate ValueTask UriPrefixCallback(string prefix);