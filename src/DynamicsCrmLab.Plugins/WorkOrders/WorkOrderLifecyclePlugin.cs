using System;
using System.Linq;
using DynamicsCrmLab.Domain.Common;
using DynamicsCrmLab.Domain.WorkOrders;
using DynamicsCrmLab.Plugins.Infrastructure;
using DynamicsCrmLab.Schema;
using Microsoft.Xrm.Sdk;

namespace DynamicsCrmLab.Plugins.WorkOrders;

/// <summary>
/// Refuses a change of stage the job does not allow.
/// </summary>
/// <remarks>
/// Register on Update of dcl_workorder, filtering attribute dcl_status only,
/// stage PreOperation (20), synchronous, with a pre image carrying dcl_status,
/// dcl_technicianid and dcl_resolution.
///
/// Without this the rules live only in the application: anything writing
/// straight to the table - a bulk edit, a flow, a developer in the maker portal
/// - can close a job nobody ever started, or reopen one that is finished. The
/// platform is the system of record, so the platform is where the rules have to
/// hold.
///
/// The rules themselves are not restated here. The stage the job came from is
/// rebuilt into the aggregate and the aggregate is asked to make the move, so
/// the answer is the same one the API would give.
///
/// PreOperation because a refusal has to stop the write, and synchronous for
/// the same reason.
/// </remarks>
public sealed class WorkOrderLifecyclePlugin() : PluginBase(nameof(WorkOrderLifecyclePlugin))
{
    /// <inheritdoc/>
    protected override void Execute(PluginContext context)
    {
        var target = context.Target;

        if (target is null || !string.Equals(target.LogicalName, WorkOrderSchema.EntityName, StringComparison.Ordinal))
        {
            return;
        }

        var requested = target.GetAttributeValue<OptionSetValue>(WorkOrderSchema.Status);
        if (requested is null)
        {
            return;
        }

        var preImage = context.PreImage();
        if (preImage is null)
        {
            context.Tracing.Trace("{0}: no pre image, the stage it came from is unknown", PluginName);

            return;
        }

        var current = (WorkOrderStatus)preImage.GetAttributeValue<OptionSetValue>(WorkOrderSchema.Status).Value;
        var wanted = (WorkOrderStatus)requested.Value;

        if (current == wanted)
        {
            return;
        }

        context.Tracing.Trace("{0}: {1} -> {2}", PluginName, current, wanted);

        // The aggregate holds the rules; rebuilding the stage it came from and
        // asking it to make the move gives the platform the same answer the
        // application gives, rather than a second copy that can drift from it.
        var workOrder = Restore(target.Id, preImage, current);

        switch (wanted)
        {
            case WorkOrderStatus.Assigned:
                workOrder.AssignTo(TechnicianOf(target, preImage));
                break;

            case WorkOrderStatus.InProgress:
                workOrder.StartWork();
                break;

            case WorkOrderStatus.Closed:
                workOrder.Close(ResolutionOf(target, preImage));
                break;

            case WorkOrderStatus.New:
            default:
                throw new DomainException($"A work order cannot be put back to {wanted}.");
        }
    }

    private static WorkOrder Restore(Guid id, Entity preImage, WorkOrderStatus current)
    {
        var technician = preImage.GetAttributeValue<EntityReference>(WorkOrderSchema.Technician)?.Id;

        return WorkOrder.Restore(
            id,
            preImage.GetAttributeValue<string>(WorkOrderSchema.Number) ?? "(unnumbered)",
            Guid.NewGuid(),
            Guid.NewGuid(),
            technician,
            current,
            preImage.GetAttributeValue<string>(WorkOrderSchema.Resolution),
            Enumerable.Empty<WorkOrderLine>());
    }

    /// <summary>
    /// Reads the technician the change puts on the job.
    /// </summary>
    /// <remarks>
    /// An update carries only what changed, so a stage moved without touching
    /// the lookup leaves the technician in the image.
    /// </remarks>
    private static Guid TechnicianOf(Entity target, Entity preImage)
    {
        var reference = target.Contains(WorkOrderSchema.Technician)
            ? target.GetAttributeValue<EntityReference>(WorkOrderSchema.Technician)
            : preImage.GetAttributeValue<EntityReference>(WorkOrderSchema.Technician);

        return reference?.Id ?? Guid.Empty;
    }

    private static string ResolutionOf(Entity target, Entity preImage) =>
        (target.Contains(WorkOrderSchema.Resolution)
            ? target.GetAttributeValue<string>(WorkOrderSchema.Resolution)
            : preImage.GetAttributeValue<string>(WorkOrderSchema.Resolution)) ?? string.Empty;
}
