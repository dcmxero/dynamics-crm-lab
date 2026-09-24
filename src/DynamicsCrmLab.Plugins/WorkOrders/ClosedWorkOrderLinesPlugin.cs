using System;
using System.Collections.Generic;
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
/// An update that moves a charge between jobs concerns two of them, and either
/// one being closed is reason to refuse: the job it leaves ends up cheaper than
/// what was agreed, the job it arrives at dearer.
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

        foreach (var workOrderId in WorkOrderIdsOf(context))
        {
            Refuse(context, workOrderId);
        }
    }

    private void Refuse(PluginContext context, Guid workOrderId)
    {
        var job = context.Service.Retrieve(
            WorkOrderSchema.EntityName,
            workOrderId,
            new ColumnSet(WorkOrderSchema.Status, WorkOrderSchema.Number));

        var status = (WorkOrderStatus)job.GetAttributeValue<OptionSetValue>(WorkOrderSchema.Status).Value;

        if (status is not WorkOrderStatus.Closed)
        {
            return;
        }

        var number = job.GetAttributeValue<string>(WorkOrderSchema.Number) ?? workOrderId.ToString();

        context.Tracing.Trace("{0}: {1} is closed, refusing the change", PluginName, number);

        throw new DomainException($"Work order {number} is closed, so its charges can no longer be changed.");
    }

    /// <summary>
    /// Reads every job the change concerns.
    /// </summary>
    /// <remarks>
    /// Usually one. An update that repoints the lookup concerns two, and taking
    /// only the one the charge is moving to would let it be taken off a closed
    /// job unchallenged.
    /// </remarks>
    private static IEnumerable<Guid> WorkOrderIdsOf(PluginContext context)
    {
        var fromTarget = context.Target?.GetAttributeValue<EntityReference>(WorkOrderLineSchema.WorkOrder)?.Id;
        var fromImage = context.PreImage()?.GetAttributeValue<EntityReference>(WorkOrderLineSchema.WorkOrder)?.Id;

        if (fromTarget is { } target)
        {
            yield return target;
        }

        if (fromImage is { } image && image != fromTarget)
        {
            yield return image;
        }
    }
}
