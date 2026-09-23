using DynamicsCrmLab.Infrastructure.Dataverse;
using DynamicsCrmLab.Schema;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DynamicsCrmLab.Infrastructure.Tests.Dataverse;

public sealed class DataverseCurrenciesTests
{
    private static readonly Guid Euro = Guid.NewGuid();

    [Fact]
    public async Task CodeOf_ReadsTheCurrencyOnlyOnce()
    {
        var client = ClientReporting("EUR");
        var currencies = new DataverseCurrencies(client, new CurrencyCache());

        await currencies.CodeOfAsync(Euro);
        await currencies.CodeOfAsync(Euro);

        client.RetrieveCount.Should().Be(1);
    }

    [Fact]
    public async Task CodeOf_ReusesWhatAnotherCallerAlreadyRead()
    {
        // Each caller gets a connection of their own, so a cache the reader
        // owned would go with the request that filled it and the same row would
        // be read again for every caller.
        var cache = new CurrencyCache();

        var first = ClientReporting("EUR");
        await new DataverseCurrencies(first, cache).CodeOfAsync(Euro);

        var second = ClientReporting("EUR");
        var code = await new DataverseCurrencies(second, cache).CodeOfAsync(Euro);

        code.Should().Be("EUR");
        second.RetrieveCount.Should().Be(0);
    }

    private static FakeDataverseClient ClientReporting(string isoCode)
    {
        var client = new FakeDataverseClient();

        client.RetrieveResults[CurrencySchema.EntityName] = new Entity(CurrencySchema.EntityName, Euro)
        {
            [CurrencySchema.IsoCode] = isoCode
        };

        return client;
    }
}
