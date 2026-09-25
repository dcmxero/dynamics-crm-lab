using DynamicsCrmLab.Application.Tests.Fakes;
using DynamicsCrmLab.Application.UseCases.CloseWorkOrder;
using DynamicsCrmLab.Domain.Common;
using DynamicsCrmLab.Domain.WorkOrders;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DynamicsCrmLab.Application.Tests.UseCases;

public sealed class CloseWorkOrderHandlerTests
{
    private readonly InMemoryWorkOrderRepository _workOrders = new();
    private readonly CloseWorkOrderHandler _handler;

    public CloseWorkOrderHandlerTests() =>
        _handler = new CloseWorkOrderHandler(_workOrders, NullLogger<CloseWorkOrderHandler>.Instance);

    [Fact]
    public async Task HandleAsync_ClosesAnInProgressWorkOrder()
    {
        var workOrder = await StoredWorkOrderAsync(startWork: true);

        var result = await _handler.HandleAsync(
            new CloseWorkOrderCommand(workOrder.Id, "Replaced the compressor seal."));

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalPrice.Should().Be(Money.Of(90m));
        workOrder.Status.Should().Be(WorkOrderStatus.Closed);
    }

    [Fact]
    public async Task HandleAsync_FailsWithoutAResolution()
    {
        var workOrder = await StoredWorkOrderAsync(startWork: true);

        var result = await _handler.HandleAsync(new CloseWorkOrderCommand(workOrder.Id, "  "));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("resolution");
    }

    [Fact]
    public async Task HandleAsync_FailsWhenTheWorkHasNotStarted()
    {
        var workOrder = await StoredWorkOrderAsync(startWork: false);

        var result = await _handler.HandleAsync(new CloseWorkOrderCommand(workOrder.Id, "Done."));

        result.IsSuccess.Should().BeFalse();
        workOrder.Status.Should().NotBe(WorkOrderStatus.Closed);
    }

    [Fact]
    public async Task HandleAsync_FailsWhenTheWorkOrderIsUnknown()
    {
        var result = await _handler.HandleAsync(new CloseWorkOrderCommand(Guid.NewGuid(), "Done."));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("does not exist");
    }

    private async Task<WorkOrder> StoredWorkOrderAsync(bool startWork)
    {
        var workOrder = WorkOrder.Create(Guid.NewGuid(), Guid.NewGuid());
        workOrder.AddLine("Technician labour", 2, Money.Of(45m));

        if (startWork)
        {
            workOrder.AssignTo(Guid.NewGuid());
            workOrder.StartWork();
        }

        await _workOrders.AddAsync(workOrder, "test");
        return workOrder;
    }
}
