using System.Collections.Concurrent;
using DynamicsCrmLab.Schema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Infrastructure.Dataverse;

/// <summary>
/// Reads the currencies an environment keeps money in.
/// </summary>
/// <remarks>
/// A money column carries an amount and the currency it was written in, and the
/// currency is a row of its own. Assuming one instead of reading it is how an
/// amount ends up labelled with a currency nobody stored.
///
/// The set of currencies in an environment changes about as often as the
/// environment is set up, so each one is read once and kept.
/// </remarks>
/// <param name="client">The Dataverse connection.</param>
public sealed class DataverseCurrencies(IDataverseClient client)
{
    private readonly ConcurrentDictionary<Guid, string> _codes = new();

    private Guid? _baseCurrencyId;

    /// <summary>
    /// Gets the currency the environment was created with.
    /// </summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The identifier and the code of the base currency.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the environment reports no currency at all.
    /// </exception>
    public async Task<(Guid Id, string Code)> BaseAsync(CancellationToken cancellationToken = default)
    {
        if (_baseCurrencyId is { } known)
        {
            return (known, _codes[known]);
        }

        var organization = await client.RetrieveMultipleAsync(
            new QueryExpression(OrganizationSchema.EntityName)
            {
                ColumnSet = new ColumnSet(OrganizationSchema.BaseCurrency),
                TopCount = 1
            },
            cancellationToken).ConfigureAwait(false);

        if (organization.Entities.Count is 0
            || organization.Entities[0].GetAttributeValue<EntityReference>(OrganizationSchema.BaseCurrency)
                is not { } reference)
        {
            throw new InvalidOperationException("The environment reports no base currency.");
        }

        var code = await CodeOfAsync(reference.Id, cancellationToken).ConfigureAwait(false);

        _baseCurrencyId = reference.Id;

        return (reference.Id, code);
    }

    /// <summary>
    /// Reads the ISO code of a currency.
    /// </summary>
    /// <param name="currencyId">The currency to read.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The ISO code, such as EUR.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no such currency exists.</exception>
    public async Task<string> CodeOfAsync(Guid currencyId, CancellationToken cancellationToken = default)
    {
        if (_codes.TryGetValue(currencyId, out var known))
        {
            return known;
        }

        var record = await client.RetrieveAsync(
            CurrencySchema.EntityName,
            currencyId,
            new ColumnSet(CurrencySchema.IsoCode),
            cancellationToken).ConfigureAwait(false);

        var code = record?.GetAttributeValue<string>(CurrencySchema.IsoCode)
                   ?? throw new InvalidOperationException($"Currency {currencyId} does not exist.");

        _codes[currencyId] = code;

        return code;
    }
}
