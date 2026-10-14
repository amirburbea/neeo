using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Drivers.Hisense.Tests;

public sealed class HisenseKeyMappingTests
{
    [Theory]
    [InlineData(RemoteKey.Back, "KEY_RETURNS")]
    [InlineData(RemoteKey.Down, "KEY_DOWN")]
    [InlineData(RemoteKey.OK, "KEY_OK")]
    [InlineData(RemoteKey.Left, "KEY_LEFT")]
    [InlineData(RemoteKey.Right, "KEY_RIGHT")]
    [InlineData(RemoteKey.Up, "KEY_UP")]
    [InlineData(RemoteKey.Home, "KEY_HOME")]
    [InlineData(RemoteKey.Menu, "KEY_MENU")]
    [InlineData(RemoteKey.Power, "KEY_POWER")]
    [InlineData(RemoteKey.MuteToggle, "KEY_MUTE")]
    [InlineData(RemoteKey.VolumeUp, "KEY_VOLUME_UP")]
    [InlineData(RemoteKey.VolumeDown, "KEY_VOLUME_DOWN")]
    [InlineData(RemoteKey.Play, "KEY_PLAY")]
    [InlineData(RemoteKey.Pause, "KEY_PAUSE")]
    [InlineData(RemoteKey.Stop, "KEY_STOP")]
    [InlineData(RemoteKey.FastForward, "KEY_FORWARDS")]
    [InlineData(RemoteKey.Rewind, "KEY_BACK")]
    [InlineData(RemoteKey.Exit, "KEY_EXIT")]
    public void RemoteKey_maps_to_expected_protocol_text(RemoteKey key, string expectedText)
    {
        string actualText = TextAttribute.GetText(key);
        Assert.Equal(expectedText, actualText);
    }
}
