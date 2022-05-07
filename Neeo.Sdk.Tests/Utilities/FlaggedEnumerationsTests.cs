using System;
using System.Linq;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Utilities;

public sealed class FlaggedEnumerationsTests
{
    [Flags]
    public enum TestEnum : ulong
    {
        [Text("ONE")]
        One = 1,

        [Text("TWO")]
        Two = 2,

        [Text("FOUR")]
        Four = 4,

        Eight = 8,
    }

    [Flags]
    public enum TestEnumInvalid
    {
        [Text("ONE")]
        One = 1,

        [Text("TWO")]
        Two = 2,

        [Text("FOUR")]
        Four = 4,

        Eight = 8,
    }

    [Fact]
    public void Should_Return_Empty_Array_For_Enums_Not_UInt64_Based()
    {
        Assert.Equal(FlaggedEnumerations.GetNames(TestEnumInvalid.One | TestEnumInvalid.Two).ToArray(), Array.Empty<string>());
    }

    [Theory]
    [InlineData(TestEnum.One | TestEnum.Two, new[] { "ONE", "TWO" })]
    [InlineData(TestEnum.Four | TestEnum.One, new[] { "ONE", "FOUR" })]
    [InlineData(TestEnum.Four | TestEnum.Two | TestEnum.One, new[] { "ONE", "TWO", "FOUR" })]
    public void Should_Return_Names_Sorted_By_Value(TestEnum value, string[] expectedOutput)
    {
        Assert.Equal(expectedOutput, FlaggedEnumerations.GetNames(value).ToArray());
    }

    [Theory]
    [InlineData(TestEnum.One)]
    [InlineData(TestEnum.Two)]
    [InlineData(TestEnum.Four)]
    [InlineData((TestEnum)0)]
    public void Should_Return_Single_Value_When_Not_Flagged_Value(TestEnum value)
    {
        string expected = TextAttribute.GetText(value);
        Assert.Equal(expected, FlaggedEnumerations.GetNames(value).Single());
    }
}