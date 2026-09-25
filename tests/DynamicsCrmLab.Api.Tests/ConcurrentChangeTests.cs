using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DynamicsCrmLab.Domain.Common;
using DynamicsCrmLab.Domain.WorkOrders;
using FluentAssertions;
using Xunit;

namespace DynamicsCrmLab.Api.Tests;

public sealed class ConcurrentChangeTests(WorkOrderApiFactoryFixture fixture)
    : IClassFixture<WorkOrderApiFactoryFixture>
{
    private readonly WorkOrderApiFactory _factory = fixture.Factory;
    private readonly HttpClient _client = fixture.Factory.CreateClientForCaller();

    [Fact]
    public async Task Assigning_ReportsAConflictWhenSomebodyElseGotThereFirst()
    {
        // Nothing is wrong with the request. It arrived after somebody else
        // moved the same job on, and a blind write would undo their work.
        var job = Stored();
        var technician = _factory.Store.AddTechnician();

        _factory.Store.ChangedByEveryoneElse(job.Id);

        var response = await _client.PostAsJsonAsync(
            $"/api/work-orders/{job.Id}/assignment",
            new { technicianId = technician.Id });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Conflict_SaysWhatToDoAboutIt()
    {
        var job = Stored();
        _factory.Store.AddTechnician();
        _factory.Store.ChangedByEveryoneElse(job.Id);

        var response = await _client.PostAsJsonAsync(
            $"/api/work-orders/{job.Id}/assignment",
            new { technicianId = (Guid?)null });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("detail").GetString().Should().Contain("Read it again");
    }

    [Fact]
    public async Task Closing_ReportsAConflictRatherThanAFault()
    {
        var job = Stored();
        var technician = _factory.Store.AddTechnician();

        job.AssignTo(technician.Id);
        job.StartWork();

        _factory.Store.ChangedByEveryoneElse(job.Id);

        var response = await _client.PostAsJsonAsync(
            $"/api/work-orders/{job.Id}/closure",
            new { resolution = "Replaced the compressor seal." });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ARequestThatLostNoRaceStillSucceeds()
    {
        var job = Stored();
        var technician = _factory.Store.AddTechnician();

        var response = await _client.PostAsJsonAsync(
            $"/api/work-orders/{job.Id}/assignment",
            new { technicianId = technician.Id });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private WorkOrder Stored()
    {
        var customer = _factory.Store.AddCustomer();
        var equipment = _factory.Store.AddEquipment(customer.Id);

        var workOrder = WorkOrder.Create(customer.Id, equipment.Id);
        workOrder.AddLine("Technician labour", 2, Money.Of(45m));

        return _factory.Store.AddWorkOrder(workOrder);
    }
}
