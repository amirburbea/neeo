using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Lists;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Examples.Devices;

public sealed class PlayerExampleDevice : IExampleDevice
{
    private readonly PlayerWidgetController _controller;

    public PlayerExampleDevice(ILogger<PlayerExampleDevice> logger)
    {
        this._controller = new(logger);
        const string deviceName = "Player SDK Example";
        this.Builder = Device.Create(deviceName, DeviceType.MusicPlayer)
            .SetManufacturer("NEEO")
            .AddAdditionalSearchTokens("SDK","player")
            .RegisterInitializer(()=>Task.CompletedTask)
            .AddButtonGroup(ButtonGroups.Power)
            .AddButtonHandler(this._controller.HandleButtonAsync)
            .AddPlayerWidget(this._controller)
            .EnableNotifications(notifier => this._controller.Notifier = notifier);
    }

    private enum Animal
    {
        Kitten,
        Puppy
    }

    private enum PlayerKey
    {
        [Text("PLAYING")]
        Playing,

        [Text("MUTE")]
        Mute,

        [Text("SHUFFLE")]
        Shuffle,

        [Text("REPEAT")]
        Repeat,

        [Text("VOLUME")]
        Volume,

        [Text("COVER_ART_SENSOR")]
        CoverArt,

        [Text("TITLE_SENSOR")]
        Title,

        [Text("DESCRIPTION_SENSOR")]
        Description
    }

    public IDeviceBuilder Builder { get; }

    private static string GetUri(Animal animal) => $"https://neeo-sdk.neeo.io/{Enum.GetName(animal)!.ToLower()}.jpg";

    private sealed class PlayerService
    {
        private readonly Dictionary<string, object> _dictionary = new();

        public PlayerService()
        {
            // We could just set false, but for performance we'll use a pre-cached boxed version of false.
            this.SetValue(PlayerKey.Playing, BooleanBoxes.False);
            this.SetValue(PlayerKey.Mute, BooleanBoxes.False);
            this.SetValue(PlayerKey.Shuffle, BooleanBoxes.False);
            this.SetValue(PlayerKey.Repeat, BooleanBoxes.False);
            this.SetValue(PlayerKey.Volume, 50d);
            this.SetValue(PlayerKey.CoverArt, GetUri(Animal.Kitten));
            this.SetValue(PlayerKey.Title, "A Kitten");
            this.SetValue(PlayerKey.Description, "This is the description...");
        }

        public TValue GetValue<TValue>(PlayerKey key) => (TValue)this._dictionary[TextAttribute.GetText(key)];

        public void SetValue(PlayerKey key, object value) => this._dictionary[TextAttribute.GetText(key)] = value;
    }

    private sealed class PlayerWidgetController : IPlayerWidgetController
    {
        private readonly ILogger _logger;
        private readonly PlayerService _service = new();

        public PlayerWidgetController(ILogger logger) => this._logger = logger;

        bool IPlayerWidgetController.IsQueueSupported => true;

        public IDeviceNotifier? Notifier { get; set; }

        string? IPlayerWidgetController.QueueDirectoryLabel { get; }

        string? IPlayerWidgetController.RootDirectoryLabel { get; }

        public Task<string> GetCoverArtUriAsync(string deviceId) => this.GetValueAsync<string>(PlayerKey.CoverArt, deviceId);

        public Task<string> GetDescriptionAsync(string deviceId) => this.GetValueAsync<string>(PlayerKey.Description, deviceId);

        public Task<bool> GetIsMutedAsync(string deviceId) => this.GetValueAsync<bool>(PlayerKey.Mute, deviceId);

        public Task<bool> GetIsPlayingAsync(string deviceId) => this.GetValueAsync<bool>(PlayerKey.Playing, deviceId);

        public Task<bool> GetRepeatAsync(string deviceId) => this.GetValueAsync<bool>(PlayerKey.Repeat, deviceId);

        public Task<bool> GetShuffleAsync(string deviceId) => this.GetValueAsync<bool>(PlayerKey.Shuffle, deviceId);

        public Task<string> GetTitleAsync(string deviceId) => this.GetValueAsync<string>(PlayerKey.Title, deviceId);

        public Task<double> GetVolumeAsync(string deviceId) => this.GetValueAsync<double>(PlayerKey.Volume, deviceId);

        public Task HandleButtonAsync(string deviceId, string button)
        {
            return Task.CompletedTask;
        }

        Task IPlayerWidgetController.HandleQueueDirectoryActionAsync(string deviceId, string actionIdentifier)
        {
            return Task.CompletedTask;
        }

        public Task HandleRootDirectoryActionAsync(string deviceId, string actionIdentifier)
        {
            return Task.CompletedTask;
        }

        Task IPlayerWidgetController.PopulateQueueDirectoryAsync(string deviceId, IListBuilder builder)
        {
            return Task.CompletedTask;
        }

        public Task PopulateRootDirectoryAsync(string deviceId, IListBuilder builder)
        {
            return Task.CompletedTask;
        }

        public Task SetIsMutedAsync(string deviceId, bool isMuted) => this.SetValueAsync(PlayerKey.Mute, deviceId, BooleanBoxes.GetBox(isMuted));

        public Task SetIsPlayingAsync(string deviceId, bool isPlaying) => this.SetValueAsync(PlayerKey.Playing, deviceId, BooleanBoxes.GetBox(isPlaying));

        public Task SetRepeatAsync(string deviceId, bool repeat) => this.SetValueAsync(PlayerKey.Repeat, deviceId, BooleanBoxes.GetBox(repeat));

        public Task SetShuffleAsync(string deviceId, bool shuffle) => this.SetValueAsync(PlayerKey.Shuffle, deviceId, BooleanBoxes.GetBox(shuffle));

        public Task SetVolumeAsync(string deviceId, double volume) => this.SetValueAsync(PlayerKey.Volume, deviceId, volume);

        private Task<TValue> GetValueAsync<TValue>(PlayerKey key, string deviceId)
        {
            this._logger.LogWarning("Getting component state {deviceId} {key}", deviceId, key);
            return Task.FromResult(this._service.GetValue<TValue>(key));
        }

        private async Task SetValueAsync(PlayerKey key, string deviceId, object value)
        {
            this._logger.LogWarning("Setting component {deviceId} {key} to {value}", deviceId, key, value);
            if (this.Notifier != null)
            {
                await this.Notifier.SendNotificationAsync(componentName: TextAttribute.GetText(key), value: value, deviceId: deviceId);
            }
            this._service.SetValue(key, value);
        }
    }
}