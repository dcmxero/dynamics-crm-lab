using DynamicsCrmLab.Domain.WorkOrders;

namespace DynamicsCrmLab.Application.Abstractions;

/// <summary>
/// Defines the store the application uses to persist work orders.
/// </summary>
/// <remarks>
/// The application layer never learns whether the store is Dataverse, a
/// database or an in-memory list, which is what keeps the use cases testable
/// without a running environment.
/// </remarks>
public interface IWorkOrderRepository
{
    /// <summary>
    /// Loads a work order.
    /// </summary>
    /// <param name="id">The identifier of the work order.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The work order, or <see langword="null"/> when no such record exists.</returns>
    Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a newly raised work order, unless this request already raised one.
    /// </summary>
    /// <remarks>
    /// The key names the request, not the job. A client that sends the same
    /// request twice - a retry after a timeout, a second press of a button -
    /// means one job, and the store is what decides that rather than a check
    /// the second request could arrive too quickly to see.
    /// </remarks>
    /// <param name="workOrder">The work order to store.</param>
    /// <param name="requestKey">What the caller called this request.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The job this request stands for, and whether it was raised now.</returns>
    Task<StoredWorkOrder> AddAsync(
        WorkOrder workOrder,
        string requestKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes back the parts of a work order that can change after it was raised.
    /// </summary>
    /// <param name="workOrder">The work order to write back.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes once the record has been written.</returns>
    Task UpdateAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists one page of work orders in a given stage, most recently raised first.
    /// </summary>
    /// <param name="status">The stage to filter by.</param>
    /// <param name="maxCount">The largest number of records to return.</param>
    /// <param name="cursor">
    /// Where to carry on from, as handed out by a previous page, or
    /// <see langword="null"/> to start at the beginning.
    /// </param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The matching work orders and where the next page starts.</returns>
    Task<Page<WorkOrder>> ListByStatusAsync(
        WorkOrderStatus status,
        int maxCount,
        string? cursor = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the job a raise request stands for.
/// </summary>
/// <param name="WorkOrderId">The identifier of the job.</param>
/// <param name="Number">The reference quoted to the customer.</param>
/// <param name="WasRaisedNow">
/// <see langword="false"/> when an earlier request with the same key had
/// already raised it.
/// </param>
public sealed record StoredWorkOrder(Guid WorkOrderId, string Number, bool WasRaisedNow);
