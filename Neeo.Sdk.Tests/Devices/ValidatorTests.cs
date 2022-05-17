using System;
using Neeo.Sdk.Devices;
using Xunit;

namespace Neeo.Sdk.Tests.Devices;

public sealed class ValidatorTests
{
    [Fact]
    public void ValidateDelay_should_throw_if_greater_than_60000()
    {
        Assert.Throws<ArgumentException>(() => Validator.ValidateDelay(60001));
        Assert.Null(Validator.ValidateDelay(null));
    }

    [Fact]
    public void ValidateDelay_should_throw_if_negative()
    {
        Assert.Throws<ArgumentException>(() => Validator.ValidateDelay(-400));
        Assert.Equal(1000, Validator.ValidateDelay(1000));
    }

    [Fact]
    public void ValidateNotNegative_should_throw_if_negative()
    {
        Assert.Throws<ArgumentException>(() => Validator.ValidateNotNegative(-400));
        Assert.Throws<ArgumentException>(() => Validator.ValidateNotNegative((int?)-400));
        Assert.Equal(20, Validator.ValidateNotNegative(20));
        Assert.Equal((int?)10, Validator.ValidateNotNegative((int?)10));
        Assert.Null(Validator.ValidateNotNegative(default(int?)));
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
        double low = 0d;
        double high = 1d;
        Assert.True(Validator.ValidateRange(low, high) is { Length: 2 } range && range[0] == low && range[1] == high);
    }

    [Fact]
    public void ValidateRange_should_throw_if_low_is_less_than_high()
    {
        Assert.Throws<ArgumentException>(() => Validator.ValidateRange(0d, -1d));
        Assert.True(Validator.ValidateRange(10d, 11d) is { Length: 2 } range && range[0] == 10d && range[1] == 11d);
    }

    [Fact]
    public void ValidateText_should_throw_if_length_less_than_minLength_or_greater_than_maxLength()
    {
        Assert.Throws<ArgumentException>(() => Validator.ValidateText("abc", minLength: 4));
        Assert.Throws<ArgumentException>(() => Validator.ValidateText("abc", maxLength: 2));
        Assert.Equal("abc", Validator.ValidateText("abc"));
    }

    [Fact]
    public void ValidateText_should_throw_if_null_and_not_allowNull()
    {
        Assert.Throws<ArgumentException>(() => Validator.ValidateText(null, allowNull: false));
        Assert.Null(Validator.ValidateText(null, allowNull: true));
    }
}