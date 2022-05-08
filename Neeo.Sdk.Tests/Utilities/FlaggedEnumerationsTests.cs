﻿using System;
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
    public void GetNames_should_return_empty_array_for_enums_not_ulong_based()
    {
        Assert.StrictEqual(FlaggedEnumerations.GetNames(TestEnumInvalid.One | TestEnumInvalid.Two).ToArray(), Array.Empty<string>());
    }

    [Theory]
    [InlineData(TestEnum.One | TestEnum.Two, new[] { "ONE", "TWO" })]
    [InlineData(TestEnum.Four | TestEnum.One, new[] { "ONE", "FOUR" })]
    [InlineData(TestEnum.Four | TestEnum.Two | TestEnum.One, new[] { "ONE", "TWO", "FOUR" })]
    public void GetNames_should_return_names_sorted_by_value(TestEnum value, string[] expectedOutput)
    {
        Assert.Equal(expectedOutput, FlaggedEnumerations.GetNames(value));
    }

    [Theory]
    [InlineData(TestEnum.One)]
    [InlineData(TestEnum.Two)]
    [InlineData(TestEnum.Four)]
    [InlineData((TestEnum)0)]
    public void GetNames_should_return_single_value_array_when_not_flagged_value(TestEnum value)
    {
        string expected = TextAttribute.GetText(value);
        var names = FlaggedEnumerations.GetNames(value);
        Assert.IsType<string[]>(names);
        Assert.Equal(expected, names.Single());
    }
}