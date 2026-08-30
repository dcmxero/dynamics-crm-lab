using DynamicsCrmLab.Application.Tests.Fakes;
using DynamicsCrmLab.Application.UseCases.RaiseWorkOrder;
using DynamicsCrmLab.Domain.Common;
using DynamicsCrmLab.Domain.WorkOrders;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DynamicsCrmLab.Application.Tests.UseCases;

public sealed class RaiseWorkOrderHandlerTests
{
    private readonly InMemoryWorkOrderRepository _workOrders = new();
    private readonly InMemoryCustomerRepository _customers = new();
    private readonly InMemoryEquipmentRepository _equipment = new();
    private readonly RaiseWorkOrderHandler _handler;

    public RaiseWorkOrderHandlerTests() =>
        _handler = new RaiseWorkOrderHandler(
            _workOrders,
            _customers,
            _equipment,
            NullLogger<RaiseWorkOrderHandler>.Instance);

    [Fact]
    public async Task HandleAsync_StoresTheWorkOrderWithItsLines()
    {
        var customer = _customers.Add();
        var unit = _equipment.Add(customer.Id);

        var result = await _handler.HandleAsync(new RaiseWorkOrderCommand(
            customer.Id,
            unit.Id,
            [new WorkOrderLineInput("Technician labour", 2, 45m), new WorkOrderLineInput("Filter", 1, 30m)]));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(WorkOrderStatus.New);
        result.Value.TotalPrice.Should().Be(Money.Of(120m));
        _workOrders.Stored.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_FailsWhenTheCustomerIsUnknown()
    {
        var result = await _handler.HandleAsync(
            new RaiseWorkOrderCommand(Guid.NewGuid(), Guid.NewGuid(), []));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("does not exist");
        _workOrders.Stored.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_FailsWhenTheEquipmentIsUnknown()
    {
        var customer = _customers.Add();

        var result = await _handler.HandleAsync(
            new RaiseWorkOrderCommand(customer.Id, Guid.NewGuid(), []));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Equipment");
        _workOrders.Stored.Should().BeEmpty();
    }
}
