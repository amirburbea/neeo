using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace HiSense.SmartTV;

public static class IPHelper
{
    private static readonly PhysicalAddress _broadcastAddress = new(new byte[6] { 255, 255, 255, 255, 255, 255 });
    private static readonly PhysicalAddress _virtualAddress = new(new byte[6] { 0, 0, 0, 0, 0, 0 });

    /// <summary>
    /// Get the IP and MAC addresses of all known devices on the LAN
    /// </summary>
    /// <remarks>
    /// 1) This table is not updated often - it can take some human-scale time
    ///    to notice that a device has dropped off the network, or a new device
    ///    has connected.
    /// 2) This discards non-local devices if they are found - these are multicast
    ///    and can be discarded by IP address range.
    /// </remarks>
    /// <returns></returns>
    public static Dictionary<IPAddress, PhysicalAddress> GetAllDevicesOnLAN()
    {
        int spaceForNetTable = 0;
        // Get the space needed
        // We do that by requesting the table, but not giving any space at all.
        // The return value will tell us how much we actually need.
        _ = NativeMethods.GetIpNetTable(IntPtr.Zero, ref spaceForNetTable, false);
        IntPtr rawTable = IntPtr.Zero;
        try
        {
            // Allocate the space.
            rawTable = Marshal.AllocCoTaskMem(spaceForNetTable);
            if (NativeMethods.GetIpNetTable(rawTable, ref spaceForNetTable, false) is int errorCode and not 0)
            {
                throw new Exception($"Unable to retrieve network table. Error code {errorCode}.");
            }
            int rowCount = Marshal.ReadInt32(rawTable);
            Dictionary<IPAddress, PhysicalAddress> output = new(rowCount + 1);
            // Add this PC to the list.
            output.Add(GetHostIPAddress(), GetHostMacAddress());
            IntPtr startPtr = new(rawTable.ToInt64() + Marshal.SizeOf<int>());
            // Convert the raw table to individual entries.
            MIB_IPNETROW[] rows = new MIB_IPNETROW[rowCount];
            for (int index = 0; index < rowCount; index++)
            {
                rows[index] = Marshal.PtrToStructure<MIB_IPNETROW>(new(startPtr.ToInt64() + (index * Marshal.SizeOf<MIB_IPNETROW>())))!;
            }
            foreach (MIB_IPNETROW row in rows)
            {
                IPAddress ipAddress = new(BitConverter.GetBytes(row.dwAddr));
                if (ipAddress.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ipAddress) || IsMulticast(ipAddress))
                {
                    continue;
                }
                PhysicalAddress macAddress = new(new byte[] { row.mac0, row.mac1, row.mac2, row.mac3, row.mac4, row.mac5 });
                if (!_virtualAddress.Equals(macAddress) && !_broadcastAddress.Equals(macAddress))
                {
                    output.Add(ipAddress, macAddress);
                }
            }
            return output;
        }
        finally
        {
            // Release the memory.
            Marshal.FreeCoTaskMem(rawTable);
        }

        static IPAddress GetHostIPAddress()
        {
            string strHostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(strHostName);
            return addresses.Length == 0
                ? IPAddress.Loopback
                : Array.Find(addresses, static address => !IPAddress.IsLoopback(address) && !address.IsIPv6LinkLocal) is { } address
                    ? address
                    : addresses[0];
        }

        static PhysicalAddress GetHostMacAddress()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }
                if (nic.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211 or NetworkInterfaceType.GigabitEthernet)
                {
                    return nic.GetPhysicalAddress();
                }
            }
            return PhysicalAddress.None;
        }

    }


    public static bool IsMulticast(IPAddress ipAddress) => ipAddress.IsIPv6Multicast || ipAddress.GetAddressBytes()[0] is not < 224 and not > 239;

    public static IPAddress? GetIPAddress(PhysicalAddress physicalAddress)
    {
        var localIPs = GetAllDevicesOnLAN();
        foreach (var pair in localIPs)
        {
            if (pair.Value.Equals(physicalAddress))
                return pair.Key;
        }

        return null;
    }

    /// <summary>
    /// MIB_IPNETROW structure returned by GetIpNetTable
    /// DO NOT MODIFY THIS STRUCTURE.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_IPNETROW
    {
        [MarshalAs(UnmanagedType.U4)]
        public int dwIndex;

        [MarshalAs(UnmanagedType.U4)]
        public int dwPhysAddrLen;

        [MarshalAs(UnmanagedType.U1)]
        public byte mac0;

        [MarshalAs(UnmanagedType.U1)]
        public byte mac1;

        [MarshalAs(UnmanagedType.U1)]
        public byte mac2;

        [MarshalAs(UnmanagedType.U1)]
        public byte mac3;

        [MarshalAs(UnmanagedType.U1)]
        public byte mac4;

        [MarshalAs(UnmanagedType.U1)]
        public byte mac5;

        [MarshalAs(UnmanagedType.U1)]
        public byte mac6;

        [MarshalAs(UnmanagedType.U1)]
        public byte mac7;

        [MarshalAs(UnmanagedType.U4)]
        public int dwAddr;

        [MarshalAs(UnmanagedType.U4)]
        public int dwType;
    }

    private static class NativeMethods
    {
        /// <summary>
        /// GetIpNetTable external method
        /// </summary>
        /// <param name="pIpNetTable"></param>
        /// <param name="pdwSize"></param>
        /// <param name="bOrder"></param>
        /// <returns></returns>
        [DllImport("IpHlpApi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern int GetIpNetTable(IntPtr pIpNetTable, [MarshalAs(UnmanagedType.U4)] ref int pdwSize, bool bOrder);
    }
}