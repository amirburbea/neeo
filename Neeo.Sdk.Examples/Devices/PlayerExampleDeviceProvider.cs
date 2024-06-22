using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Examples.Devices;

public sealed class PlayerExampleDeviceProvider : IDeviceProvider
{
    internal static readonly Pet[] Pets = Enum.GetValues<Pet>();

    private readonly PlayerWidgetController _controller;

    public PlayerExampleDeviceProvider(ILogger<PlayerExampleDeviceProvider> logger)
    {
        this._controller = new(logger);
        const string deviceName = "SDK Player Example";
        this.DeviceBuilder = Device.Create(deviceName, DeviceType.MusicPlayer)
            .SetDriverVersion(3)
            .AddButtonGroup(ButtonGroups.MenuAndBack)
            .AddButton(Buttons.Guide)
            .AddAdditionalSearchTokens("SDK", "player")
            .AddButtonGroup(ButtonGroups.Power)
            .AddButtonHandler(this._controller.HandleButtonAsync)
            .AddPlayerWidget(this._controller)
            .EnableNotifications(notifier => this._controller.Notifier = notifier);
    }

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

    public IDeviceBuilder DeviceBuilder { get; }

    private static string GetCoverArt(Pet pet) => $"https://neeo-sdk.neeo.io/{pet.ToString().ToLower()}.jpg";

    private static string GetDescription(Pet pet) => $"A song about my {pet.ToString().ToLower()}";

    private static string GetTitle(Pet pet) => $"The {pet.ToString().ToLower()} song";

    private sealed class PlayerService
    {
        private readonly ConcurrentDictionary<string, object> _dictionary = [];

        public PlayerService()
        {
            // We could just set false, but for performance we'll use a pre-cached boxed version of false.
            this.SetValue(PlayerKey.Playing, false);
            this.SetValue(PlayerKey.Mute, false);
            this.SetValue(PlayerKey.Shuffle, false);
            this.SetValue(PlayerKey.Repeat, false);
            this.SetValue(PlayerKey.Volume, 50d);
            this.SetValue(PlayerKey.CoverArt, GetCoverArt(Pet.Kitten));
            this.SetValue(PlayerKey.Title, GetTitle(Pet.Kitten));
            this.SetValue(PlayerKey.Description, GetDescription(Pet.Kitten));
        }

        public Pet Pet { get; set; } = Pet.Kitten;

        public TValue GetValue<TValue>(PlayerKey key) => (TValue)this._dictionary[TextAttribute.GetText(key)];

        public void SetValue(PlayerKey key, object value) => this._dictionary[TextAttribute.GetText(key)] = value;
    }

    private sealed class PlayerWidgetController(ILogger logger) : IPlayerWidgetController
    {
        private readonly PlayerService _service = new();

        public bool IsQueueSupported => false;

        public IDeviceNotifier Notifier { get; set; } = new DummyDeviceNotifier();

        string? IPlayerWidgetController.QueueDirectoryLabel { get; }

        string? IPlayerWidgetController.RootDirectoryLabel { get; }

        public Task<string> GetCoverArtAsync(string deviceId, CancellationToken cancellationToken) => this.GetValueAsync<string>(PlayerKey.CoverArt);

        public Task<string> GetDescriptionAsync(string deviceId, CancellationToken cancellationToken) => this.GetValueAsync<string>(PlayerKey.Description);

        public Task<bool> GetIsMutedAsync(string deviceId, CancellationToken cancellationToken) => this.GetValueAsync<bool>(PlayerKey.Mute);

        public Task<bool> GetIsPlayingAsync(string deviceId, CancellationToken cancellationToken) => this.GetValueAsync<bool>(PlayerKey.Playing);

        public Task<bool> GetRepeatAsync(string deviceId, CancellationToken cancellationToken) => this.GetValueAsync<bool>(PlayerKey.Repeat);

        public Task<bool> GetShuffleAsync(string deviceId, CancellationToken cancellationToken) => this.GetValueAsync<bool>(PlayerKey.Shuffle);

        public Task<string> GetTitleAsync(string deviceId, CancellationToken cancellationToken) => this.GetValueAsync<string>(PlayerKey.Title);

        public Task<double> GetVolumeAsync(string deviceId, CancellationToken cancellationToken) => this.GetValueAsync<double>(PlayerKey.Volume);

        public Task HandleButtonAsync(string deviceId, string button, CancellationToken cancellationToken = default) => Button.TryResolve(button) switch
        {
            Buttons.Play => this.SetIsPlayingAsync(deviceId, true, cancellationToken),
            Buttons.PlayPauseToggle => this.SetIsPlayingAsync(deviceId, !this._service.GetValue<bool>(PlayerKey.Playing), cancellationToken),
            Buttons.Pause => this.SetIsPlayingAsync(deviceId, false, cancellationToken),
            Buttons.VolumeUp => this.SetVolumeAsync(deviceId, Math.Min(this._service.GetValue<double>(PlayerKey.Volume) + 5d, 100d), cancellationToken),
            Buttons.VolumeDown => this.SetVolumeAsync(deviceId, Math.Max(this._service.GetValue<double>(PlayerKey.Volume) - 5d, 0d), cancellationToken),
            Buttons.NextTrack => this.ChangeTrackAsync(deviceId, (Pet)((int)this._service.Pet + 1)),
            Buttons.PreviousTrack => this.ChangeTrackAsync(deviceId, (Pet)((int)this._service.Pet - 1)),
            Buttons.MuteToggle => this.SetIsMutedAsync(deviceId, !this._service.GetValue<bool>(PlayerKey.Mute), cancellationToken),
            _ => Task.CompletedTask,
        };

        Task IPlayerWidgetController.HandleQueueDirectoryActionAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken)
        {
            // Queue is not supported for this demo.
            return Task.CompletedTask;
        }

        public Task HandleRootDirectoryActionAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken)
        {
            return Enum.TryParse(actionIdentifier, out Pet pet) ? this.ChangeTrackAsync(deviceId, pet) : Task.CompletedTask;
        }

        Task IPlayerWidgetController.BrowseQueueDirectoryAsync(string deviceId, DirectoryBuilder builder, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task BrowseRootDirectoryAsync(string deviceId, DirectoryBuilder builder, CancellationToken cancellationToken)
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
                    builder.AddEntry(new(GetTitle(pet), GetDescription(pet), Enum.GetName(pet), ThumbnailUri: GetCoverArt(pet)));
                }
            }
            return Task.CompletedTask;
        }

        public Task SetIsMutedAsync(string deviceId, bool isMuted, CancellationToken cancellationToken) => this.SetValueAsync(PlayerKey.Mute, BooleanBoxes.GetBox(isMuted));

        public Task SetIsPlayingAsync(string deviceId, bool isPlaying, CancellationToken cancellationToken) => this.SetValueAsync(PlayerKey.Playing, BooleanBoxes.GetBox(isPlaying));

        public Task SetRepeatAsync(string deviceId, bool repeat, CancellationToken cancellationToken) => this.SetValueAsync(PlayerKey.Repeat, BooleanBoxes.GetBox(repeat));

        public Task SetShuffleAsync(string deviceId, bool shuffle, CancellationToken cancellationToken) => this.SetValueAsync(PlayerKey.Shuffle, BooleanBoxes.GetBox(shuffle));

        public Task SetVolumeAsync(string deviceId, double volume, CancellationToken cancellationToken) => this.SetValueAsync(PlayerKey.Volume, volume);

        private Task ChangeTrackAsync(string deviceId, Pet pet) => (int)pet switch
        {
            < 0 => this.ChangeTrackAsync(deviceId, Pets[^1]), // Did we subtract 1 from 0? Go to last value.
            int value when value >= Pets.Length => this.ChangeTrackAsync(deviceId, 0), // Did we add 1 to the last value? Go to 0.
            _ => Task.WhenAll(
                Task.FromResult(this._service.Pet = pet),
                this.SetCoverArtAsync(GetCoverArt(pet)),
                this.SetTitleAsync(GetTitle(pet)),
                this.SetDescriptionAsync(GetDescription(pet))
            )
        };

        private Task<TValue> GetValueAsync<TValue>(PlayerKey key)
        {
            logger.LogInformation("Getting component state {key}", key);
            return Task.FromResult(this._service.GetValue<TValue>(key));
        }

        private Task SetCoverArtAsync(string coverArt) => this.SetValueAsync(PlayerKey.CoverArt, coverArt);

        private Task SetDescriptionAsync(string description) => this.SetValueAsync(PlayerKey.Description, description);

        private Task SetTitleAsync(string title) => this.SetValueAsync(PlayerKey.Title, title);

        private async Task SetValueAsync(PlayerKey key, object value)
        {
            logger.LogInformation("Setting component {key} to {value}", key, value);
            await this.Notifier.SendNotificationAsync(TextAttribute.GetText(key), value).ConfigureAwait(false);
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
