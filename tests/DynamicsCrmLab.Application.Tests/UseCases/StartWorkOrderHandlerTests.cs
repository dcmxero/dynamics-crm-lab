using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Application.Tests.Fakes;
using DynamicsCrmLab.Application.UseCases.StartWorkOrder;
using DynamicsCrmLab.Domain.WorkOrders;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DynamicsCrmLab.Application.Tests.UseCases;

public sealed class StartWorkOrderHandlerTests
{
    private readonly InMemoryWorkOrderRepository _workOrders = new();
    private readonly StartWorkOrderHandler _handler;

    public StartWorkOrderHandlerTests() =>
        _handler = new StartWorkOrderHandler(_workOrders, NullLogger<StartWorkOrderHandler>.Instance);

    [Fact]
    public async Task HandleAsync_MovesAnAssignedJobToInProgress()
    {
        var workOrder = WorkOrder.Create(Guid.NewGuid(), Guid.NewGuid());
        workOrder.AssignTo(Guid.NewGuid());
        await _workOrders.AddAsync(workOrder, "test");

        var result = await _handler.HandleAsync(new StartWorkOrderCommand(workOrder.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(WorkOrderStatus.InProgress);
        workOrder.Status.Should().Be(WorkOrderStatus.InProgress);
    }

    [Fact]
    public async Task HandleAsync_RefusesAJobNobodyIsOn()
    {
        var workOrder = WorkOrder.Create(Guid.NewGuid(), Guid.NewGuid());
        await _workOrders.AddAsync(workOrder, "test");

        var result = await _handler.HandleAsync(new StartWorkOrderCommand(workOrder.Id));

        result.IsSuccess.Should().BeFalse();
        result.Failure.Should().Be(ResultFailure.RuleBroken);
    }

    [Fact]
    public async Task HandleAsync_ReportsAJobThatIsNotThereAsMissing()
    {
        var result = await _handler.HandleAsync(new StartWorkOrderCommand(Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Failure.Should().Be(ResultFailure.NotFound);
    }
}
