using System.Globalization;
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
