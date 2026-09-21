using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.Common;
using DynamicsCrmLab.Domain.WorkOrders;
using Microsoft.Extensions.Logging;

namespace DynamicsCrmLab.Application.UseCases.CloseWorkOrder;

/// <summary>
/// Represents a request to finish a work order.
/// </summary>
/// <param name="WorkOrderId">The job to close.</param>
/// <param name="Resolution">The account of the work carried out.</param>
public sealed record CloseWorkOrderCommand(Guid WorkOrderId, string Resolution);

/// <summary>
/// Represents a work order that has just been closed.
/// </summary>
/// <param name="WorkOrderId">The job that was closed.</param>
/// <param name="Number">The reference quoted to the customer.</param>
/// <param name="TotalPrice">The amount to invoice.</param>
public sealed record CloseWorkOrderResult(Guid WorkOrderId, string Number, Money TotalPrice);

/// <summary>
/// Finishes a work order and records what was done.
/// </summary>
/// <param name="workOrders">The store work orders are read from and written to.</param>
/// <param name="logger">Records the outcome of the use case.</param>
public sealed class CloseWorkOrderHandler(
    IWorkOrderRepository workOrders,
    ILogger<CloseWorkOrderHandler> logger)
{
    /// <summary>
    /// Closes the work order described by the command.
    /// </summary>
    /// <param name="command">The job to close and the resolution to record.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>
    /// The closed work order, or a failed result when the job cannot be found
    /// or is not in a state that can be closed.
    /// </returns>
    public async Task<Result<CloseWorkOrderResult>> HandleAsync(
        CloseWorkOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workOrder = await workOrders.GetByIdAsync(command.WorkOrderId, cancellationToken).ConfigureAwait(false);
        if (workOrder is null)
        {
            return Result.NotFound<CloseWorkOrderResult>($"Work order {command.WorkOrderId} does not exist.");
        }

        try
        {
            workOrder.Close(command.Resolution);

            // The store enforces rules of its own, and a rule it refuses is the
            // same kind of answer as one the aggregate refuses.
            await workOrders.UpdateAsync(workOrder, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException exception)
        {
            return Result.RuleBroken<CloseWorkOrderResult>(exception.Message);
        }

        ApplicationLog.WorkOrderClosed(logger, workOrder.Number, workOrder.TotalPrice);

        return Result.Success(new CloseWorkOrderResult(workOrder.Id, workOrder.Number, workOrder.TotalPrice));
    }
}
