using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Drivers.WebOS;

internal static class Program
{
    private static async Task Main()
    {
        /*
        byte[] bytes = MessageEncryption.DeriveKey("7S9R359I");
        Console.WriteLine(Convert.ToHexString(bytes));
        Console.WriteLine("DONE");
        // IMessageEncryption encryption = new MessageEncryption();
        byte[] key = MessageEncryption.DeriveKey("7S9R359I");
        const string command = "VOLUME_MUTE on";
        string decrypted = MessageEncryption.Decrypt(key, MessageEncryption.Encrypt(key, command));
        Console.WriteLine($"'{decrypted}'");
        */
        using TVClient client = new("7S9R359I");
        await client.ConnectAsync(IPAddress.Parse("192.168.253.120")).ConfigureAwait(false);

        Console.WriteLine($"MUTE:{await client.GetMuteStateAsync()}");
        Console.WriteLine($"LANMAC:'{await client.GetWiredMacAddressAsync()}'");
        Console.WriteLine($"WIFIMAC:'{await client.GetWiFiMacAddressAsync()}'");
        Console.WriteLine($"IPC:{await client.GetIPControlStateAsync()}");
        Console.WriteLine(await client.SetMuteStateAsync(true));
        Console.WriteLine($"MUTE:{await client.GetMuteStateAsync()}");
        Console.WriteLine(await client.SetMuteStateAsync(false));
        Console.WriteLine($"MUTE:{await client.GetMuteStateAsync()}");
        Console.WriteLine($"VOLUME:{await client.GetVolumeAsync()}");
        Console.WriteLine(await client.SetVolumeAsync(await client.GetVolumeAsync()));
        Console.WriteLine($"CURRENT_APP:'{await client.GetCurrentAppAsync()}'");
        //Console.WriteLine(await client.LaunchAppAsync(App.Netflix));
        Console.WriteLine(await client.LaunchAppAsync(App.YouTube));
        Console.WriteLine(await client.SendKeyAsync(Key.VolumeUp));
        //await client.PowerOffAsync();
        Console.ReadKey();
    }

    public sealed class TinySocket : IDisposable
    {
        private readonly TcpClient _client = new(AddressFamily.InterNetwork);

        public bool IsConnected => this._client.Connected;

        public void Close() => this._client.Close();

        public async ValueTask ConnectAsync(string host)
        {
            using CancellationTokenSource source = new(Constants.Timeout);
            await this._client.ConnectAsync(host, Constants.Port, source.Token);
        }

        public async ValueTask ConnectAsync(IPAddress ipAddress)
        {
            using CancellationTokenSource source = new(Constants.Timeout);
            await this._client.ConnectAsync(new(ipAddress, Constants.Port), source.Token);
        }

        public void Dispose() => this._client.Dispose();

        public async ValueTask<byte[]> ReadAsync(int bufferSize = 32 * 1024)
        {
            using CancellationTokenSource source = new(Constants.Timeout);
            using IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(bufferSize);
            int bytesRead = await this._client.GetStream().ReadAsync(owner.Memory, source.Token).ConfigureAwait(false);
            return owner.Memory[0..bytesRead].ToArray();
        }

        public async ValueTask WriteAsync(ReadOnlyMemory<byte> data)
        {
            using CancellationTokenSource source = new(Constants.Timeout);
            await this._client.GetStream().WriteAsync(data, source.Token);
        }

        private static class Constants
        {
            public const int Port = 9761;
            public const int Timeout = 5000;
        }
    }

    public sealed class TVClient : IDisposable
    {
        private readonly byte[] _key;
        private readonly TinySocket _socket = new();

        public TVClient(string keyCode) => this._key = MessageEncryption.DeriveKey(keyCode);

        public void Close() => this._socket.Close();

        public ValueTask ConnectAsync(string host) => this._socket.ConnectAsync(host);

        public ValueTask ConnectAsync(IPAddress ipAddress) => this._socket.ConnectAsync(ipAddress);

        public void Dispose() => this._socket.Dispose();

        public async ValueTask<string> GetCurrentAppAsync() => (await this.SendCommandAsync("CURRENT_APP").ConfigureAwait(false))[4..];

        public async ValueTask<bool> GetIPControlStateAsync() => "ON" == await this.SendCommandAsync("GET_IPCONTROL_STATE").ConfigureAwait(false);

        public async ValueTask<bool> GetMuteStateAsync() => (await this.SendCommandAsync("MUTE_STATE").ConfigureAwait(false))[5..] == "on";

        public async ValueTask<int> GetVolumeAsync() => int.Parse((await this.SendCommandAsync("CURRENT_VOL").ConfigureAwait(false))[4..]);

        public ValueTask<string> GetWiFiMacAddressAsync() => this.GetMacAddressAsync("wifi");

        public ValueTask<string> GetWiredMacAddressAsync() => this.GetMacAddressAsync("wired");

        public ValueTask<bool> LaunchAppAsync(App app) => this.LaunchAppAsync(AppName.Of(app));

        public async ValueTask<bool> LaunchAppAsync(string appName) => "OK" == await SendCommandAsync($"APP_LAUNCH {appName}").ConfigureAwait(false);

        public async ValueTask PowerOffAsync() => await this.SendCommandAsync("POWER off").ConfigureAwait(false);

        public async ValueTask<bool> SendKeyAsync(Key key) => "OK" == await this.SendCommandAsync($"KEY_ACTION {KeyName.Of(key)}").ConfigureAwait(false);

        public ValueTask<bool> SetInputAsync(Input input) => this.LaunchAppAsync($"com.webos.app.{InputName.Of(input)}");

        public async ValueTask<bool> SetMuteStateAsync(bool value) => "OK" == await this.SendCommandAsync($"VOLUME_MUTE {(value ? "on" : "off")}").ConfigureAwait(false);

        public async ValueTask<string> SetPictureModeAsync(string pictureMode) => await this.SendCommandAsync($"PICTURE_MODE {pictureMode}");

        public async ValueTask<bool> SetVolumeAsync(int level) => level is < 0 or > 100
            ? throw new ArgumentOutOfRangeException(nameof(level))
            : "OK" == await this.SendCommandAsync($"VOLUME_CONTROL {level}").ConfigureAwait(false);

        private ValueTask<string> GetMacAddressAsync(string type) => this.SendCommandAsync($"GET_MACADDRESS {type}");

        private async ValueTask<string> SendCommandAsync(string command)
        {
            byte[] encrypted = MessageEncryption.Encrypt(this._key, command);
            await this._socket.WriteAsync(encrypted).ConfigureAwait(false);
            byte[] response = await this._socket.ReadAsync().ConfigureAwait(false);
            return MessageEncryption.Decrypt(this._key, response);
        }
    }
}