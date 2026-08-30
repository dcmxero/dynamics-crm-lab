using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.WorkOrders;

namespace DynamicsCrmLab.Application.Tests.Fakes;

internal sealed class InMemoryWorkOrderRepository : IWorkOrderRepository
{
    private readonly Dictionary<Guid, WorkOrder> _stored = [];

    public IReadOnlyCollection<WorkOrder> Stored => _stored.Values;

    public Task<Guid> AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        _stored[workOrder.Id] = workOrder;
        return Task.FromResult(workOrder.Id);
    }

    public Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_stored.GetValueOrDefault(id));

    public Task UpdateAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        _stored[workOrder.Id] = workOrder;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<WorkOrder>> ListByStatusAsync(
        WorkOrderStatus status,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WorkOrder> matching = [.. _stored.Values.Where(o => o.Status == status).Take(maxCount)];

        return Task.FromResult(matching);
    }
}
