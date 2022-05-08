using System;
using System.Reflection;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Utilities;

public sealed class TextAttributeTests
{
    [Flags]
    public enum TestEnum
    {
        [Text("ONE")]
        One = 1,

        [Text("TWO")]
        Two = 2,

        Four = 4
    }

    [Theory]
    [InlineData("ONE")]
    [InlineData("TWO")]
    public void GetEnum_should_return_associated_enum_for_attribute_text(string attributeText)
    {
        bool tested = false;
        foreach (FieldInfo field in typeof(TestEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetCustomAttribute<TextAttribute>() is { } attribute && attribute.Text == attributeText)
            {
                Assert.Equal(field.GetValue(null), TextAttribute.GetEnum<TestEnum>(attributeText));
                tested = true;
                break;
            }
        }
        Assert.True(tested);
    }

    [Theory]
    [InlineData("Four")]
    public void GetEnum_should_return_enum_by_name_when_missing_attribute(string name)
    {
        TestEnum expected = Enum.Parse<TestEnum>(name);
        Assert.Equal(expected, TextAttribute.GetEnum<TestEnum>(name));
    }

    [Fact]
    public void GetEnum_should_return_null_for_invalid_input()
    {
        Assert.Null(TextAttribute.GetEnum<TestEnum>("SOME_GARBAGE"));
    }

    [Theory]
    [InlineData(TestEnum.One)]
    [InlineData(TestEnum.Two)]
    public void GetText_should_return_attribute_text(TestEnum value)
    {
        string? expectedText = typeof(TestEnum).GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static)?.GetCustomAttribute<TextAttribute>()?.Text;
        Assert.NotNull(expectedText); // Verify the test data was correct and we definitely have the attribute.
        Assert.Equal(expectedText, TextAttribute.GetText(value));
    }

    [Theory]
    [InlineData(TestEnum.Four)]
    [InlineData(TestEnum.One | TestEnum.Two)]
    public void GetText_should_return_ToString_when_missing_attribute(TestEnum value)
    {
        string expectedText = value.ToString();
        Assert.Equal(expectedText, TextAttribute.GetText(value));
    }
}