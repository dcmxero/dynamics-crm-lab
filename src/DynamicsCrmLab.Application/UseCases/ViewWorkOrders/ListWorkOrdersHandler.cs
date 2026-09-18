using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.Common;
using DynamicsCrmLab.Domain.WorkOrders;

namespace DynamicsCrmLab.Application.UseCases.ViewWorkOrders;

/// <summary>
/// Represents one work order as it appears in a list.
/// </summary>
/// <param name="WorkOrderId">The identifier of the job.</param>
/// <param name="Number">The reference quoted to the customer.</param>
/// <param name="Status">The stage the job has reached.</param>
/// <param name="TechnicianId">The technician responsible, if any.</param>
/// <param name="TotalPrice">The amount to invoice.</param>
/// <param name="LineCount">How many charges have been recorded.</param>
public sealed record WorkOrderSummary(
    Guid WorkOrderId,
    string Number,
    WorkOrderStatus Status,
    Guid? TechnicianId,
    Money TotalPrice,
    int LineCount);

/// <summary>
/// Lists the work orders that have reached a given stage.
/// </summary>
/// <param name="workOrders">The store work orders are read from.</param>
public sealed class ListWorkOrdersHandler(IWorkOrderRepository workOrders)
{
    /// <summary>The largest page the API will hand out in one response.</summary>
    public const int MaxPageSize = 200;

    /// <summary>
    /// Lists work orders in the given stage.
    /// </summary>
    /// <param name="status">The stage to filter by.</param>
    /// <param name="maxCount">The largest number of jobs to return.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The matching jobs, most recently raised first.</returns>
    public async Task<IReadOnlyList<WorkOrderSummary>> HandleAsync(
        WorkOrderStatus status,
        int maxCount = 50,
        CancellationToken cancellationToken = default)
    {
        var capped = Math.Clamp(maxCount, 1, MaxPageSize);

        var found = await workOrders
            .ListByStatusAsync(status, capped, cancellationToken)
            .ConfigureAwait(false);

        return [.. found.Select(Summarise)];
    }

    private static WorkOrderSummary Summarise(WorkOrder workOrder) =>
        new(workOrder.Id,
            workOrder.Number,
            workOrder.Status,
            workOrder.TechnicianId,
            workOrder.TotalPrice,
            workOrder.Lines.Count);
}
