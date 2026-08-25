using DynamicsCrmLab.Domain.Common;
using DynamicsCrmLab.Domain.WorkOrders;
using FluentAssertions;
using Xunit;

namespace DynamicsCrmLab.Domain.Tests.WorkOrders;

public sealed class WorkOrderTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid EquipmentId = Guid.NewGuid();

    [Fact]
    public void Create_StartsInNewStatus()
    {
        var order = NewWorkOrder();

        order.Status.Should().Be(WorkOrderStatus.New);
        order.Number.Should().StartWith("WO-");
    }

    [Fact]
    public void Create_RejectsOrderWithoutCustomer()
    {
        var act = () => WorkOrder.Create(Guid.Empty, EquipmentId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RejectsOrderWithoutEquipment()
    {
        var act = () => WorkOrder.Create(CustomerId, Guid.Empty);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AssignTo_MovesTheOrderToAssigned()
    {
        var order = NewWorkOrder();
        var technicianId = Guid.NewGuid();

        order.AssignTo(technicianId);

        order.Status.Should().Be(WorkOrderStatus.Assigned);
        order.TechnicianId.Should().Be(technicianId);
    }

    [Fact]
    public void StartWork_IsRejectedWhenNoTechnicianIsAssigned()
    {
        var act = NewWorkOrder().StartWork;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddLine_RejectsNonPositiveQuantity()
    {
        var order = NewWorkOrder();

        var act = () => order.AddLine("Labour", 0, Money.Of(50m));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void TotalPrice_IsTheSumOfTheLines()
    {
        var order = NewWorkOrder();
        order.AddLine("Technician labour", 2, Money.Of(45m));
        order.AddLine("Filter", 1, Money.Of(30m));

        order.TotalPrice.Should().Be(Money.Of(120m));
    }

    [Fact]
    public void TotalPrice_IsZeroWithoutLines()
    {
        NewWorkOrder().TotalPrice.Should().Be(Money.Zero());
    }

    private static WorkOrder NewWorkOrder() => WorkOrder.Create(CustomerId, EquipmentId);
}
