using System.Globalization;
using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.WorkOrders;

namespace DynamicsCrmLab.Application.Tests.Fakes;

internal sealed class InMemoryWorkOrderRepository : IWorkOrderRepository
{
    private readonly Dictionary<Guid, WorkOrder> _stored = [];

    public IReadOnlyCollection<WorkOrder> Stored => _stored.Values;

    private readonly Dictionary<string, Guid> _byRequestKey = new(StringComparer.Ordinal);

    public Task<StoredWorkOrder> AddAsync(
        WorkOrder workOrder,
        string requestKey,
        CancellationToken cancellationToken = default)
    {
        // The store is what decides that two requests with one key mean one
        // job, so the fake has to decide it the same way.
        if (_byRequestKey.TryGetValue(requestKey, out var raisedEarlier))
        {
            var earlier = _stored[raisedEarlier];

            return Task.FromResult(new StoredWorkOrder(earlier.Id, earlier.Number, WasRaisedNow: false));
        }

        _stored[workOrder.Id] = workOrder;
        _byRequestKey[requestKey] = workOrder.Id;

        return Task.FromResult(new StoredWorkOrder(workOrder.Id, workOrder.Number, WasRaisedNow: true));
    }

    public Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_stored.GetValueOrDefault(id));

    public Task UpdateAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        _stored[workOrder.Id] = workOrder;
        return Task.CompletedTask;
    }

    public Task<Page<WorkOrder>> ListByStatusAsync(
        WorkOrderStatus status,
        int maxCount,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        // The cursor is the count already handed out, which is all a list in
        // memory needs to carry on from.
        var alreadyRead = int.TryParse(cursor, out var parsed) ? parsed : 0;

        var matching = _stored.Values.Where(o => o.Status == status).ToList();
        IReadOnlyList<WorkOrder> page = [.. matching.Skip(alreadyRead).Take(maxCount)];

        var next = alreadyRead + page.Count < matching.Count
            ? (alreadyRead + page.Count).ToString(CultureInfo.InvariantCulture)
            : null;

        return Task.FromResult(new Page<WorkOrder>(page, next));
    }
}
