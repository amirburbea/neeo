using System;
using System.Linq;
using System.Threading.Tasks;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Devices.Setup;
using Xunit;

namespace Neeo.Sdk.Tests.Devices;

public sealed class DeviceBuilderTests
{
    [Fact]
    public void Create_should_initialize_properties_with_defaults()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio);

        Assert.Equal("Stereo", builder.Name);
        Assert.Equal(DeviceType.Audio, builder.Type);
        Assert.Equal("NEEO", builder.Manufacturer);
        Assert.False(builder.HasPowerStateSensor);
        Assert.Empty(builder.Buttons);
        Assert.Empty(builder.Directories);
        Assert.Empty(builder.Sliders);
        Assert.Empty(builder.Switches);
        Assert.Empty(builder.Sensors);
        Assert.Null(builder.ButtonHandler);
        Assert.Null(builder.DriverVersion);
        Assert.Null(builder.Icon);
        Assert.Null(builder.SpecificName);
        Assert.StartsWith("apt-", builder.AdapterName);
    }

    [Fact]
    public void Create_with_empty_name_should_throw()
    {
        Assert.Throws<ArgumentException>(() => Device.Create("", DeviceType.Audio));
    }

    [Fact]
    public void Create_with_null_name_should_throw()
    {
        Assert.Throws<ArgumentNullException>(() => Device.Create(null!, DeviceType.Audio));
    }

    [Fact]
    public void SetManufacturer_should_update_manufacturer()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .SetManufacturer("Denon");

        Assert.Equal("Denon", builder.Manufacturer);
    }

    [Fact]
    public void SetDriverVersion_and_SpecificName_and_Icon_should_update_properties()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .SetDriverVersion(42)
            .SetSpecificName("AVR-X3700H")
            .SetIcon(DeviceIconOverride.Sonos);

        Assert.Equal(42, builder.DriverVersion);
        Assert.Equal("AVR-X3700H", builder.SpecificName);
        Assert.Equal(DeviceIconOverride.Sonos, builder.Icon);
    }

    [Fact]
    public void AddAdditionalSearchTokens_should_accumulate_tokens()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .AddAdditionalSearchTokens("Denon", "AVR")
            .AddAdditionalSearchTokens(["Amp", "Receiver"]);

        Assert.Equal(["Denon", "AVR", "Amp", "Receiver"], builder.AdditionalSearchTokens);
    }

    [Fact]
    public void AddButton_should_add_button_name()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .AddButton(Buttons.MuteToggle)
            .AddButton("CUSTOM_BTN", "Custom Label");

        Assert.Contains("MUTE TOGGLE", builder.Buttons);
        Assert.Contains("CUSTOM_BTN", builder.Buttons);
    }

    [Fact]
    public void AddButton_duplicate_name_should_throw()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .AddButton(Buttons.MuteToggle);

        Assert.Throws<ArgumentException>(() => builder.AddButton(Buttons.MuteToggle));
    }

    [Fact]
    public void AddButtonHandler_should_set_handler_and_prevent_duplicate()
    {
        static Task Handler(string deviceId, string button, System.Threading.CancellationToken ct) => Task.CompletedTask;

        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .AddButtonHandler(Handler);

        Assert.NotNull(builder.ButtonHandler);
        Assert.Throws<InvalidOperationException>(() => builder.AddButtonHandler(Handler));
    }

    [Fact]
    public void AddSlider_should_register_slider()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .AddSlider("volume", "Volume", (_, _) => Task.FromResult(50d), (_, _, _) => Task.CompletedTask, 0d, 100d, "%");

        Assert.Contains("volume", builder.Sliders);
    }

    [Fact]
    public void AddSlider_duplicate_or_invalid_range_should_throw()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .AddSlider("volume", null, (_, _) => Task.FromResult(50d), (_, _, _) => Task.CompletedTask);

        Assert.Throws<ArgumentException>(() => builder.AddSlider("volume", null, (_, _) => Task.FromResult(50d), (_, _, _) => Task.CompletedTask));
        Assert.Throws<ArgumentException>(() => builder.AddSlider("vol2", null, (_, _) => Task.FromResult(50d), (_, _, _) => Task.CompletedTask, rangeLow: 100d, rangeHigh: 0d));
    }

    [Fact]
    public void AddSwitch_should_register_switch()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .AddSwitch("power", "Power", (_, _) => Task.FromResult(true), (_, _, _) => Task.CompletedTask);

        Assert.Contains("power", builder.Switches);
        Assert.Throws<ArgumentException>(() => builder.AddSwitch("power", null, (_, _) => Task.FromResult(true), (_, _, _) => Task.CompletedTask));
    }

    [Fact]
    public void AddSensor_should_register_boolean_and_range_sensors()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .AddSensor("is_connected", "Connected", (_, _) => Task.FromResult(true))
            .AddSensor("temp", "Temperature", (_, _) => Task.FromResult(22.5d), 0d, 50d, "C");

        Assert.Contains("is_connected", builder.Sensors);
        Assert.Contains("temp", builder.Sensors);
        Assert.Throws<ArgumentException>(() => builder.AddSensor("is_connected", null, (_, _) => Task.FromResult(true)));
    }

    [Fact]
    public void AddPowerStateSensor_should_set_HasPowerStateSensor()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .AddPowerStateSensor((_, _) => Task.FromResult(true));

        Assert.True(builder.HasPowerStateSensor);
        Assert.Throws<InvalidOperationException>(() => builder.AddPowerStateSensor((_, _) => Task.FromResult(true)));
    }

    [Fact]
    public void AddDirectory_should_register_directory()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .AddDirectory("Radio", "Radio Stations", DirectoryRole.Root, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.CompletedTask);

        Assert.Contains("Radio", builder.Directories);
        Assert.Throws<ArgumentException>(() => builder.AddDirectory("Radio", null, null, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.CompletedTask));
    }

    [Fact]
    public void AddPlayerWidget_on_unsupported_device_type_should_throw()
    {
        IDeviceBuilder builder = Device.Create("Bulb", DeviceType.Light);

        Assert.Throws<InvalidOperationException>(() => builder.AddPlayerWidget(new DummyPlayerController()));
    }

    [Fact]
    public void AddPlayerWidget_on_media_player_should_succeed_and_prevent_duplicate()
    {
        IDeviceBuilder builder = Device.Create("Player", DeviceType.MediaPlayer)
            .AddPlayerWidget(new DummyPlayerController());

        Assert.Contains(PlayerWidgetConstants.VolumeSliderName, builder.Sliders);
        Assert.Contains(PlayerWidgetConstants.PlayingSwitchName, builder.Switches);
        Assert.Contains(PlayerWidgetConstants.RootDirectoryName, builder.Directories);
        Assert.Throws<InvalidOperationException>(() => builder.AddPlayerWidget(new DummyPlayerController()));
    }

    [Fact]
    public void DefineTiming_should_set_timing()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .DefineTiming(powerOnDelay: 1000, shutdownDelay: 500, sourceSwitchDelay: 200);

        Assert.NotNull(builder.Timing);
        Assert.Equal(1000, builder.Timing.Value.PowerOnDelay);
        Assert.Equal(500, builder.Timing.Value.ShutdownDelay);
        Assert.Equal(200, builder.Timing.Value.SourceSwitchDelay);
    }

    [Fact]
    public void BuildAdapter_with_buttons_but_no_handler_should_throw()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .AddButton(Buttons.MuteToggle);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => builder.BuildAdapter());
        Assert.Contains("handler was specified", ex.Message);
    }

    [Fact]
    public void BuildAdapter_for_Tv_without_input_should_throw()
    {
        IDeviceBuilder builder = Device.Create("TV", DeviceType.TV)
            .AddButton(Buttons.PowerOn)
            .AddButtonHandler((_, _, _) => Task.CompletedTask);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => builder.BuildAdapter());
        Assert.Contains("No input buttons defined", ex.Message);
    }

    [Fact]
    public void BuildAdapter_for_Tv_with_input_and_handler_should_succeed()
    {
        IDeviceBuilder builder = Device.Create("TV", DeviceType.TV)
            .AddButton("INPUT_HDMI_1", "HDMI 1")
            .AddButton(Buttons.PowerOn)
            .AddButtonHandler((_, _, _) => Task.CompletedTask);

        IDeviceAdapter adapter = builder.BuildAdapter();

        Assert.NotNull(adapter);
        Assert.Equal(builder.AdapterName, adapter.AdapterName);
        Assert.Equal("TV", adapter.DeviceName);
        Assert.NotEmpty(adapter.Components);
    }

    [Fact]
    public void BuildAdapter_bridge_device_without_registration_should_throw()
    {
        IDeviceBuilder builder = Device.Create("Bridge", DeviceType.Accessory)
            .AddCharacteristic(DeviceCharacteristic.BridgeDevice);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => builder.BuildAdapter());
        Assert.Contains("must support registration", ex.Message);
    }

    [Fact]
    public void BuildAdapter_bridge_device_with_registration_should_succeed()
    {
        IDeviceBuilder builder = Device.Create("Bridge", DeviceType.Accessory)
            .AddCharacteristic(DeviceCharacteristic.BridgeDevice)
            .EnableDiscovery("Discover", "Desc", (_, _) => Task.FromResult(Array.Empty<DiscoveredDevice>()))
            .EnableRegistration("Register", "Desc", _ => Task.FromResult(true), (_, _, _) => Task.FromResult(RegistrationResult.Success));

        IDeviceAdapter adapter = builder.BuildAdapter();
        Assert.NotNull(adapter);
    }

    private sealed class DummyPlayerController : IPlayerWidgetController
    {
        public Task<string> GetCoverArtAsync(string deviceId, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task<string> GetDescriptionAsync(string deviceId, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task<bool> GetIsMutedAsync(string deviceId, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> GetIsPlayingAsync(string deviceId, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> GetRepeatAsync(string deviceId, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> GetShuffleAsync(string deviceId, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<string> GetTitleAsync(string deviceId, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task<double> GetVolumeAsync(string deviceId, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(0d);
        public Task BrowseRootDirectoryAsync(string deviceId, IDirectoryBuilder builder, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task HandleRootDirectoryActionAsync(string deviceId, string actionIdentifier, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetIsMutedAsync(string deviceId, bool isMuted, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetIsPlayingAsync(string deviceId, bool isPlaying, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetRepeatAsync(string deviceId, bool repeat, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetShuffleAsync(string deviceId, bool shuffle, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetVolumeAsync(string deviceId, double volume, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
