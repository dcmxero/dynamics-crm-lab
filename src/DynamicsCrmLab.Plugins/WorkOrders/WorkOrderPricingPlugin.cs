using System;
using System.Collections.Generic;
using System.Linq;
using DynamicsCrmLab.Domain.WorkOrders;
using DynamicsCrmLab.Plugins.Infrastructure;
using DynamicsCrmLab.Schema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DomainMoney = DynamicsCrmLab.Domain.Common.Money;
using XrmMoney = Microsoft.Xrm.Sdk.Money;

namespace DynamicsCrmLab.Plugins.WorkOrders;

/// <summary>
/// Keeps the amount to invoice on a work order in step with its lines.
/// </summary>
/// <remarks>
/// Register on Create and Update of dcl_workorder, stage PreOperation (20),
/// synchronous.
///
/// PreOperation is the point of it: the value is written onto the target and
/// the platform saves it along with the rest of the record. Doing the same work
/// in PostOperation would mean a second update, a second pass through the
/// pipeline, and a plug-in that can retrigger itself.
///
/// The total is worked out by the shared domain rather than by arithmetic
/// repeated here, so the console application and the platform can never
/// disagree about what a job costs.
/// </remarks>
public sealed class WorkOrderPricingPlugin() : PluginBase(nameof(WorkOrderPricingPlugin))
{
    /// <inheritdoc/>
    protected override void Execute(PluginContext context)
    {
        var target = context.Target;

        if (target is null || !string.Equals(target.LogicalName, WorkOrderSchema.EntityName, StringComparison.Ordinal))
        {
            return;
        }

        if (context.IsNested)
        {
            context.Tracing.Trace("{0}: nested run, leaving the total alone", PluginName);

            return;
        }

        var lines = ReadLines(context, target.Id);
        var total = TotalOf(lines);

        context.Tracing.Trace("{0}: {1} line(s), total {2}", PluginName, lines.Count, total);

        target[WorkOrderSchema.TotalPrice] = new XrmMoney(total.Amount);
    }

    private static DomainMoney TotalOf(IReadOnlyCollection<WorkOrderLine> lines)
    {
        var workOrder = WorkOrder.Restore(
            Guid.NewGuid(),
            "pricing",
            Guid.NewGuid(),
            Guid.NewGuid(),
            technicianId: null,
            WorkOrderStatus.New,
            resolution: null,
            lines);

        return workOrder.TotalPrice;
    }

    private static List<WorkOrderLine> ReadLines(PluginContext context, Guid workOrderId)
    {
        var query = new QueryExpression(WorkOrderLineSchema.EntityName)
        {
            ColumnSet = new ColumnSet(
                WorkOrderLineSchema.Description,
                WorkOrderLineSchema.Quantity,
                WorkOrderLineSchema.UnitPrice),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression(
                        WorkOrderLineSchema.WorkOrder,
                        ConditionOperator.Equal,
                        workOrderId)
                }
            }
        };

        return context.Service
            .RetrieveMultiple(query)
            .Entities
            .Select(record => new WorkOrderLine(
                record.Id,
                record.GetAttributeValue<string>(WorkOrderLineSchema.Description) ?? "(no description)",
                record.GetAttributeValue<int>(WorkOrderLineSchema.Quantity),
                DomainMoney.Of(record.GetAttributeValue<XrmMoney>(WorkOrderLineSchema.UnitPrice)?.Value ?? 0m)))
            .ToList();
    }
}
