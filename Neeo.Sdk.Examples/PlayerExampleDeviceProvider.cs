using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Examples;

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
            .EnableDeviceRoute(prefix => this._controller.UriPrefix = prefix, (_, path, _) => Task.FromResult(EmbeddedImages.HandleRequest(path)))
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
            this.SetValue(PlayerKey.Title, GetTitle(Pet.Kitten));
            this.SetValue(PlayerKey.Description, GetDescription(Pet.Kitten));
        }

        public Pet Pet { get; set; } = Pet.Kitten;

        public TValue GetValue<TValue>(PlayerKey key) => (TValue)this._dictionary[TextAttribute.GetText(key)];

        public void SetValue(PlayerKey key, object value) => this._dictionary[TextAttribute.GetText(key)] = value;
    }

    private sealed class PlayerWidgetController(ILogger logger) : IPlayerWidgetController
    {
        private readonly Lock _queueLock = new();
        private readonly List<Pet> _queue = [];
        private readonly PlayerService _service = new();

        public bool IsQueueSupported => true;

        public IDeviceNotifier? Notifier { get; set; }

        public string UriPrefix { get; set; } = string.Empty;

        public Task<string> GetCoverArtAsync(string deviceId, CancellationToken cancellationToken) => Task.FromResult(this.GetCoverArt(this._service.Pet));

        public Task<string> GetDescriptionAsync(string deviceId, CancellationToken cancellationToken) => Task.FromResult(this.GetValue<string>(PlayerKey.Description));

        public Task<bool> GetIsMutedAsync(string deviceId, CancellationToken cancellationToken) => Task.FromResult(this.GetValue<bool>(PlayerKey.Mute));

        public Task<bool> GetIsPlayingAsync(string deviceId, CancellationToken cancellationToken) => Task.FromResult(this.GetValue<bool>(PlayerKey.Playing));

        public Task<bool> GetRepeatAsync(string deviceId, CancellationToken cancellationToken) => Task.FromResult(this.GetValue<bool>(PlayerKey.Repeat));

        public Task<bool> GetShuffleAsync(string deviceId, CancellationToken cancellationToken) => Task.FromResult(this.GetValue<bool>(PlayerKey.Shuffle));

        public Task<string> GetTitleAsync(string deviceId, CancellationToken cancellationToken) => Task.FromResult(this.GetValue<string>(PlayerKey.Title));

        public Task<double> GetVolumeAsync(string deviceId, CancellationToken cancellationToken) => Task.FromResult(this.GetValue<double>(PlayerKey.Volume));

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
            Buttons.ClearQueue => this.ClearQueueAsync(),
            _ => Task.CompletedTask,
        };

        public Task HandleRootDirectoryActionAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken)
        {
            logger.LogInformation("Root directory action {actionIdentifier}", actionIdentifier);
            return Enum.TryParse(actionIdentifier, out Pet pet) ? this.ChangeTrackAsync(deviceId, pet) : Task.CompletedTask;
        }

        public Task BrowseRootDirectoryAsync(string deviceId, IDirectoryBuilder builder, CancellationToken cancellationToken)
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
                    builder.AddEntry(new(GetTitle(pet), GetDescription(pet), ThumbnailUri: this.GetCoverArt(pet), ActionIdentifier: Enum.GetName(pet)));
                }
            }
            return Task.CompletedTask;
        }

        public Task BrowseQueueDirectoryAsync(string deviceId, IDirectoryBuilder builder, CancellationToken cancellationToken)
        {
            Pet[] queued;
            using (this._queueLock.EnterScope())
            {
                queued = [.. this._queue];
            }
            logger.LogInformation("Browsing queue ({count} items)", queued.Length);
            builder.SetTitle("Player Queue");
            if (builder.Parameters.Offset == 0)
            {
                builder.AddButtonRow(
                    new("Clear", Inverse: true, ActionIdentifier: "QUEUE_CLEAR"),
                    new(Icon: DirectoryButtonIcon.Repeat, ActionIdentifier: "QUEUE_REPEAT"),
                    new(Icon: DirectoryButtonIcon.Shuffle, ActionIdentifier: "QUEUE_SHUFFLE")
                );
            }
            foreach (Pet pet in queued)
            {
                builder.AddEntry(new(GetTitle(pet), GetDescription(pet), ThumbnailUri: this.GetCoverArt(pet), ActionIdentifier: Enum.GetName(pet)));
            }
            builder.SetTotalMatchingItems(queued.Length);
            return Task.CompletedTask;
        }

        public Task HandleQueueDirectoryActionAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken)
        {
            logger.LogInformation("Queue directory action {actionIdentifier}", actionIdentifier);
            return actionIdentifier switch
            {
                "QUEUE_CLEAR" => this.ClearQueueAsync(),
                "QUEUE_REPEAT" => this.SetRepeatAsync(deviceId, !this._service.GetValue<bool>(PlayerKey.Repeat), cancellationToken),
                "QUEUE_SHUFFLE" => this.SetShuffleAsync(deviceId, !this._service.GetValue<bool>(PlayerKey.Shuffle), cancellationToken),
                _ when Enum.TryParse(actionIdentifier, out Pet pet) => this.ChangeTrackAsync(deviceId, pet),
                _ => Task.CompletedTask,
            };
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
                this.SetCoverArtAsync(this.GetCoverArt(pet)),
                this.SetTitleAsync(GetTitle(pet)),
                this.SetDescriptionAsync(GetDescription(pet))
            )
        };

        private string GetCoverArt(Pet pet) => $"{this.UriPrefix}{pet.ToString().ToLowerInvariant()}.jpg";

        private Task ClearQueueAsync()
        {
            logger.LogInformation("Clearing queue");
            using (this._queueLock.EnterScope())
            {
                this._queue.Clear();
            }
            return Task.CompletedTask;
        }

        private TValue GetValue<TValue>(PlayerKey key)
        {
            logger.LogInformation("Getting component state {key}", key);
            return this._service.GetValue<TValue>(key);
        }

        private Task SetCoverArtAsync(string coverArt) => this.SetValueAsync(PlayerKey.CoverArt, coverArt);

        private Task SetDescriptionAsync(string description) => this.SetValueAsync(PlayerKey.Description, description);

        private Task SetTitleAsync(string title) => this.SetValueAsync(PlayerKey.Title, title);

        private async Task SetValueAsync(PlayerKey key, object value)
        {
            logger.LogInformation("Setting component {key} to {value}", key, value);
            await (this.Notifier?.SendNotificationAsync(TextAttribute.GetText(key), value) ?? Task.CompletedTask).ConfigureAwait(false);
            this._service.SetValue(key, value);
        }
    }
}
