using DynamicsCrmLab.Domain.WorkOrders;
using DynamicsCrmLab.Infrastructure.Dataverse.Mapping;
using DynamicsCrmLab.Infrastructure.Dataverse.Schema;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Xunit;
using DomainMoney = DynamicsCrmLab.Domain.Common.Money;
using XrmMoney = Microsoft.Xrm.Sdk.Money;

namespace DynamicsCrmLab.Infrastructure.Tests.Dataverse;

public sealed class WorkOrderMapperTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid EquipmentId = Guid.NewGuid();

    [Fact]
    public void ToDomain_RebuildsTheAggregateFromRows()
    {
        var workOrderId = Guid.NewGuid();
        var technicianId = Guid.NewGuid();

        var record = new Entity(WorkOrderSchema.EntityName, workOrderId)
        {
            [WorkOrderSchema.Number] = "WO-20260901-ABCDEF",
            [WorkOrderSchema.Customer] = new EntityReference(ContactSchema.EntityName, CustomerId),
            [WorkOrderSchema.Equipment] = new EntityReference(EquipmentSchema.EntityName, EquipmentId),
            [WorkOrderSchema.Technician] = new EntityReference(TechnicianSchema.EntityName, technicianId),
            [WorkOrderSchema.Status] = new OptionSetValue((int)WorkOrderStatus.InProgress),
            [WorkOrderSchema.Resolution] = null
        };

        var lineRecord = new Entity(WorkOrderLineSchema.EntityName, Guid.NewGuid())
        {
            [WorkOrderLineSchema.Description] = "Technician labour",
            [WorkOrderLineSchema.Quantity] = 2,
            [WorkOrderLineSchema.UnitPrice] = new XrmMoney(45m)
        };

        var workOrder = WorkOrderMapper.ToDomain(record, [lineRecord]);

        workOrder.Id.Should().Be(workOrderId);
        workOrder.Number.Should().Be("WO-20260901-ABCDEF");
        workOrder.CustomerId.Should().Be(CustomerId);
        workOrder.TechnicianId.Should().Be(technicianId);
        workOrder.Status.Should().Be(WorkOrderStatus.InProgress);
        workOrder.TotalPrice.Should().Be(DomainMoney.Of(90m));
    }

    [Fact]
    public void ToDomain_TreatsAnUnassignedJobAsHavingNoTechnician()
    {
        var record = new Entity(WorkOrderSchema.EntityName, Guid.NewGuid())
        {
            [WorkOrderSchema.Number] = "WO-20260901-000001",
            [WorkOrderSchema.Status] = new OptionSetValue((int)WorkOrderStatus.New)
        };

        var workOrder = WorkOrderMapper.ToDomain(record, []);

        workOrder.TechnicianId.Should().BeNull();
        workOrder.Lines.Should().BeEmpty();
    }

    [Fact]
    public void ToUpdateRecord_CarriesOnlyTheColumnsThatCanChange()
    {
        var workOrder = ClosedWorkOrder();

        var record = WorkOrderMapper.ToUpdateRecord(workOrder);

        record.Attributes.Keys.Should().BeEquivalentTo(
            [WorkOrderSchema.Status, WorkOrderSchema.Resolution, WorkOrderSchema.Technician]);
        record.Attributes.Should().NotContainKey(WorkOrderSchema.Number);
        record.Attributes.Should().NotContainKey(WorkOrderSchema.Customer);
    }

    [Fact]
    public void ToRecord_CarriesTheLookupsAndTheStartingStatus()
    {
        var workOrder = WorkOrder.Create(CustomerId, EquipmentId);

        var record = WorkOrderMapper.ToRecord(workOrder);

        record.GetAttributeValue<EntityReference>(WorkOrderSchema.Customer).Id.Should().Be(CustomerId);
        record.GetAttributeValue<EntityReference>(WorkOrderSchema.Equipment).Id.Should().Be(EquipmentId);
        record.GetAttributeValue<OptionSetValue>(WorkOrderSchema.Status).Value
            .Should().Be((int)WorkOrderStatus.New);
    }

    [Fact]
    public void ToLineRecord_PointsTheLineAtItsWorkOrder()
    {
        var workOrder = ClosedWorkOrder();
        var line = workOrder.Lines[0];

        var record = WorkOrderMapper.ToLineRecord(line, workOrder.Id);

        record.GetAttributeValue<EntityReference>(WorkOrderLineSchema.WorkOrder).Id.Should().Be(workOrder.Id);
        record.GetAttributeValue<XrmMoney>(WorkOrderLineSchema.UnitPrice).Value.Should().Be(45m);
    }

    private static WorkOrder ClosedWorkOrder()
    {
        var workOrder = WorkOrder.Create(CustomerId, EquipmentId);
        workOrder.AddLine("Technician labour", 2, DomainMoney.Of(45m));
        workOrder.AssignTo(Guid.NewGuid());
        workOrder.StartWork();
        workOrder.Close("Replaced the filter.");
        return workOrder;
    }
}
