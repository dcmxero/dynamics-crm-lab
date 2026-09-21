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

    /// <summary>
    /// Rebuilds the aggregate from a work order row and its line rows.
    /// </summary>
    /// <param name="record">The work order row.</param>
    /// <param name="lineRecords">The line rows belonging to it.</param>
    /// <param name="currency">The currency the rows hold their money in.</param>
    /// <returns>The restored work order.</returns>
    public static WorkOrder ToDomain(Entity record, IEnumerable<Entity> lineRecords, string currency)
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
            lineRecords.Select(line => ToDomainLine(line, currency)));
    }

    /// <summary>
    /// Builds the row written when a work order is first stored.
    /// </summary>
    /// <param name="workOrder">The work order to store.</param>
    /// <param name="currencyId">The currency the money on the job is held in.</param>
    /// <returns>A complete work order row.</returns>
    public static Entity ToRecord(WorkOrder workOrder, Guid currencyId)
    {
        ArgumentNullException.ThrowIfNull(workOrder);

        return new Entity(WorkOrderSchema.EntityName, workOrder.Id)
        {
            [WorkOrderSchema.Number] = workOrder.Number,
            [WorkOrderSchema.Customer] = new EntityReference(ContactSchema.EntityName, workOrder.CustomerId),
            [WorkOrderSchema.Equipment] = new EntityReference(EquipmentSchema.EntityName, workOrder.EquipmentId),
            [WorkOrderSchema.Status] = new OptionSetValue((int)workOrder.Status),

            // Said rather than left to the platform, so that what the API
            // reports back is what was actually stored.
            [WorkOrderSchema.Currency] = new EntityReference(CurrencySchema.EntityName, currencyId)
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

        // Written even when there is nobody on the job: leaving it out would
        // mean a job could be assigned but never unassigned.
        record[WorkOrderSchema.Technician] = workOrder.TechnicianId is { } technicianId
            ? new EntityReference(TechnicianSchema.EntityName, technicianId)
            : null;

        return record;
    }

    /// <summary>
    /// Builds the row for a single charge on a work order.
    /// </summary>
    /// <param name="line">The line to store.</param>
    /// <param name="workOrderId">The work order the line belongs to.</param>
    /// <param name="currencyId">The currency the price is held in.</param>
    /// <returns>A work order line row.</returns>
    public static Entity ToLineRecord(WorkOrderLine line, Guid workOrderId, Guid currencyId)
    {
        ArgumentNullException.ThrowIfNull(line);

        return new Entity(WorkOrderLineSchema.EntityName, line.Id)
        {
            [WorkOrderLineSchema.WorkOrder] = new EntityReference(WorkOrderSchema.EntityName, workOrderId),
            [WorkOrderLineSchema.Description] = line.Description,
            [WorkOrderLineSchema.Quantity] = line.Quantity,
            [WorkOrderLineSchema.UnitPrice] = new XrmMoney(line.UnitPrice.Amount),
            [WorkOrderLineSchema.Currency] = new EntityReference(CurrencySchema.EntityName, currencyId)
        };
    }

    private static WorkOrderLine ToDomainLine(Entity record, string currency) =>
        new(record.Id,
            record.GetAttributeValue<string>(WorkOrderLineSchema.Description) ?? "(no description)",
            record.GetAttributeValue<int>(WorkOrderLineSchema.Quantity),
            DomainMoney.Of(
                record.GetAttributeValue<XrmMoney>(WorkOrderLineSchema.UnitPrice)?.Value ?? 0m,
                currency));
}
