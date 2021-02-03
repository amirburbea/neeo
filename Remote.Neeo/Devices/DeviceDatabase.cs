using System.Collections.Generic;
using Remote.Neeo.Devices;

namespace Remote.Neeo.Web
{
    public sealed class DeviceDatabase
    {
        private readonly IReadOnlyCollection<IDeviceAdapter> _devices;

        public DeviceDatabase(IReadOnlyCollection<IDeviceAdapter> devices) => this._devices = devices;

        private static class Constants
        {
            public const double MatchFactor = 0.5;
            public const int MaxSearchResults = 10;
        }
    }
}
