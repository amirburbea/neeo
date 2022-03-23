using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Lists;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Examples.Devices;

public sealed class PlayerExampleDeviceProvider : IDeviceProvider
{
    internal static readonly Pet[] Pets = Enum.GetValues<Pet>();

    private readonly PlayerWidgetController _controller;

    public PlayerExampleDeviceProvider(ILogger<PlayerExampleDeviceProvider> logger)
    {
        this._controller = new(logger);
        const string deviceName = "Player SDK Example";
        this.DeviceBuilder = Device.Create(deviceName, DeviceType.MusicPlayer)
            .SetDriverVersion(3)
            .AddButtonGroup(ButtonGroups.MenuAndBack)
            .AddButton(Buttons.Guide)
            .AddAdditionalSearchTokens("SDK", "player")
            .RegisterInitializer(() => Task.CompletedTask)
            .AddButtonGroup(ButtonGroups.Power)
            .AddButtonHandler(this._controller.HandleButtonAsync)
            .AddPlayerWidget(this._controller)
            .EnableNotifications(notifier => this._controller.Notifier = notifier);
    }

    public IDeviceBuilder DeviceBuilder { get; }

    internal enum Pet
    {
        Kitten,
        Puppy,
        File
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
        Description,
    }

    private static string GetCoverArt(Pet pet) => $"https://neeo-sdk.neeo.io/{pet.ToString().ToLower()}.jpg";

    private static string GetDescription(Pet pet) => $"A song about my {pet.ToString().ToLower()}";

    private static string GetTitle(Pet pet) => $"The {pet.ToString().ToLower()} song";

    private sealed class PlayerService
    {
        private readonly ConcurrentDictionary<string, object> _dictionary = new();

        public PlayerService()
        {
            // We could just set false, but for performance we'll use a pre-cached boxed version of false.
            this.SetValue(PlayerKey.Playing, BooleanBoxes.False);
            this.SetValue(PlayerKey.Mute, BooleanBoxes.False);
            this.SetValue(PlayerKey.Shuffle, BooleanBoxes.False);
            this.SetValue(PlayerKey.Repeat, BooleanBoxes.False);
            this.SetValue(PlayerKey.Volume, 50d);
            this.SetValue(PlayerKey.CoverArt, GetCoverArt(Pet.Kitten));
            this.SetValue(PlayerKey.Title, GetTitle(Pet.Kitten));
            this.SetValue(PlayerKey.Description, GetDescription(Pet.Kitten));
        }

        public Pet Pet { get; set; } = Pet.Kitten;

        public TValue GetValue<TValue>(PlayerKey key) => (TValue)this._dictionary[TextAttribute.GetText(key)];

        public void SetValue(PlayerKey key, object value) => this._dictionary[TextAttribute.GetText(key)] = value;
    }

    private sealed class PlayerWidgetController : IPlayerWidgetController
    {
        private readonly ILogger _logger;
        private readonly PlayerService _service = new();

        public PlayerWidgetController(ILogger logger) => this._logger = logger;

        public bool IsQueueSupported => false;

        public IDeviceNotifier Notifier { get; set; } = new DummyDeviceNotifier();

        string? IPlayerWidgetController.QueueDirectoryLabel { get; }

        string? IPlayerWidgetController.RootDirectoryLabel { get; }

        public Task<string> GetCoverArtAsync(string deviceId) => this.GetValueAsync<string>(PlayerKey.CoverArt, deviceId);

        public Task<string> GetDescriptionAsync(string deviceId) => this.GetValueAsync<string>(PlayerKey.Description, deviceId);

        public Task<bool> GetIsMutedAsync(string deviceId) => this.GetValueAsync<bool>(PlayerKey.Mute, deviceId);

        public Task<bool> GetIsPlayingAsync(string deviceId) => this.GetValueAsync<bool>(PlayerKey.Playing, deviceId);

        public Task<bool> GetRepeatAsync(string deviceId) => this.GetValueAsync<bool>(PlayerKey.Repeat, deviceId);

        public Task<bool> GetShuffleAsync(string deviceId) => this.GetValueAsync<bool>(PlayerKey.Shuffle, deviceId);

        public Task<string> GetTitleAsync(string deviceId) => this.GetValueAsync<string>(PlayerKey.Title, deviceId);

        public Task<double> GetVolumeAsync(string deviceId) => this.GetValueAsync<double>(PlayerKey.Volume, deviceId);

        public Task HandleButtonAsync(string deviceId, string button) => Button.TryResolve(button) switch
        {
            Buttons.Play => this.SetIsPlayingAsync(deviceId, true),
            Buttons.PlayPauseToggle => this.SetIsPlayingAsync(deviceId, !this._service.GetValue<bool>(PlayerKey.Playing)),
            Buttons.Pause => this.SetIsPlayingAsync(deviceId, false),
            Buttons.VolumeUp => this.SetVolumeAsync(deviceId, Math.Min(this._service.GetValue<double>(PlayerKey.Volume) + 5d, 100d)),
            Buttons.VolumeDown => this.SetVolumeAsync(deviceId, Math.Max(this._service.GetValue<double>(PlayerKey.Volume) - 5d, 0d)),
            Buttons.NextTrack => this.ChangeTrackAsync(deviceId, (Pet)((int)this._service.Pet + 1)),
            Buttons.PreviousTrack => this.ChangeTrackAsync(deviceId, (Pet)((int)this._service.Pet - 1)),
            Buttons.MuteToggle => this.SetIsMutedAsync(deviceId, !this._service.GetValue<bool>(PlayerKey.Mute)),
            _ => Task.CompletedTask,
        };

        Task IPlayerWidgetController.HandleQueueDirectoryActionAsync(string deviceId, string actionIdentifier)
        {
            // Queue is not supported for this demo.
            return Task.CompletedTask;
        }

        public Task HandleRootDirectoryActionAsync(string deviceId, string actionIdentifier)
        {
            return Enum.TryParse(actionIdentifier, out Pet pet) ? this.ChangeTrackAsync(deviceId, pet) : Task.CompletedTask;
        }

        Task IPlayerWidgetController.PopulateQueueDirectoryAsync(string deviceId, IListBuilder builder)
        {
            return Task.CompletedTask;
        }

        public Task PopulateRootDirectoryAsync(string deviceId, IListBuilder builder)
        {
            if (string.IsNullOrEmpty(builder.Parameters.BrowseIdentifier))
            {
                builder.AddHeader("Artists");
                for (int i = 0; i < 5; i++)
                {
                    string name = $"Artist #{i}";
                    builder.AddEntry(new(name, name, name));
                }
            }
            else
            {
                builder.AddHeader(builder.Parameters.BrowseIdentifier);
                foreach (Pet pet in Pets)
                {
                    builder.AddEntry(new(GetTitle(pet), GetDescription(pet), null, Enum.GetName(pet), thumbnailUri: GetCoverArt(pet)));
                }
            }
            return Task.CompletedTask;
        }

        public Task SetIsMutedAsync(string deviceId, bool isMuted) => this.SetValueAsync(PlayerKey.Mute, deviceId, BooleanBoxes.GetBox(isMuted));

        public Task SetIsPlayingAsync(string deviceId, bool isPlaying) => this.SetValueAsync(PlayerKey.Playing, deviceId, BooleanBoxes.GetBox(isPlaying));

        public Task SetRepeatAsync(string deviceId, bool repeat) => this.SetValueAsync(PlayerKey.Repeat, deviceId, BooleanBoxes.GetBox(repeat));

        public Task SetShuffleAsync(string deviceId, bool shuffle) => this.SetValueAsync(PlayerKey.Shuffle, deviceId, BooleanBoxes.GetBox(shuffle));

        public Task SetVolumeAsync(string deviceId, double volume) => this.SetValueAsync(PlayerKey.Volume, deviceId, volume);

        private Task ChangeTrackAsync(string deviceId, Pet pet) => (int)pet switch
        {
            < 0 => this.ChangeTrackAsync(deviceId, Pets[^1]), // Did we subtract 1 from 0? Go to last value.
            int value when value >= Pets.Length => this.ChangeTrackAsync(deviceId, 0), // Did we add 1 to the last value? Go to 0.
            _ => Task.WhenAll(
                Task.FromResult(this._service.Pet = pet),
                this.SetCoverArtAsync(deviceId, GetCoverArt(pet)),
                this.SetTitleAsync(deviceId, GetTitle(pet)),
                this.SetDescriptionAsync(deviceId, GetDescription(pet))
            )
        };

        private Task<TValue> GetValueAsync<TValue>(PlayerKey key, string deviceId)
        {
            this._logger.LogInformation("Getting component state {deviceId} {key}", deviceId, key);
            return Task.FromResult(this._service.GetValue<TValue>(key));
        }

        private Task SetCoverArtAsync(string deviceId, string coverArt) => this.SetValueAsync(PlayerKey.CoverArt, deviceId, coverArt);

        private Task SetDescriptionAsync(string deviceId, string description) => this.SetValueAsync(PlayerKey.Description, deviceId, description);

        private Task SetTitleAsync(string deviceId, string title) => this.SetValueAsync(PlayerKey.Title, deviceId, title);

        private async Task SetValueAsync(PlayerKey key, string deviceId, object value)
        {
            this._logger.LogInformation("Setting component {deviceId} {key} to {value}", deviceId, key, value);
            await this.Notifier.SendNotificationAsync(componentName: TextAttribute.GetText(key), value: value, deviceId: deviceId).ConfigureAwait(false);
            this._service.SetValue(key, value);
        }

        private sealed class DummyDeviceNotifier : IDeviceNotifier
        {
            bool IDeviceNotifier.SupportsPowerNotifications => true;

            Task IDeviceNotifier.SendNotificationAsync(string componentName, object value, string deviceId, CancellationToken cancellationToken) => Task.CompletedTask;

            Task IDeviceNotifier.SendPowerNotificationAsync(bool powerState, string deviceId, CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}