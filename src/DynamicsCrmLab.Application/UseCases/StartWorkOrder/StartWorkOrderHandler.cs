using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.Common;
using DynamicsCrmLab.Domain.WorkOrders;
using Microsoft.Extensions.Logging;

namespace DynamicsCrmLab.Application.UseCases.StartWorkOrder;

/// <summary>
/// Represents a request to start work on a job.
/// </summary>
/// <param name="WorkOrderId">The job the technician is starting.</param>
public sealed record StartWorkOrderCommand(Guid WorkOrderId);

/// <summary>
/// Represents a work order work has just started on.
/// </summary>
/// <param name="WorkOrderId">The job that was started.</param>
/// <param name="Number">The reference quoted to the customer.</param>
/// <param name="Status">The stage the job has reached.</param>
public sealed record StartWorkOrderResult(Guid WorkOrderId, string Number, WorkOrderStatus Status);

/// <summary>
/// Moves an assigned job to in progress.
/// </summary>
/// <remarks>
/// A job cannot be closed straight from assigned, so without this step the
/// lifecycle cannot be completed: the technician arriving on site is a distinct
/// event from being given the job.
/// </remarks>
/// <param name="workOrders">The store work orders are read from and written to.</param>
/// <param name="logger">Records the outcome of the use case.</param>
public sealed class StartWorkOrderHandler(
    IWorkOrderRepository workOrders,
    ILogger<StartWorkOrderHandler> logger)
{
    /// <summary>
    /// Starts work on the job described by the command.
    /// </summary>
    /// <param name="command">The job to start.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>
    /// The started work order, or a failed result when the job cannot be found
    /// or has not been assigned.
    /// </returns>
    public async Task<Result<StartWorkOrderResult>> HandleAsync(
        StartWorkOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workOrder = await workOrders.GetByIdAsync(command.WorkOrderId, cancellationToken).ConfigureAwait(false);
        if (workOrder is null)
        {
            return Result.NotFound<StartWorkOrderResult>($"Work order {command.WorkOrderId} does not exist.");
        }

        try
        {
            workOrder.StartWork();
        }
        catch (DomainException exception)
        {
            return Result.RuleBroken<StartWorkOrderResult>(exception.Message);
        }

        await workOrders.UpdateAsync(workOrder, cancellationToken).ConfigureAwait(false);

        ApplicationLog.WorkOrderStarted(logger, workOrder.Number);

        return Result.Success(new StartWorkOrderResult(workOrder.Id, workOrder.Number, workOrder.Status));
    }
}
