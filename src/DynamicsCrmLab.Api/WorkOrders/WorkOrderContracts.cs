using DynamicsCrmLab.Application.UseCases.AssignWorkOrder;
using DynamicsCrmLab.Application.UseCases.CloseWorkOrder;
using DynamicsCrmLab.Application.UseCases.RaiseWorkOrder;
using DynamicsCrmLab.Application.UseCases.StartWorkOrder;
using DynamicsCrmLab.Application.UseCases.ViewWorkOrders;
using DynamicsCrmLab.Domain.WorkOrders;

namespace DynamicsCrmLab.Api.WorkOrders;

/// <summary>
/// Represents a work order as the API hands it out in a list.
/// </summary>
/// <param name="Id">The identifier of the job.</param>
/// <param name="Number">The reference quoted to the customer.</param>
/// <param name="Status">The stage the job has reached.</param>
/// <param name="TechnicianId">The technician responsible, if any.</param>
/// <param name="TotalPrice">The amount to invoice.</param>
/// <param name="Currency">The currency the total is expressed in.</param>
/// <param name="LineCount">How many charges have been recorded.</param>
internal sealed record WorkOrderSummaryResponse(
    Guid Id,
    string Number,
    string Status,
    Guid? TechnicianId,
    decimal TotalPrice,
    string Currency,
    int LineCount);

/// <summary>
/// Represents one charge as the API hands it out.
/// </summary>
/// <param name="Id">The identifier of the line.</param>
/// <param name="Description">The work done or the part used.</param>
/// <param name="Quantity">The number of hours or units.</param>
/// <param name="UnitPrice">The price of a single hour or unit.</param>
/// <param name="LineTotal">The charge for this line.</param>
internal sealed record WorkOrderLineResponse(
    Guid Id,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

/// <summary>
/// Represents a work order in full as the API hands it out.
/// </summary>
/// <param name="Id">The identifier of the job.</param>
/// <param name="Number">The reference quoted to the customer.</param>
/// <param name="CustomerId">The customer the job is billed to.</param>
/// <param name="EquipmentId">The equipment the job concerns.</param>
/// <param name="TechnicianId">The technician responsible, if any.</param>
/// <param name="TechnicianName">The name of that technician, if any.</param>
/// <param name="Status">The stage the job has reached.</param>
/// <param name="Resolution">The account of the work, once it was closed.</param>
/// <param name="TotalPrice">The amount to invoice.</param>
/// <param name="Currency">The currency the totals are expressed in.</param>
/// <param name="Lines">The charges recorded against the job.</param>
internal sealed record WorkOrderResponse(
    Guid Id,
    string Number,
    Guid CustomerId,
    Guid EquipmentId,
    Guid? TechnicianId,
    string? TechnicianName,
    string Status,
    string? Resolution,
    decimal TotalPrice,
    string Currency,
    IReadOnlyList<WorkOrderLineResponse> Lines);

/// <summary>
/// Represents one charge supplied when raising a work order.
/// </summary>
/// <param name="Description">The work done or the part used.</param>
/// <param name="Quantity">The number of hours or units.</param>
/// <param name="UnitPrice">The price of a single hour or unit.</param>
internal sealed record WorkOrderLineRequest(string Description, int Quantity, decimal UnitPrice);

/// <summary>
/// Represents a request to raise a work order.
/// </summary>
/// <param name="CustomerId">The customer the job is billed to.</param>
/// <param name="EquipmentId">The equipment the job concerns.</param>
/// <param name="Lines">The charges to record against the job.</param>
internal sealed record RaiseWorkOrderRequest(
    Guid CustomerId,
    Guid EquipmentId,
    IReadOnlyList<WorkOrderLineRequest> Lines);

/// <summary>
/// Represents a request to put a technician on a work order.
/// </summary>
/// <param name="TechnicianId">
/// The technician to assign, or <see langword="null"/> to take whoever is free.
/// </param>
internal sealed record AssignWorkOrderRequest(Guid? TechnicianId);

/// <summary>
/// Represents a request to finish a work order.
/// </summary>
/// <param name="Resolution">The account of the work carried out.</param>
internal sealed record CloseWorkOrderRequest(string Resolution);

/// <summary>
/// Represents a work order that has just been raised.
/// </summary>
/// <param name="Id">The identifier of the job.</param>
/// <param name="Number">The reference quoted to the customer.</param>
/// <param name="Status">The stage the job starts in.</param>
/// <param name="TotalPrice">The amount to invoice.</param>
/// <param name="Currency">The currency the total is expressed in.</param>
internal sealed record WorkOrderCreatedResponse(
    Guid Id,
    string Number,
    string Status,
    decimal TotalPrice,
    string Currency);

/// <summary>
/// Represents a work order that has just been assigned.
/// </summary>
/// <param name="Id">The identifier of the job.</param>
/// <param name="TechnicianId">The technician now responsible for it.</param>
/// <param name="TechnicianName">The name shown on the schedule.</param>
internal sealed record WorkOrderAssignedResponse(Guid Id, Guid TechnicianId, string TechnicianName);

/// <summary>
/// Represents a work order work has just started on.
/// </summary>
/// <param name="Id">The identifier of the job.</param>
/// <param name="Number">The reference quoted to the customer.</param>
/// <param name="Status">The stage the job has reached.</param>
internal sealed record WorkOrderStartedResponse(Guid Id, string Number, string Status);

/// <summary>
/// Represents a work order that has just been closed.
/// </summary>
/// <param name="Id">The identifier of the job.</param>
/// <param name="Number">The reference quoted to the customer.</param>
/// <param name="TotalPrice">The amount to invoice.</param>
/// <param name="Currency">The currency the total is expressed in.</param>
internal sealed record WorkOrderClosedResponse(
    Guid Id,
    string Number,
    decimal TotalPrice,
    string Currency);

/// <summary>
/// Turns what the use cases return into what the API hands out.
/// </summary>
/// <remarks>
/// The wire shape is deliberately its own thing: statuses travel as names so a
/// renumbered choice column cannot silently change what a client sees, and
/// amounts are split into a number and a currency rather than leaking the
/// domain value object.
/// </remarks>
internal static class WorkOrderMapping
{
    public static WorkOrderSummaryResponse ToResponse(this WorkOrderSummary summary) =>
        new(summary.WorkOrderId,
            summary.Number,
            summary.Status.ToString(),
            summary.TechnicianId,
            summary.TotalPrice.Amount,
            summary.TotalPrice.Currency,
            summary.LineCount);

    public static WorkOrderResponse ToResponse(this WorkOrderView view) =>
        new(view.WorkOrderId,
            view.Number,
            view.CustomerId,
            view.EquipmentId,
            view.TechnicianId,
            view.TechnicianName,
            view.Status.ToString(),
            view.Resolution,
            view.TotalPrice.Amount,
            view.TotalPrice.Currency,
            [.. view.Lines.Select(line => new WorkOrderLineResponse(
                line.LineId,
                line.Description,
                line.Quantity,
                line.UnitPrice.Amount,
                line.LineTotal.Amount))]);

    public static WorkOrderCreatedResponse ToResponse(this RaiseWorkOrderResult raised) =>
        new(raised.WorkOrderId,
            raised.Number,
            raised.Status.ToString(),
            raised.TotalPrice.Amount,
            raised.TotalPrice.Currency);

    public static WorkOrderAssignedResponse ToResponse(this AssignWorkOrderResult assigned) =>
        new(assigned.WorkOrderId, assigned.TechnicianId, assigned.TechnicianName);

    public static WorkOrderStartedResponse ToResponse(this StartWorkOrderResult started) =>
        new(started.WorkOrderId, started.Number, started.Status.ToString());

    public static WorkOrderClosedResponse ToResponse(this CloseWorkOrderResult closed) =>
        new(closed.WorkOrderId, closed.Number, closed.TotalPrice.Amount, closed.TotalPrice.Currency);

    public static bool TryParseStatus(string? value, out WorkOrderStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status) && Enum.IsDefined(status);
}
