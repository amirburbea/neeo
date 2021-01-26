using System;

namespace Remote.Broadlink
{
    public sealed class DiscoveryException : Exception
    {
        internal DiscoveryException()
            : base("Discovery failed.")
        {
        }
    }
}
