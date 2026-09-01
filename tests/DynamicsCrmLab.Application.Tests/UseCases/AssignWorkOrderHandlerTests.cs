using DynamicsCrmLab.Application.Tests.Fakes;
using DynamicsCrmLab.Application.UseCases.AssignWorkOrder;
using DynamicsCrmLab.Domain.WorkOrders;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DynamicsCrmLab.Application.Tests.UseCases;

public sealed class AssignWorkOrderHandlerTests
{
    private readonly InMemoryWorkOrderRepository _workOrders = new();
    private readonly InMemoryTechnicianRepository _technicians = new();
    private readonly AssignWorkOrderHandler _handler;

    public AssignWorkOrderHandlerTests() =>
        _handler = new AssignWorkOrderHandler(
            _workOrders,
            _technicians,
            NullLogger<AssignWorkOrderHandler>.Instance);

    [Fact]
    public async Task HandleAsync_PicksAnAvailableTechnicianWhenNoneIsNamed()
    {
        var workOrder = await StoredWorkOrderAsync();
        var technician = _technicians.Add();

        var result = await _handler.HandleAsync(new AssignWorkOrderCommand(workOrder.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value!.TechnicianId.Should().Be(technician.Id);
        workOrder.Status.Should().Be(WorkOrderStatus.Assigned);
    }

    [Fact]
    public async Task HandleAsync_AssignsTheNamedTechnician()
    {
        var workOrder = await StoredWorkOrderAsync();
        _technicians.Add("Ivan Hruska");
        var wanted = _technicians.Add("Zuzana Bielikova");

        var result = await _handler.HandleAsync(new AssignWorkOrderCommand(workOrder.Id, wanted.Id));

        result.Value!.TechnicianName.Should().Be("Zuzana Bielikova");
    }

    [Fact]
    public async Task HandleAsync_FailsWhenNobodyIsAvailable()
    {
        var workOrder = await StoredWorkOrderAsync();
        _technicians.Add(isAvailable: false);

        var result = await _handler.HandleAsync(new AssignWorkOrderCommand(workOrder.Id));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("No technician is available");
    }

    [Fact]
    public async Task HandleAsync_FailsWhenTheNamedTechnicianIsBusy()
    {
        var workOrder = await StoredWorkOrderAsync();
        var busy = _technicians.Add(isAvailable: false);

        var result = await _handler.HandleAsync(new AssignWorkOrderCommand(workOrder.Id, busy.Id));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_FailsWhenTheWorkOrderIsUnknown()
    {
        var result = await _handler.HandleAsync(new AssignWorkOrderCommand(Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("does not exist");
    }

    private async Task<WorkOrder> StoredWorkOrderAsync()
    {
        var workOrder = WorkOrder.Create(Guid.NewGuid(), Guid.NewGuid());
        await _workOrders.AddAsync(workOrder);
        return workOrder;
    }
}
