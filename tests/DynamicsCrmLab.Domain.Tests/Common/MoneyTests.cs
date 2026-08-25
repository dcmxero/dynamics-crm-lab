using DynamicsCrmLab.Domain.Common;
using FluentAssertions;
using Xunit;

namespace DynamicsCrmLab.Domain.Tests.Common;

public sealed class MoneyTests
{
    [Fact]
    public void Of_RejectsNegativeAmount()
    {
        var act = () => Money.Of(-1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Of_RejectsInvalidCurrencyCode()
    {
        var act = () => Money.Of(10m, "EURO");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Of_NormalisesTheCurrencyCodeToUpperCase()
    {
        Money.Of(10m, "usd").Currency.Should().Be("USD");
    }

    [Fact]
    public void Addition_RejectsDifferentCurrencies()
    {
        var act = () => Money.Of(10m, "EUR") + Money.Of(10m, "USD");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Addition_SumsAmountsOfTheSameCurrency()
    {
        (Money.Of(10m) + Money.Of(20m)).Should().Be(Money.Of(30m));
    }

    [Fact]
    public void Multiplication_ScalesTheAmount()
    {
        (Money.Of(10m) * 3).Should().Be(Money.Of(30m));
    }

    [Theory]
    [InlineData(10.344, 10.34)]
    [InlineData(10.346, 10.35)]
    public void Of_RoundsToTwoDecimalPlaces(decimal input, decimal expected)
    {
        Money.Of(input).Amount.Should().Be(expected);
    }
}
