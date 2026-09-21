using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.Common;
using DynamicsCrmLab.Domain.WorkOrders;

namespace DynamicsCrmLab.Application.UseCases.ViewWorkOrders;

/// <summary>
/// Represents one charge on a work order.
/// </summary>
/// <param name="LineId">The identifier of the line.</param>
/// <param name="Description">The work done or the part used.</param>
/// <param name="Quantity">The number of hours or units.</param>
/// <param name="UnitPrice">The price of a single hour or unit.</param>
/// <param name="LineTotal">The charge for this line.</param>
public sealed record WorkOrderLineView(
    Guid LineId,
    string Description,
    int Quantity,
    Money UnitPrice,
    Money LineTotal);

/// <summary>
/// Represents a work order in full.
/// </summary>
/// <param name="WorkOrderId">The identifier of the job.</param>
/// <param name="Number">The reference quoted to the customer.</param>
/// <param name="CustomerId">The customer the job is billed to.</param>
/// <param name="EquipmentId">The equipment the job concerns.</param>
/// <param name="TechnicianId">The technician responsible, if any.</param>
/// <param name="TechnicianName">The name of that technician, if any.</param>
/// <param name="Status">The stage the job has reached.</param>
/// <param name="Resolution">The account of the work, once it was closed.</param>
/// <param name="TotalPrice">The amount to invoice.</param>
/// <param name="Lines">The charges recorded against the job.</param>
public sealed record WorkOrderView(
    Guid WorkOrderId,
    string Number,
    Guid CustomerId,
    Guid EquipmentId,
    Guid? TechnicianId,
    string? TechnicianName,
    WorkOrderStatus Status,
    string? Resolution,
    Money TotalPrice,
    IReadOnlyList<WorkOrderLineView> Lines);

/// <summary>
/// Reads one work order in full.
/// </summary>
/// <remarks>
/// The technician is read alongside the job: a screen showing an identifier
/// where a name belongs is of no use to anybody, and the alternative is every
/// caller fetching the name itself.
/// </remarks>
/// <param name="workOrders">The store work orders are read from.</param>
/// <param name="technicians">The store technicians are read from.</param>
public sealed class GetWorkOrderHandler(
    IWorkOrderRepository workOrders,
    ITechnicianRepository technicians)
{
    /// <summary>
    /// Reads the work order with the given identifier.
    /// </summary>
    /// <param name="workOrderId">The job to read.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The job, or a failed result when no such record exists.</returns>
    public async Task<Result<WorkOrderView>> HandleAsync(
        Guid workOrderId,
        CancellationToken cancellationToken = default)
    {
        var workOrder = await workOrders.GetByIdAsync(workOrderId, cancellationToken).ConfigureAwait(false);
        if (workOrder is null)
        {
            return Result.NotFound<WorkOrderView>($"Work order {workOrderId} does not exist.");
        }

        var technician = workOrder.TechnicianId is { } technicianId
            ? await technicians.GetByIdAsync(technicianId, cancellationToken).ConfigureAwait(false)
            : null;

        return Result.Success(ToView(workOrder, technician?.FullName));
    }

    private static WorkOrderView ToView(WorkOrder workOrder, string? technicianName) =>
        new(workOrder.Id,
            workOrder.Number,
            workOrder.CustomerId,
            workOrder.EquipmentId,
            workOrder.TechnicianId,
            technicianName,
            workOrder.Status,
            workOrder.Resolution,
            workOrder.TotalPrice,
            [.. workOrder.Lines.Select(line => new WorkOrderLineView(
                line.Id,
                line.Description,
                line.Quantity,
                line.UnitPrice,
                line.LineTotal))]);
}
