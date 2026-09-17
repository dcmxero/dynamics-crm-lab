using System;
using DynamicsCrmLab.Domain.WorkOrders;
using DynamicsCrmLab.Plugins.Infrastructure;
using Microsoft.Xrm.Sdk;

namespace DynamicsCrmLab.Plugins.WorkOrders;

/// <summary>
/// Raises a follow-up task once a work order has been closed.
/// </summary>
/// <remarks>
/// Register on Update of dcl_workorder, filtering attribute dcl_status only,
/// stage PostOperation (40), <b>asynchronous</b>, with a pre image carrying
/// dcl_status and dcl_number.
///
/// Asynchronous on purpose: nobody saving the form needs to wait for a
/// follow-up task to exist. Synchronous is for work that has to block the
/// transaction or that the user must see immediately.
/// </remarks>
public sealed class WorkOrderClosedNotificationPlugin()
    : PluginBase(nameof(WorkOrderClosedNotificationPlugin))
{
    /// <inheritdoc/>
    protected override void Execute(PluginContext context)
    {
        var target = context.Target;

        if (target is null || !string.Equals(target.LogicalName, WorkOrderColumns.EntityName, StringComparison.Ordinal))
        {
            return;
        }

        var status = target.GetAttributeValue<OptionSetValue>(WorkOrderColumns.Status);

        // The filtering attribute should already have kept other changes out,
        // but a registration can be edited and this costs nothing to confirm.
        if (status is null || status.Value != (int)WorkOrderStatus.Closed)
        {
            return;
        }

        // The pre image already carries the previous state, so there is no
        // reason to read the record back out of the platform.
        var preImage = context.PreImage();
        var previousStatus = preImage?.GetAttributeValue<OptionSetValue>(WorkOrderColumns.Status)?.Value;

        if (previousStatus == status.Value)
        {
            context.Tracing.Trace("{0}: status unchanged, no task raised", PluginName);

            return;
        }

        var number = preImage?.GetAttributeValue<string>(WorkOrderColumns.Number) ?? target.Id.ToString();

        context.Service.Create(new Entity("task")
        {
            ["subject"] = $"Invoice work order {number}",
            ["description"] = "The job is closed and the total is ready to be invoiced.",
            ["regardingobjectid"] = new EntityReference(WorkOrderColumns.EntityName, target.Id),
            ["scheduledend"] = DateTime.UtcNow.AddDays(1)
        });

        context.Tracing.Trace("{0}: follow-up task raised for {1}", PluginName, number);
    }
}
