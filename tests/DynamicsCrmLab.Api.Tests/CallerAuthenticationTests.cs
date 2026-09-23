using System.Net;
using FluentAssertions;
using Xunit;

namespace DynamicsCrmLab.Api.Tests;

public sealed class CallerAuthenticationTests(WorkOrderApiFactoryFixture fixture)
    : IClassFixture<WorkOrderApiFactoryFixture>
{
    private readonly WorkOrderApiFactory _factory = fixture.Factory;

    [Fact]
    public async Task Listing_IsRefusedWithoutAToken()
    {
        var response = await _factory.CreateClient().GetAsync("/api/work-orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Listing_IsRefusedForATokenThatDoesNotCarryTheScope()
    {
        // A token for some other application is still a token. It is not consent
        // to work with work orders, and the API has to tell the two apart.
        var client = _factory.CreateClientForCaller(scope: "profile");

        var response = await client.GetAsync("/api/work-orders");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Listing_IsAllowedForATokenThatCarriesTheScope()
    {
        var response = await _factory.CreateClientForCaller().GetAsync("/api/work-orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Raising_IsRefusedWithoutAToken()
    {
        // The write routes are not a separate arrangement: the whole group is
        // closed, so a route added later is closed as well.
        var response = await _factory.CreateClient().PostAsync("/api/work-orders", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
