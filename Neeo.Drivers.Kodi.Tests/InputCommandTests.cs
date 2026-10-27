using System;
using Xunit;

namespace Neeo.Drivers.Kodi.Tests;

public sealed class InputCommandTests
{
    [Theory]
    [InlineData(InputCommand.PlayPauseToggle, "Input.ExecuteAction", "playpause")]
    [InlineData(InputCommand.NumberSeperator, "Input.ExecuteAction", "channelnumberseparator")]
    [InlineData(InputCommand.Play, "Input.ExecuteAction", "play")]
    [InlineData(InputCommand.Pause, "Input.ExecuteAction", "pause")]
    [InlineData(InputCommand.Stop, "Input.ExecuteAction", "stop")]
    [InlineData(InputCommand.FastForward, "Input.ExecuteAction", "fastforward")]
    [InlineData(InputCommand.Rewind, "Input.ExecuteAction", "rewind")]
    [InlineData(InputCommand.Back, "Input.Back", null)]
    [InlineData(InputCommand.Select, "Input.Select", null)]
    public void InputCommand_has_expected_method_and_action(InputCommand command, string expectedMethod, string? expectedAction)
    {
        InputCommandAttribute? attribute = InputCommandAttribute.GetAttribute(command);
        Assert.NotNull(attribute);
        Assert.Equal(expectedMethod, attribute.Method);
        Assert.Equal(expectedAction, attribute.Action);
    }

    [Fact]
    public void Every_InputCommand_has_a_valid_attribute_without_placeholders()
    {
        foreach (InputCommand command in Enum.GetValues<InputCommand>())
        {
            InputCommandAttribute? attribute = InputCommandAttribute.GetAttribute(command);
            Assert.NotNull(attribute);
            Assert.False(string.IsNullOrWhiteSpace(attribute.Method));
            Assert.DoesNotContain("xxxx", attribute.Method, StringComparison.OrdinalIgnoreCase);
            if (attribute.Action is not null)
            {
                Assert.False(string.IsNullOrWhiteSpace(attribute.Action));
                Assert.DoesNotContain("xxxx", attribute.Action, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
