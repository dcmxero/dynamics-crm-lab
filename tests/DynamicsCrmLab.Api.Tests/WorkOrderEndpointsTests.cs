using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DynamicsCrmLab.Domain.Common;
using DynamicsCrmLab.Domain.WorkOrders;
using FluentAssertions;
using Xunit;

namespace DynamicsCrmLab.Api.Tests;

public sealed class WorkOrderEndpointsTests(WorkOrderApiFactoryFixture fixture)
    : IClassFixture<WorkOrderApiFactoryFixture>
{
    private readonly WorkOrderApiFactory _factory = fixture.Factory;
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    [Fact]
    public async Task Raise_ReturnsCreatedWithTheNewJob()
    {
        var customer = _factory.Store.AddCustomer();
        var equipment = _factory.Store.AddEquipment(customer.Id);

        var response = await _client.PostAsJsonAsync("/api/work-orders", new
        {
            customerId = customer.Id,
            equipmentId = equipment.Id,
            lines = new[] { new { description = "Technician labour", quantity = 2, unitPrice = 45m } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("number").GetString().Should().StartWith("WO-");
        body.GetProperty("totalPrice").GetDecimal().Should().Be(90m);
        body.GetProperty("currency").GetString().Should().Be("EUR");
    }

    [Fact]
    public async Task Raise_ReturnsUnprocessableEntityWhenARuleIsBroken()
    {
        var customer = _factory.Store.AddCustomer();
        var equipment = _factory.Store.AddEquipment(customer.Id);

        var response = await _client.PostAsJsonAsync("/api/work-orders", new
        {
            customerId = customer.Id,
            equipmentId = equipment.Id,
            lines = new[] { new { description = "Technician labour", quantity = 0, unitPrice = 45m } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("detail").GetString().Should().Contain("quantity");
    }

    [Fact]
    public async Task Get_ReturnsNotFoundForAnUnknownJob()
    {
        var response = await _client.GetAsync($"/api/work-orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(404);
    }

    [Fact]
    public async Task Get_ReturnsTheJobWithItsLines()
    {
        var workOrder = StoredWorkOrder();

        var response = await _client.GetAsync($"/api/work-orders/{workOrder.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("New");
        body.GetProperty("lines").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task List_ReturnsJobsInTheRequestedStage()
    {
        StoredWorkOrder();

        var response = await _client.GetAsync("/api/work-orders?status=New&take=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task List_RejectsAStatusItDoesNotKnow()
    {
        var response = await _client.GetAsync("/api/work-orders?status=Elsewhere");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Close_RefusesAJobThatHasNotStarted()
    {
        var workOrder = StoredWorkOrder();

        var response = await _client.PostAsJsonAsync(
            $"/api/work-orders/{workOrder.Id}/closure",
            new { resolution = "Replaced the filter." });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Assign_PutsAFreeTechnicianOnTheJob()
    {
        var workOrder = StoredWorkOrder();
        _factory.Store.AddTechnician();

        var response = await _client.PostAsJsonAsync(
            $"/api/work-orders/{workOrder.Id}/assignment",
            new { technicianId = (Guid?)null });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        workOrder.Status.Should().Be(WorkOrderStatus.Assigned);
    }

    [Fact]
    public async Task Raise_ReturnsNotFoundWhenTheCustomerIsUnknown()
    {
        var response = await _client.PostAsJsonAsync("/api/work-orders", new
        {
            customerId = Guid.NewGuid(),
            equipmentId = Guid.NewGuid(),
            lines = new[] { new { description = "Technician labour", quantity = 1, unitPrice = 45m } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Assign_ReturnsNotFoundWhenTheTechnicianIsUnknown()
    {
        var workOrder = StoredWorkOrder();

        var response = await _client.PostAsJsonAsync(
            $"/api/work-orders/{workOrder.Id}/assignment",
            new { technicianId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Assign_ReturnsUnprocessableEntityWhenTheTechnicianIsBusy()
    {
        var workOrder = StoredWorkOrder();
        var busy = _factory.Store.AddTechnician(isAvailable: false);

        var response = await _client.PostAsJsonAsync(
            $"/api/work-orders/{workOrder.Id}/assignment",
            new { technicianId = busy.Id });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private WorkOrder StoredWorkOrder()
    {
        var customer = _factory.Store.AddCustomer();
        var equipment = _factory.Store.AddEquipment(customer.Id);

        var workOrder = WorkOrder.Create(customer.Id, equipment.Id);
        workOrder.AddLine("Technician labour", 2, Money.Of(45m));

        return _factory.Store.AddWorkOrder(workOrder);
    }
}

/// <summary>
/// Keeps one host alive for the whole test class rather than starting it per test.
/// </summary>
public sealed class WorkOrderApiFactoryFixture : IDisposable
{
    internal WorkOrderApiFactory Factory { get; } = new();

    public void Dispose() => Factory.Dispose();
}
