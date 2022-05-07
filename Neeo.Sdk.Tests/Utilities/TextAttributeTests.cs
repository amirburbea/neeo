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
    public void Should_Return_Associated_Enum_From_GetEnum_For_Attribute_Text(string attributeText)
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
    [InlineData(TestEnum.One)]
    [InlineData(TestEnum.Two)]
    public void Should_Return_Attribute_Text_From_GetText(TestEnum value)
    {
        string? expectedText = typeof(TestEnum).GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static)?.GetCustomAttribute<TextAttribute>()?.Text;
        Assert.NotNull(expectedText); // Verify the test data was correct and we definitely have the attribute.
        Assert.Equal(expectedText, TextAttribute.GetText(value));
    }

    [Theory]
    [InlineData("Four")]
    public void Should_Return_Enum_By_Name_From_GetEnum_When_Missing_Attribute(string name)
    {
        TestEnum expected = Enum.Parse<TestEnum>(name);
        Assert.Equal(expected, TextAttribute.GetEnum<TestEnum>(name));
    }

    [Fact]
    public void Should_Return_Null_From_GetEnum_For_Invalid_Input()
    {
        Assert.Null(TextAttribute.GetEnum<TestEnum>("SOME_GARBAGE"));
    }

    [Theory]
    [InlineData(TestEnum.Four)]
    [InlineData(TestEnum.One | TestEnum.Two)]
    public void Should_Return_ToString_From_GetText_When_Missing_Attribute(TestEnum value)
    {
        string expectedText = value.ToString();
        Assert.Equal(expectedText, TextAttribute.GetText(value));
    }
}