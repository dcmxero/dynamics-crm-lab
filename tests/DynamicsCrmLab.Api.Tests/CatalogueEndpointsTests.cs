using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DynamicsCrmLab.Api.Tests;

public sealed class CatalogueEndpointsTests(WorkOrderApiFactoryFixture fixture)
    : IClassFixture<WorkOrderApiFactoryFixture>
{
    private readonly WorkOrderApiFactory _factory = fixture.Factory;
    private readonly HttpClient _client = fixture.Factory.CreateClientForCaller();

    [Fact]
    public async Task Customers_AreFoundByWhatHasBeenTypedSoFar()
    {
        _factory.Store.AddCustomer("Northwind Catering");

        var found = await Read("/api/customers?name=North");

        found.EnumerateArray().Select(c => c.GetProperty("name").GetString())
            .Should().Contain("Northwind Catering");
    }

    [Fact]
    public async Task Customers_LeaveOutTheOnesThatDoNotMatch()
    {
        _factory.Store.AddCustomer("Southgate Hotels");

        var found = await Read("/api/customers?name=Zzz");

        found.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Customers_AreListedWhenNothingHasBeenTypedYet()
    {
        _factory.Store.AddCustomer();

        var found = await Read("/api/customers");

        found.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Equipment_IsOnlyTheEquipmentOfThatCustomer()
    {
        // Raising a job against somebody else's unit is not a job anybody can
        // do, so offering it would be offering a mistake.
        var mine = _factory.Store.AddCustomer("Mine");
        var theirs = _factory.Store.AddCustomer("Theirs");

        _factory.Store.AddEquipment(mine.Id, "SN-MINE");
        _factory.Store.AddEquipment(theirs.Id, "SN-THEIRS");

        var found = await Read($"/api/customers/{mine.Id}/equipment");

        found.EnumerateArray().Select(e => e.GetProperty("serialNumber").GetString())
            .Should().ContainSingle().Which.Should().Be("SN-MINE");
    }

    [Fact]
    public async Task Equipment_IsEmptyForACustomerWithNothingRegistered()
    {
        var customer = _factory.Store.AddCustomer("Nothing Registered");

        var response = await _client.GetAsync($"/api/customers/{customer.Id}/equipment");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Equipment_ReportsACustomerThatDoesNotExistAsMissing()
    {
        // Different from having nothing registered, and the caller choosing a
        // unit needs to know which of the two they got.
        var response = await _client.GetAsync($"/api/customers/{Guid.NewGuid()}/equipment");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Catalogue_IsClosedToAnAnonymousCaller()
    {
        var response = await _factory.CreateClient().GetAsync("/api/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<JsonElement> Read(string route)
    {
        var response = await _client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
