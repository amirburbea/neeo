using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    /// <summary>
    /// A callback which is invoked in response to favorites being requested on the NEEO remote
    /// in order to allow the driver to respond accordingly.
    /// <para/>
    /// Example: The favorite &quot;42&quot; - rather than the NEEO Brain invoking the button handler twice
    /// (with &quot;4&quot; and &quot;2&quot;), the handler is invoked with a single string of &quot;42&quot;.
    /// <para />
    /// </summary>
    /// <param name="deviceId">The id associated with the device.</param>
    /// <param name="favorite">The favorite requested.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public delegate Task FavoritesHandler(string deviceId, string favorite);
}