using System;
using DynamicsCrmLab.Domain.Common;
using DynamicsCrmLab.Domain.WorkOrders;
using DynamicsCrmLab.Plugins.Infrastructure;
using DynamicsCrmLab.Schema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Plugins.WorkOrders;

/// <summary>
/// Refuses a charge added to, changed on, or taken off a closed job.
/// </summary>
/// <remarks>
/// Register on Create, Update and Delete of dcl_workorderline, stage
/// PreOperation (20), synchronous, with a pre image carrying dcl_workorderid.
///
/// The aggregate refuses to be modified once closed, but that only binds
/// whoever goes through the aggregate. A charge written straight to the line
/// table would change what a finished job costs after the customer was told,
/// and the pricing step would dutifully rewrite the total to match.
///
/// PreOperation because a refusal has to stop the write.
/// </remarks>
public sealed class ClosedWorkOrderLinesPlugin() : PluginBase(nameof(ClosedWorkOrderLinesPlugin))
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

        var workOrderId = WorkOrderIdOf(context);
        if (workOrderId is null)
        {
            return;
        }

        var job = context.Service.Retrieve(
            WorkOrderSchema.EntityName,
            workOrderId.Value,
            new ColumnSet(WorkOrderSchema.Status, WorkOrderSchema.Number));

        var status = (WorkOrderStatus)job.GetAttributeValue<OptionSetValue>(WorkOrderSchema.Status).Value;

        if (status is not WorkOrderStatus.Closed)
        {
            return;
        }

        var number = job.GetAttributeValue<string>(WorkOrderSchema.Number) ?? workOrderId.Value.ToString();

        context.Tracing.Trace("{0}: {1} is closed, refusing the change", PluginName, number);

        throw new DomainException($"Work order {number} is closed, so its charges can no longer be changed.");
    }

    private static Guid? WorkOrderIdOf(PluginContext context)
    {
        var fromTarget = context.Target?.GetAttributeValue<EntityReference>(WorkOrderLineSchema.WorkOrder);
        var fromImage = context.PreImage()?.GetAttributeValue<EntityReference>(WorkOrderLineSchema.WorkOrder);

        return (fromTarget ?? fromImage)?.Id;
    }
}
