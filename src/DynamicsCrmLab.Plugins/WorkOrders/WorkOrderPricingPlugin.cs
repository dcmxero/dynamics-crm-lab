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
/// Register on Create, Update and Delete of dcl_workorderline, stage
/// PostOperation (40), synchronous, with a pre image carrying dcl_workorderid.
///
/// The line rather than the job is what triggers this. A job is saved before
/// its lines exist, so a step on the job would add up an empty list and write a
/// total of zero over a correct one. The lines are what the total is made of,
/// so a line changing is the event worth reacting to.
///
/// PostOperation because the row has to be written before it can be counted,
/// and the pre image because a deleted line carries nothing but its identifier
/// by the time the step runs.
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
        var target = context.TargetReference;

        if (target is null
            || !string.Equals(target.LogicalName, WorkOrderLineSchema.EntityName, StringComparison.Ordinal))
        {
            return;
        }

        if (context.IsNested)
        {
            context.Tracing.Trace("{0}: nested run, leaving the total alone", PluginName);

            return;
        }

        var workOrderIds = WorkOrderIdsOf(context);
        if (workOrderIds.Count == 0)
        {
            context.Tracing.Trace("{0}: the line belongs to no job, nothing to total", PluginName);

            return;
        }

        foreach (var workOrderId in workOrderIds)
        {
            Retotal(context, workOrderId);
        }
    }

    private void Retotal(PluginContext context, Guid workOrderId)
    {
        var lines = ReadLines(context, workOrderId);
        var total = TotalOf(lines);

        context.Tracing.Trace(
            "{0}: job {1}, {2} line(s), total {3}",
            PluginName,
            workOrderId,
            lines.Count,
            total);

        context.Service.Update(new Entity(WorkOrderSchema.EntityName, workOrderId)
        {
            [WorkOrderSchema.TotalPrice] = new XrmMoney(total.Amount)
        });
    }

    /// <summary>
    /// Finds the jobs whose total the change affects.
    /// </summary>
    /// <remarks>
    /// An update carries only the columns that changed and a delete carries no
    /// columns at all, so the pre image is what answers this in both cases.
    ///
    /// Usually that is one job. Moving a line to another job is the exception:
    /// the target carries where it went and the image where it came from, and
    /// both totals have changed. Taking only one of them would leave the job it
    /// left still charging for it.
    /// </remarks>
    private static List<Guid> WorkOrderIdsOf(PluginContext context)
    {
        var fromTarget = context.Target?.GetAttributeValue<EntityReference>(WorkOrderLineSchema.WorkOrder);
        var fromImage = context.PreImage()?.GetAttributeValue<EntityReference>(WorkOrderLineSchema.WorkOrder);

        var affected = new List<Guid>();

        foreach (var reference in new[] { fromTarget, fromImage })
        {
            if (reference != null && !affected.Contains(reference.Id))
            {
                affected.Add(reference.Id);
            }
        }

        return affected;
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
