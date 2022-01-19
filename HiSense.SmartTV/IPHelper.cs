﻿using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace HiSense.SmartTV;

public class IPHelper
{
    /// <summary>
    /// MIB_IPNETROW structure returned by GetIpNetTable
    /// DO NOT MODIFY THIS STRUCTURE.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct MIB_IPNETROW
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

    /// <summary>
    /// GetIpNetTable external method
    /// </summary>
    /// <param name="pIpNetTable"></param>
    /// <param name="pdwSize"></param>
    /// <param name="bOrder"></param>
    /// <returns></returns>
    [DllImport("IpHlpApi.dll")]
    [return: MarshalAs(UnmanagedType.U4)]
    static extern int GetIpNetTable(IntPtr pIpNetTable,
          [MarshalAs(UnmanagedType.U4)] ref int pdwSize, bool bOrder);

    /// <summary>
    /// Error codes GetIpNetTable returns that we recognise
    /// </summary>
    const int ERROR_INSUFFICIENT_BUFFER = 122;
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
        Dictionary<IPAddress, PhysicalAddress> all = new();
        // Add this PC to the list...
        all.Add(GetIPAddress()!, GetMacAddress());
        int spaceForNetTable = 0;
        // Get the space needed
        // We do that by requesting the table, but not giving any space at all.
        // The return value will tell us how much we actually need.
        _ =  GetIpNetTable(IntPtr.Zero, ref spaceForNetTable, false);
        // Allocate the space
        // We use a try-finally block to ensure release.
        IntPtr rawTable = IntPtr.Zero;
        try
        {
            rawTable = Marshal.AllocCoTaskMem(spaceForNetTable);
            // Get the actual data
            int errorCode = GetIpNetTable(rawTable, ref spaceForNetTable, false);
            if (errorCode != 0)
            {
                // Failed for some reason - can do no more here.
                throw new Exception(string.Format(
                  "Unable to retrieve network table. Error code {0}", errorCode));
            }
            // Get the rows count
            int rowsCount = Marshal.ReadInt32(rawTable);
            IntPtr currentBuffer = new(rawTable.ToInt64() + Marshal.SizeOf(typeof(int)));
            // Convert the raw table to individual entries
            MIB_IPNETROW[] rows = new MIB_IPNETROW[rowsCount];
            for (int index = 0; index < rowsCount; index++)
            {
                rows[index] = Marshal.PtrToStructure<MIB_IPNETROW>(new IntPtr(currentBuffer.ToInt64() + (index * Marshal.SizeOf(typeof(MIB_IPNETROW)))))!;
            }
            // Define the dummy entries list (we can discard these)
            PhysicalAddress virtualMAC = new(new byte[] { 0, 0, 0, 0, 0, 0 });
            PhysicalAddress broadcastMAC = new(new byte[] { 255, 255, 255, 255, 255, 255 });
            foreach (MIB_IPNETROW row in rows)
            {
                IPAddress ip = new(BitConverter.GetBytes(row.dwAddr));
                if (ip.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ip))
                {
                    continue;
                }
                byte[] rawMAC = new byte[] { row.mac0, row.mac1, row.mac2, row.mac3, row.mac4, row.mac5 };
                PhysicalAddress pa = new(rawMAC);
                if (!pa.Equals(virtualMAC) && !pa.Equals(broadcastMAC) && !IsMulticast(ip))
                {
                    //Console.WriteLine("IP: {0}\t\tMAC: {1}", ip.ToString(), pa.ToString());
                    if (!all.ContainsKey(ip))
                    {
                        all.Add(ip, pa);
                    }
                }
            }
        }
        finally
        {
            // Release the memory.
            Marshal.FreeCoTaskMem(rawTable);
        }
        return all;
    }

    /// <summary>
    /// Gets the IP address of the current PC
    /// </summary>
    /// <returns></returns>
    public static IPAddress? GetIPAddress()
    {
        String strHostName = Dns.GetHostName();
        IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
        IPAddress[] addr = ipEntry.AddressList;
        foreach (IPAddress ip in addr)
        {
            if (!ip.IsIPv6LinkLocal)
            {
                return (ip);
            }
        }
        return addr.Length > 0 ? addr[0] : null;
    }

    /// <summary>
    /// Gets the MAC address of the current PC.
    /// </summary>
    /// <returns></returns>
    public static PhysicalAddress GetMacAddress()
    {
        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Only consider Ethernet network interfaces
            if(nic.OperationalStatus == OperationalStatus.Up)
            {
                return nic.GetPhysicalAddress();
            }
        }
        return null;
    }

    /// <summary>
    /// Returns true if the specified IP address is a multicast address
    /// </summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    public static bool IsMulticast(IPAddress ip)
    {
        bool result = true;
        if (!ip.IsIPv6Multicast)
        {
            byte highIP = ip.GetAddressBytes()[0];
            if (highIP is < 224 or > 239)
            {
                result = false;
            }
        }
        return result;
    }

    public static IPAddress GetIPAddress(PhysicalAddress physicalAddress)
    {
        var localIPs = GetAllDevicesOnLAN();
        foreach (var pair in localIPs)
        {
            if (pair.Value.Equals(physicalAddress))
                return pair.Key;
        }

        return null;
    }
}