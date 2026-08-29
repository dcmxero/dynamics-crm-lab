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
    /// Stores a newly raised work order.
    /// </summary>
    /// <param name="workOrder">The work order to store.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The identifier the store assigned to the record.</returns>
    Task<Guid> AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes back the parts of a work order that can change after it was raised.
    /// </summary>
    /// <param name="workOrder">The work order to write back.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes once the record has been written.</returns>
    Task UpdateAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists work orders in a given stage, most recently raised first.
    /// </summary>
    /// <param name="status">The stage to filter by.</param>
    /// <param name="maxCount">The largest number of records to return.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The matching work orders.</returns>
    Task<IReadOnlyList<WorkOrder>> ListByStatusAsync(
        WorkOrderStatus status,
        int maxCount,
        CancellationToken cancellationToken = default);
}
