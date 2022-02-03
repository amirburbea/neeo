﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Neeo.Sdk.Utilities;

public static partial class NetworkDevices
{
    private static ReadOnlySpan<byte> BroadcastAddress => new byte[] { 255, 255, 255, 255, 255, 255 };

    private static ReadOnlySpan<byte> VirtualAddress => new byte[] { 0, 0, 0, 0, 0, 0 };

    private static void PopulateNetworkDevicesViaInterop(Dictionary<IPAddress, PhysicalAddress> output)
    {
        int spaceForTable = 0;
        // Get the space needed by requesting the table, but not giving any space at all.
        // The return value will tell us how much space we actually need.
        _ = NativeMethods.GetIpNetTable(IntPtr.Zero, ref spaceForTable, false);
        IntPtr rawTable = IntPtr.Zero;
        try
        {
            // Allocate the space.
            rawTable = Marshal.AllocCoTaskMem(spaceForTable);
            if (NativeMethods.GetIpNetTable(rawTable, ref spaceForTable, false) is int errorCode and not 0)
            {
                throw new Exception($"Unable to retrieve network table. Error code {errorCode}.");
            }
            int rowCount = Marshal.ReadInt32(rawTable);
            IntPtr startPointer = new(rawTable.ToInt64() + Marshal.SizeOf<int>());
            for (int index = 0; index < rowCount; index++)
            {
                MIB_IPNETROW row = Marshal.PtrToStructure<MIB_IPNETROW>(new(startPointer.ToInt64() + index * Marshal.SizeOf<MIB_IPNETROW>()));
                IPAddress ipAddress = new(BitConverter.GetBytes(row.dwAddr));
                if (ipAddress.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ipAddress) || IsMulticast(ipAddress))
                {
                    continue;
                }
                byte[] bytes = new[] { row.mac0, row.mac1, row.mac2, row.mac3, row.mac4, row.mac5 };
                if (bytes.AsSpan().IndexOf(NetworkDevices.VirtualAddress) < 0 && bytes.AsSpan().IndexOf(NetworkDevices.BroadcastAddress) < 0)
                {
                    output.Add(ipAddress, new(bytes));
                }
            }
        }
        finally
        {
            // Release the memory.
            Marshal.FreeCoTaskMem(rawTable);
        }

        static bool IsMulticast(IPAddress address) => address.IsIPv6Multicast || address.GetAddressBytes()[0] is not < 224 and not > 239;
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
        [DllImport("IpHlpApi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern int GetIpNetTable(IntPtr pIpNetTable, [MarshalAs(UnmanagedType.U4)] ref int pdwSize, bool bOrder);
    }
}