using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

/// <summary>
/// A callback which is invoked in response to a favorite being requested on the NEEO remote
/// in order to allow the driver to respond accordingly.
/// </summary>
/// <param name="deviceId">The id associated with the device.</param>
/// <param name="favorite">The favorite requested.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
/// <remarks>
/// Example: Given a favorite of &quot;42&quot;, rather than invoking a button handler twice (&quot;DIGIT 4&quot;
/// followed by &quot;DIGIT 2&quot;), the handler is invoked with a single value of &quot;42&quot;.
/// </remarks>
public delegate Task FavoriteHandler(string deviceId, string favorite);
