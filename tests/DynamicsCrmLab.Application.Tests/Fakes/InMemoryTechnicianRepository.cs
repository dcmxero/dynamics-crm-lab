using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.Technicians;

namespace DynamicsCrmLab.Application.Tests.Fakes;

internal sealed class InMemoryTechnicianRepository : ITechnicianRepository
{
    private readonly Dictionary<Guid, Technician> _stored = [];

    public Technician Add(string fullName = "Peter Kovac", bool isAvailable = true)
    {
        var technician = new Technician(Guid.NewGuid(), fullName, isAvailable);
        _stored[technician.Id] = technician;
        return technician;
    }

    public Task<Technician?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_stored.GetValueOrDefault(id));

    public Task<IReadOnlyList<Technician>> ListAvailableAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Technician> available = [.. _stored.Values.Where(t => t.IsAvailable).Take(maxCount)];

        return Task.FromResult(available);
    }
}
