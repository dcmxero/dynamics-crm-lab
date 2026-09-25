using DynamicsCrmLab.Domain.Common;
using DynamicsCrmLab.Domain.WorkOrders;

namespace DynamicsCrmLab.Application.UseCases.RaiseWorkOrder;

/// <summary>
/// Represents a work order that has just been raised.
/// </summary>
/// <param name="WorkOrderId">The identifier the store assigned to the record.</param>
/// <param name="Number">The reference quoted to the customer.</param>
/// <param name="Status">The stage the job starts in.</param>
/// <param name="TotalPrice">The amount to invoice, derived from the lines.</param>
/// <param name="WasRaisedNow">
/// <see langword="false"/> when an earlier request carrying the same key had
/// already raised it, so this one changed nothing.
/// </param>
public sealed record RaiseWorkOrderResult(
    Guid WorkOrderId,
    string Number,
    WorkOrderStatus Status,
    Money TotalPrice,
    bool WasRaisedNow = true);
