using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Broadlink.RM;

public class RMDevice : IDisposable
{
    private static readonly byte[] _dataHeader = new byte[] { 0xd0, 0x00, 0x02, 0x00, 0x00, 0x00, };
    private static readonly byte[] _iv = new byte[] { 0x56, 0x2e, 0x17, 0x99, 0x6d, 0x09, 0x3d, 0x28, 0xdd, 0xb3, 0xba, 0x69, 0x5a, 0x2e, 0x6f, 0x58, };

    private readonly UdpClient _client;
    private readonly Task _listeningTask;
    private readonly byte[] _macAddress;
    private int _count;
    private bool _disposed;
    private byte[] _id = new byte[] { 0x00, 0x00, 0x00, 0x00, };
    private byte[] _key = new byte[] { 0x09, 0x76, 0x28, 0x34, 0x3f, 0xe9, 0x9e, 0x23, 0x76, 0x5c, 0x15, 0x13, 0xac, 0xcf, 0x8b, 0x02, };

    public RMDevice(IPAddress localAddress, IPEndPoint remoteEndPoint, byte[] macAddress, int deviceType)
    {
        this.DeviceType = deviceType;
        this._client = new(new IPEndPoint(localAddress, 0));
        this._client.Connect(remoteEndPoint);
        this._macAddress = macAddress;
        this._listeningTask = new Task(this.Listen, TaskCreationOptions.LongRunning);
    }

    public event EventHandler? AckReceived;

    public event EventHandler<DataEventArgs<byte[]>>? DataReceived;

    public event EventHandler? Ready;

    public event EventHandler<DataEventArgs<double>>? TemperatureReceived;

    private enum RequestType : byte
    {
        Authenticate = 0x65,
        Command = 0x6a
    }

    public int DeviceType { get; }

    public IPEndPoint RemoteEndPoint => (IPEndPoint)this._client.Client.RemoteEndPoint!;

    public virtual bool SupportsRF => false;

    public virtual bool SupportsTemperature => false;

    public Task BeginLearning() => this.SendCommand(0x03);

    public Task CancelLearning() => this.SendCommand(0x1e);

    public Task CheckData() => this.SendCommand(0x04);

    public Task CheckRFData() => this.SupportsRF ? this.SendCommand(0x1a) : throw new NotSupportedException();

    public Task CheckRFData2() => this.SupportsRF ? this.SendCommand(0x1b) : throw new NotSupportedException();

    public Task CheckTemperature() => this.SupportsTemperature ? this.SendCommand(0x01) : throw new NotSupportedException();

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public Task EnterRFSweep() => this.SupportsRF ? this.SendCommand(0x19) : throw new NotSupportedException();

    public Task SendData(byte[] data) => this.SendRequest(RequestType.Command, RMDevice._dataHeader.Combine(data));

    public Task WaitForAck()
    {
        TaskCompletionSource source = new();

        void OnAckReceived(object? sender, EventArgs e)
        {
            source.TrySetResult();
            this.AckReceived -= OnAckReceived;
        }

        this.AckReceived += OnAckReceived;
        return source.Task;
    }

    public async Task<byte[]> WaitForData()
    {
        TaskCompletionSource<byte[]> source = new();
        bool done = false;

        void OnDataReceived(object? sender, DataEventArgs<byte[]> e)
        {
            source.SetResult(e.Data);
            done = true;
            this.DataReceived -= OnDataReceived;
        }

        this.DataReceived += OnDataReceived;
        while (!done)
        {
            await Task.Delay(500).ConfigureAwait(false);
            if (!done)
            {
                await this.CheckData().ConfigureAwait(false);
            }
        }
        return await source.Task.ConfigureAwait(false);
    }

    internal Task Authenticate()
    {
        this._listeningTask.Start(TaskScheduler.Default);
        byte[] payload = new byte[0x50];
        payload[0x04] = 0x31;
        payload[0x05] = 0x31;
        payload[0x06] = 0x31;
        payload[0x07] = 0x31;
        payload[0x08] = 0x31;
        payload[0x09] = 0x31;
        payload[0x0a] = 0x31;
        payload[0x0b] = 0x31;
        payload[0x0c] = 0x31;
        payload[0x0d] = 0x31;
        payload[0x0e] = 0x31;
        payload[0x0f] = 0x31;
        payload[0x10] = 0x31;
        payload[0x11] = 0x31;
        payload[0x12] = 0x31;
        payload[0x1e] = 0x01;
        payload[0x2d] = 0x01;
        payload[0x30] = (byte)'T';
        payload[0x31] = (byte)'e';
        payload[0x32] = (byte)'s';
        payload[0x33] = (byte)'t';
        payload[0x34] = (byte)' ';
        payload[0x35] = (byte)' ';
        payload[0x36] = (byte)'1';
        return this.SendRequest(RequestType.Authenticate, payload);
    }

    protected virtual void Dispose(bool disposing)
    {
        this._disposed = true;
        this._client.Dispose();
        this._listeningTask.Wait();
    }

    private Aes GetAes()
    {
        Aes aes = Aes.Create();
        aes.KeySize = 128;
        aes.IV = RMDevice._iv;
        aes.Key = this._key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.Zeros;
        return aes;
    }

    private async Task<byte[]> GetDecryptedBytes(byte[] bytes)
    {
        using MemoryStream source = new(bytes);
        using MemoryStream target = new();
        using Aes aes = this.GetAes();
        using ICryptoTransform transform = aes.CreateDecryptor();
        using (CryptoStream cryptoStream = new(source, transform, CryptoStreamMode.Read))
        {
            await cryptoStream.CopyToAsync(target).ConfigureAwait(false);
        }
        return target.ToArray();
    }

    private async Task<byte[]> GetEncryptedBytes(byte[] bytes)
    {
        using MemoryStream source = new(bytes);
        using MemoryStream target = new();
        using Aes aes = this.GetAes();
        using ICryptoTransform transform = aes.CreateEncryptor();
        using (CryptoStream cryptoStream = new(target, transform, CryptoStreamMode.Write))
        {
            await source.CopyToAsync(cryptoStream).ConfigureAwait(false);
        }
        return target.ToArray();
    }

    private async void Listen()
    {
        try
        {
            while (!this._disposed)
            {
                UdpReceiveResult result = await this._client.ReceiveAsync().ConfigureAwait(false);
                int errorCode = result.Buffer[0x22] | (result.Buffer[0x23] << 8);
                if (errorCode != 0)
                {
                    continue;
                }
                byte[] encrypted = new byte[result.Buffer.Length - 0x38];
                Buffer.BlockCopy(result.Buffer, 0x38, encrypted, 0, encrypted.Length);
                byte[] payload = await this.GetDecryptedBytes(encrypted).ConfigureAwait(false);
                byte command = result.Buffer[0x26];
                switch (command)
                {
                    case 0x0a:
                        if (this.TemperatureReceived is { } temperatureReceived)
                        {
                            double value = (payload[0x06] * 10 + payload[0x07]) / 10.0;
                            temperatureReceived?.Invoke(this, new(value));
                        }
                        continue;
                    case 0xe9:
                        Buffer.BlockCopy(payload, 0x00, this._id = new byte[0x04], 0x00, 0x04);
                        Buffer.BlockCopy(payload, 0x04, this._key = new byte[0x10], 0x00, 0x10);
                        this.Ready?.Invoke(this, EventArgs.Empty);
                        continue;
                    case 0xee:
                    case 0xef:
                        if (payload[0] == 0x04)
                        {
                            this.AckReceived?.Invoke(this, EventArgs.Empty);
                        }
                        else if (this.DataReceived is { } dataReceived)
                        {
                            byte[] data = new byte[payload.Length - 4];
                            Buffer.BlockCopy(payload, 6, data, 0, payload.Length - 6);
                            dataReceived(this, new(data));
                        }
                        continue;
                }
                Console.Error.WriteLine("Unhandled command type: {0}", command);
            }
        }
        catch (SocketException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private Task SendCommand(byte command)
    {
        byte[] payload = new byte[16];
        payload[0] = 0x04;
        payload[1] = 0x00;
        payload[2] = command;
        return this.SendRequest(RequestType.Command, payload);
    }

    private async Task SendRequest(RequestType requestType, byte[] payload)
    {
        this._count = (this._count + 1) & 0xffff;
        byte[] packet = new byte[0x38];
        packet[0x00] = 0x5a;
        packet[0x01] = 0xa5;
        packet[0x02] = 0xaa;
        packet[0x03] = 0x55;
        packet[0x04] = 0x5a;
        packet[0x05] = 0xa5;
        packet[0x06] = 0xaa;
        packet[0x07] = 0x55;
        packet[0x24] = 0x2a;
        packet[0x25] = 0x27;
        packet[0x26] = (byte)requestType;
        packet[0x28] = (byte)(this._count & 0xff);
        packet[0x29] = (byte)(this._count >> 8);
        packet[0x2a] = this._macAddress[5];
        packet[0x2b] = this._macAddress[4];
        packet[0x2c] = this._macAddress[3];
        packet[0x2d] = this._macAddress[2];
        packet[0x2e] = this._macAddress[1];
        packet[0x2f] = this._macAddress[0];
        packet[0x30] = this._id[0];
        packet[0x31] = this._id[1];
        packet[0x32] = this._id[2];
        packet[0x33] = this._id[3];
        int checksum = payload.Checksum();
        payload = await this.GetEncryptedBytes(payload).ConfigureAwait(false);
        packet[0x34] = (byte)(checksum & 0xff);
        packet[0x35] = (byte)(checksum >> 8);
        packet = packet.Combine(payload);
        checksum = packet.Checksum();
        packet[0x20] = (byte)(checksum & 0xff);
        packet[0x21] = (byte)(checksum >> 8);
        await this._client.SendAsync(packet, packet.Length).ConfigureAwait(false);
    }
}
