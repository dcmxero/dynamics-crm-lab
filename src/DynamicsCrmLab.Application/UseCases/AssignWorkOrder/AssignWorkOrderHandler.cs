using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.Common;
using DynamicsCrmLab.Domain.WorkOrders;
using Microsoft.Extensions.Logging;

namespace DynamicsCrmLab.Application.UseCases.AssignWorkOrder;

/// <summary>
/// Represents a request to put a technician on a work order.
/// </summary>
/// <param name="WorkOrderId">The job to assign.</param>
/// <param name="TechnicianId">
/// The technician to assign, or <see langword="null"/> to take the first
/// available one.
/// </param>
public sealed record AssignWorkOrderCommand(Guid WorkOrderId, Guid? TechnicianId = null);

/// <summary>
/// Represents a work order that has just been assigned.
/// </summary>
/// <param name="WorkOrderId">The job that was assigned.</param>
/// <param name="TechnicianId">The technician now responsible for it.</param>
/// <param name="TechnicianName">The name shown on the schedule.</param>
public sealed record AssignWorkOrderResult(Guid WorkOrderId, Guid TechnicianId, string TechnicianName);

/// <summary>
/// Puts a technician on a work order, picking one when none was named.
/// </summary>
/// <param name="workOrders">The store work orders are read from and written to.</param>
/// <param name="technicians">The store technicians are read from.</param>
/// <param name="logger">Records the outcome of the use case.</param>
public sealed class AssignWorkOrderHandler(
    IWorkOrderRepository workOrders,
    ITechnicianRepository technicians,
    ILogger<AssignWorkOrderHandler> logger)
{
    private const int CandidatesToConsider = 25;

    /// <summary>
    /// Assigns the work order described by the command.
    /// </summary>
    /// <param name="command">The job to assign and, optionally, to whom.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>
    /// The assignment, or a failed result when the job or the technician cannot
    /// be found, when nobody is available, or when the job no longer accepts one.
    /// </returns>
    public async Task<Result<AssignWorkOrderResult>> HandleAsync(
        AssignWorkOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workOrder = await workOrders.GetByIdAsync(command.WorkOrderId, cancellationToken).ConfigureAwait(false);
        if (workOrder is null)
        {
            return Result.NotFound<AssignWorkOrderResult>($"Work order {command.WorkOrderId} does not exist.");
        }

        var technician = await ResolveTechnicianAsync(command.TechnicianId, cancellationToken).ConfigureAwait(false);
        if (technician is null)
        {
            return command.TechnicianId is null
                ? Result.RuleBroken<AssignWorkOrderResult>("No technician is available.")
                : Result.NotFound<AssignWorkOrderResult>($"Technician {command.TechnicianId} does not exist.");
        }

        try
        {
            workOrder.AssignTo(technician.Id);
        }
        catch (DomainException exception)
        {
            return Result.RuleBroken<AssignWorkOrderResult>(exception.Message);
        }

        await workOrders.UpdateAsync(workOrder, cancellationToken).ConfigureAwait(false);

        ApplicationLog.WorkOrderAssigned(logger, workOrder.Number, technician.FullName);

        return Result.Success(new AssignWorkOrderResult(workOrder.Id, technician.Id, technician.FullName));
    }

    private async Task<Domain.Technicians.Technician?> ResolveTechnicianAsync(
        Guid? technicianId,
        CancellationToken cancellationToken)
    {
        if (technicianId is { } id)
        {
            var named = await technicians.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

            return named is { IsAvailable: true } ? named : null;
        }

        var available = await technicians
            .ListAvailableAsync(CandidatesToConsider, cancellationToken)
            .ConfigureAwait(false);

        return available.Count is 0 ? null : available[0];
    }
}
