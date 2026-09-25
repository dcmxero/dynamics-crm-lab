using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DynamicsCrmLab.Api.Tests;

public sealed class RepeatedRequestTests(WorkOrderApiFactoryFixture fixture)
    : IClassFixture<WorkOrderApiFactoryFixture>
{
    private readonly WorkOrderApiFactory _factory = fixture.Factory;
    private readonly HttpClient _client = fixture.Factory.CreateClientForCaller();

    [Fact]
    public async Task TheSameRequestSentTwice_RaisesOneJob()
    {
        // A client that times out and retries means one job, not two, and
        // nobody wants to explain a duplicate invoice.
        var body = Raise();

        var first = await Send(body, key: "retry-after-a-timeout");
        var second = await Send(body, key: "retry-after-a-timeout");

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        (await IdOf(first)).Should().Be(await IdOf(second));
    }

    [Fact]
    public async Task ARepeat_IsNotReportedAsSomethingNewlyCreated()
    {
        var body = Raise();

        await Send(body, key: "same-again");
        var repeat = await Send(body, key: "same-again");

        repeat.StatusCode.Should().Be(HttpStatusCode.OK);
        repeat.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task TwoDifferentRequests_RaiseTwoJobs()
    {
        var body = Raise();

        var first = await Send(body, key: "one");
        var second = await Send(body, key: "another");

        second.StatusCode.Should().Be(HttpStatusCode.Created);
        (await IdOf(first)).Should().NotBe(await IdOf(second));
    }

    [Fact]
    public async Task WithoutAKey_EveryRequestIsItsOwn()
    {
        // Saying nothing means this request is unlike any other, which is what
        // a caller who has not thought about it expects.
        var body = Raise();

        var first = await Send(body, key: null);
        var second = await Send(body, key: null);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        (await IdOf(first)).Should().NotBe(await IdOf(second));
    }

    private object Raise()
    {
        var customer = _factory.Store.AddCustomer();
        var equipment = _factory.Store.AddEquipment(customer.Id);

        return new
        {
            customerId = customer.Id,
            equipmentId = equipment.Id,
            lines = new[] { new { description = "Technician labour", quantity = 2, unitPrice = 45m } }
        };
    }

    private async Task<HttpResponseMessage> Send(object body, string? key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/work-orders")
        {
            Content = JsonContent.Create(body)
        };

        if (key is not null)
        {
            request.Headers.Add("Idempotency-Key", key);
        }

        return await _client.SendAsync(request);
    }

    private static async Task<string?> IdOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
}
