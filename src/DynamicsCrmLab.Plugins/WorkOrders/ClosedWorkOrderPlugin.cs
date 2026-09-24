using System;
using DynamicsCrmLab.Domain.Common;
using DynamicsCrmLab.Domain.WorkOrders;
using DynamicsCrmLab.Plugins.Infrastructure;
using DynamicsCrmLab.Schema;
using Microsoft.Xrm.Sdk;

namespace DynamicsCrmLab.Plugins.WorkOrders;

/// <summary>
/// Refuses any change to a job that is already finished.
/// </summary>
/// <remarks>
/// Register on Update of dcl_workorder with no filtering attributes, stage
/// PreOperation (20), synchronous, with a pre image carrying dcl_status and
/// dcl_number.
///
/// Closing a job is a statement to the customer about what was done and what it
/// cost. The stage guard alone does not protect that statement: it is asked
/// only about the stage, so an update that leaves the stage at closed and
/// empties the resolution is not a stage change and passes untouched. The
/// record has to be closed to everything, not only to going backwards.
///
/// No filtering attributes on purpose. A column added later is covered without
/// anybody remembering to come back here.
///
/// PreOperation because a refusal has to stop the write.
/// </remarks>
public sealed class ClosedWorkOrderPlugin() : PluginBase(nameof(ClosedWorkOrderPlugin))
{
    /// <inheritdoc/>
    protected override void Execute(PluginContext context)
    {
        var target = context.Target;

        if (target is null || !string.Equals(target.LogicalName, WorkOrderSchema.EntityName, StringComparison.Ordinal))
        {
            return;
        }

        var preImage = context.PreImage();

        if (preImage?.GetAttributeValue<OptionSetValue>(WorkOrderSchema.Status) is not { } was)
        {
            context.Tracing.Trace("{0}: no pre image, the stage it came from is unknown", PluginName);

            return;
        }

        if ((WorkOrderStatus)was.Value is not WorkOrderStatus.Closed)
        {
            return;
        }

        var number = preImage.GetAttributeValue<string>(WorkOrderSchema.Number) ?? target.Id.ToString();

        context.Tracing.Trace("{0}: {1} is closed, refusing the change", PluginName, number);

        throw new DomainException($"Work order {number} is closed, so it can no longer be changed.");
    }
}
