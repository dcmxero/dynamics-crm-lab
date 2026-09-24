using System.Globalization;

namespace DynamicsCrmLab.Domain.Common;

/// <summary>
/// Represents a monetary amount together with its currency.
/// </summary>
/// <remarks>
/// Amounts are rounded to two decimal places using banker's rounding, which is
/// what the accounting side expects. Arithmetic across currencies is rejected
/// rather than silently producing a meaningless number.
/// </remarks>
public readonly record struct Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Gets the amount, rounded to two decimal places.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the three-letter ISO currency code, in upper case.
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Creates an amount in the given currency.
    /// </summary>
    /// <param name="amount">The amount. Must not be negative.</param>
    /// <param name="currency">The three-letter ISO currency code.</param>
    /// <returns>The amount rounded to two decimal places, halves away from zero.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the amount is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when the currency is not a three-letter code.</exception>
    public static Money Of(decimal amount, string currency = "EUR")
    {
        if (amount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must not be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length is not 3)
        {
            throw new ArgumentException("Currency must be a three-letter ISO code.", nameof(currency));
        }

        // Away from zero, because that is what the money column does with a
        // halfway amount, and the store is the record. Rounding to even here
        // would make the same price worth a different total depending on whether
        // it arrived through the application or was written straight to the
        // table, and nobody looking at the two totals could say which was right.
        return new Money(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), currency.ToUpperInvariant());
    }

    /// <summary>
    /// Creates a zero amount in the given currency.
    /// </summary>
    /// <param name="currency">The three-letter ISO currency code.</param>
    /// <returns>An amount of zero.</returns>
    public static Money Zero(string currency = "EUR") => Of(0m, currency);

    /// <summary>
    /// Adds two amounts of the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>The sum, in the shared currency.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the currencies differ.</exception>
    public static Money operator +(Money left, Money right)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Cannot add {left.Currency} to {right.Currency}.");
        }

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    /// <summary>
    /// Adds two amounts of the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>The sum, in the shared currency.</returns>
    public static Money Add(Money left, Money right) => left + right;

    /// <summary>
    /// Scales an amount by a whole number, for example a line quantity.
    /// </summary>
    /// <param name="value">The amount to scale.</param>
    /// <param name="multiplier">The multiplier. Must not be negative.</param>
    /// <returns>The scaled amount.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the multiplier is negative.</exception>
    public static Money operator *(Money value, int multiplier)
    {
        if (multiplier is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier), multiplier, "Multiplier must not be negative.");
        }

        return new Money(value.Amount * multiplier, value.Currency);
    }

    /// <summary>
    /// Scales an amount by a whole number, for example a line quantity.
    /// </summary>
    /// <param name="value">The amount to scale.</param>
    /// <param name="multiplier">The multiplier. Must not be negative.</param>
    /// <returns>The scaled amount.</returns>
    public static Money Multiply(Money value, int multiplier) => value * multiplier;

    /// <inheritdoc/>
    public override string ToString() =>
        Amount.ToString("N2", CultureInfo.InvariantCulture) + " " + Currency;
}
