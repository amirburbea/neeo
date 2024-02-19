using System;
using Neeo.Sdk.Devices;
using Xunit;

namespace Neeo.Sdk.Tests.Devices;

public sealed class ValidatorTests
{
    [Fact]
    public void ValidateDelay_should_return_value_if_null_or_between_0_and_60000()
    {
        Assert.Null(Validator.ValidateDelay(null));
        Assert.Equal(0, Validator.ValidateDelay(0));
        Assert.Equal(400, Validator.ValidateDelay(400));
        Assert.Equal(60000, Validator.ValidateDelay(60000));
    }

    [Fact]
    public void ValidateDelay_should_throw_if_greater_than_60000()
    {
        Assert.Throws<ArgumentException>(() => Validator.ValidateDelay(60001));
    }

    [Fact]
    public void ValidateDelay_should_throw_if_negative()
    {
        Assert.Throws<ArgumentException>(() => Validator.ValidateDelay(-400));
    }

    [Fact]
    public void ValidateNotNegative_should_return_value_if_null_or_not_negative()
    {
        Assert.Null(Validator.ValidateNotNegative(default(int?)));
        Assert.Equal(20, Validator.ValidateNotNegative(20));
        Assert.Equal((int?)10, Validator.ValidateNotNegative((int?)10));
    }

    [Fact]
    public void ValidateNotNegative_should_throw_if_negative()
    {
        Assert.Throws<ArgumentException>(() => Validator.ValidateNotNegative(-400));
        Assert.Throws<ArgumentException>(() => Validator.ValidateNotNegative((int?)-400));
    }

    [Fact]
    public void ValidateRange_returns_array_of_low_and_high_when_low_is_less_than_high()
    {
        Assert.True(Validator.ValidateRange(0d, 0.1d) is [0d, 0.1d]);
        Assert.True(Validator.ValidateRange(0d, 100d) is [0d, 100d]);
    }

    [Fact]
    public void ValidateRange_should_throw_for_NaN_or_Infinity()
    {
        Assert.Throws<ArgumentException>(() => Validator.ValidateRange(0d, double.NaN));
        Assert.Throws<ArgumentException>(() => Validator.ValidateRange(double.NaN, 0d));
        Assert.Throws<ArgumentException>(() => Validator.ValidateRange(0d, double.NegativeInfinity));
        Assert.Throws<ArgumentException>(() => Validator.ValidateRange(double.NegativeInfinity, 0d));
        Assert.Throws<ArgumentException>(() => Validator.ValidateRange(0d, double.PositiveInfinity));
        Assert.Throws<ArgumentException>(() => Validator.ValidateRange(double.PositiveInfinity, 0d));
    }

    [Fact]
    public void ValidateRange_should_throw_if_low_is_less_than_or_equal_to_high()
    {
        Assert.Throws<ArgumentException>(() => Validator.ValidateRange(0d, -1d));
        Assert.Throws<ArgumentException>(() => Validator.ValidateRange(0d, 0d));
    }

    [Fact]
    public void ValidateText_should_return_value_if_in_length_constraints_or_null_and_allowNull()
    {
        Assert.Equal("abc", Validator.ValidateText("abc"));
        Assert.Null(Validator.ValidateText(null, allowNull: true));
    }

    [Fact]
    public void ValidateText_should_throw_if_length_not_between_minLength_and_maxLength()
    {
        Assert.Throws<ArgumentException>(() => Validator.ValidateText("abc", minLength: 4));
        Assert.Throws<ArgumentException>(() => Validator.ValidateText("abc", maxLength: 2));
    }

    [Fact]
    public void ValidateText_should_throw_if_null_and_not_allowNull()
    {
        Assert.Throws<ArgumentException>(() => Validator.ValidateText(null, allowNull: false));
    }
}
