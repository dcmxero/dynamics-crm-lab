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

    [Fact]
    public void Close_RequiresAResolution()
    {
        var order = InProgressWorkOrder();

        var act = () => order.Close("   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Close_IsRejectedBeforeTheWorkHasStarted()
    {
        var order = NewWorkOrder();

        var act = () => order.Close("Replaced the filter.");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Close_ClosesAnInProgressOrder()
    {
        var order = InProgressWorkOrder();

        order.Close("Replaced the filter.");

        order.Status.Should().Be(WorkOrderStatus.Closed);
        order.Resolution.Should().Be("Replaced the filter.");
    }

    [Fact]
    public void AddLine_IsRejectedOnAClosedOrder()
    {
        var order = ClosedWorkOrder();

        var act = () => order.AddLine("Labour", 1, Money.Of(50m));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AssignTo_IsRejectedOnAClosedOrder()
    {
        var order = ClosedWorkOrder();

        var act = () => order.AssignTo(Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    private static WorkOrder NewWorkOrder() => WorkOrder.Create(CustomerId, EquipmentId);

    private static WorkOrder InProgressWorkOrder()
    {
        var order = NewWorkOrder();
        order.AssignTo(Guid.NewGuid());
        order.StartWork();
        return order;
    }

    private static WorkOrder ClosedWorkOrder()
    {
        var order = InProgressWorkOrder();
        order.Close("Done.");
        return order;
    }
}
