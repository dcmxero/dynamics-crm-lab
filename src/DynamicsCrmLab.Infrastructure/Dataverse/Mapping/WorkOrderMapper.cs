using DynamicsCrmLab.Domain.WorkOrders;
using DynamicsCrmLab.Schema;
using Microsoft.Xrm.Sdk;

// Dataverse and the domain both define a Money type; the aliases say which is meant.
using DomainMoney = DynamicsCrmLab.Domain.Common.Money;
using XrmMoney = Microsoft.Xrm.Sdk.Money;

namespace DynamicsCrmLab.Infrastructure.Dataverse.Mapping;

/// <summary>
/// Translates between the work order aggregate and Dataverse rows.
/// </summary>
/// <remarks>
/// Kept apart from the repository: the repository decides how records are
/// fetched, the mapper decides what shape they take. Each has one reason to change.
/// </remarks>
internal static class WorkOrderMapper
{
    private const string DefaultCurrency = "EUR";

    /// <summary>
    /// Rebuilds the aggregate from a work order row and its line rows.
    /// </summary>
    /// <param name="record">The work order row.</param>
    /// <param name="lineRecords">The line rows belonging to it.</param>
    /// <returns>The restored work order.</returns>
    public static WorkOrder ToDomain(Entity record, IEnumerable<Entity> lineRecords)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(lineRecords);

        return WorkOrder.Restore(
            record.Id,
            record.GetAttributeValue<string>(WorkOrderSchema.Number) ?? string.Empty,
            record.GetAttributeValue<EntityReference>(WorkOrderSchema.Customer)?.Id ?? Guid.Empty,
            record.GetAttributeValue<EntityReference>(WorkOrderSchema.Equipment)?.Id ?? Guid.Empty,
            record.GetAttributeValue<EntityReference>(WorkOrderSchema.Technician)?.Id,
            (WorkOrderStatus)(record.GetAttributeValue<OptionSetValue>(WorkOrderSchema.Status)?.Value
                              ?? (int)WorkOrderStatus.New),
            record.GetAttributeValue<string>(WorkOrderSchema.Resolution),
            lineRecords.Select(ToDomainLine));
    }

    /// <summary>
    /// Builds the row written when a work order is first stored.
    /// </summary>
    /// <param name="workOrder">The work order to store.</param>
    /// <returns>A complete work order row.</returns>
    public static Entity ToRecord(WorkOrder workOrder)
    {
        ArgumentNullException.ThrowIfNull(workOrder);

        return new Entity(WorkOrderSchema.EntityName, workOrder.Id)
        {
            [WorkOrderSchema.Number] = workOrder.Number,
            [WorkOrderSchema.Customer] = new EntityReference(ContactSchema.EntityName, workOrder.CustomerId),
            [WorkOrderSchema.Equipment] = new EntityReference(EquipmentSchema.EntityName, workOrder.EquipmentId),
            [WorkOrderSchema.Status] = new OptionSetValue((int)workOrder.Status)
        };
    }

    /// <summary>
    /// Builds the row written when a stored work order changes.
    /// </summary>
    /// <remarks>
    /// Only the columns that can change after the job was raised are included.
    /// Dataverse decides which plug-ins to run from the columns in the request,
    /// so sending unchanged ones would trigger logic for nothing.
    /// </remarks>
    /// <param name="workOrder">The work order to write back.</param>
    /// <returns>A work order row holding only the mutable columns.</returns>
    public static Entity ToUpdateRecord(WorkOrder workOrder)
    {
        ArgumentNullException.ThrowIfNull(workOrder);

        var record = new Entity(WorkOrderSchema.EntityName, workOrder.Id)
        {
            [WorkOrderSchema.Status] = new OptionSetValue((int)workOrder.Status),
            [WorkOrderSchema.Resolution] = workOrder.Resolution
        };

        if (workOrder.TechnicianId is { } technicianId)
        {
            record[WorkOrderSchema.Technician] =
                new EntityReference(TechnicianSchema.EntityName, technicianId);
        }

        return record;
    }

    /// <summary>
    /// Builds the row for a single charge on a work order.
    /// </summary>
    /// <param name="line">The line to store.</param>
    /// <param name="workOrderId">The work order the line belongs to.</param>
    /// <returns>A work order line row.</returns>
    public static Entity ToLineRecord(WorkOrderLine line, Guid workOrderId)
    {
        ArgumentNullException.ThrowIfNull(line);

        return new Entity(WorkOrderLineSchema.EntityName, line.Id)
        {
            [WorkOrderLineSchema.WorkOrder] = new EntityReference(WorkOrderSchema.EntityName, workOrderId),
            [WorkOrderLineSchema.Description] = line.Description,
            [WorkOrderLineSchema.Quantity] = line.Quantity,
            [WorkOrderLineSchema.UnitPrice] = new XrmMoney(line.UnitPrice.Amount)
        };
    }

    private static WorkOrderLine ToDomainLine(Entity record) =>
        new(record.Id,
            record.GetAttributeValue<string>(WorkOrderLineSchema.Description) ?? "(no description)",
            record.GetAttributeValue<int>(WorkOrderLineSchema.Quantity),
            DomainMoney.Of(
                record.GetAttributeValue<XrmMoney>(WorkOrderLineSchema.UnitPrice)?.Value ?? 0m,
                DefaultCurrency));
}
