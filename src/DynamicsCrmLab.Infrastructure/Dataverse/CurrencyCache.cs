using System.Collections.Concurrent;

namespace DynamicsCrmLab.Infrastructure.Dataverse;

/// <summary>
/// Remembers the currencies of an environment across callers.
/// </summary>
/// <remarks>
/// The currencies belong to the environment, not to whoever is signed in, so
/// they outlive a single request. Once the connection is opened per caller, a
/// cache held by the reader would be thrown away with every request and the
/// same two rows would be read again on each one.
/// </remarks>
public sealed class CurrencyCache
{
    private readonly ConcurrentDictionary<Guid, string> _codes = new();

    /// <summary>
    /// Gets or sets the identifier of the currency the environment was created
    /// with, or <see langword="null"/> while it has not been read yet.
    /// </summary>
    public Guid? BaseCurrencyId { get; set; }

    /// <summary>
    /// Looks up the ISO code of a currency.
    /// </summary>
    /// <param name="currencyId">The currency to look up.</param>
    /// <param name="code">The code, when it is already known.</param>
    /// <returns><see langword="true"/> when the code was remembered.</returns>
    public bool TryGetCode(Guid currencyId, out string? code) => _codes.TryGetValue(currencyId, out code);

    /// <summary>
    /// Remembers the ISO code of a currency.
    /// </summary>
    /// <param name="currencyId">The currency the code belongs to.</param>
    /// <param name="code">The code, such as EUR.</param>
    public void Remember(Guid currencyId, string code) => _codes[currencyId] = code;
}
