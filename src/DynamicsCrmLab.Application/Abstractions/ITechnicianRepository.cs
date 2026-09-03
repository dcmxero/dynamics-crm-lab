using DynamicsCrmLab.Domain.Technicians;

namespace DynamicsCrmLab.Application.Abstractions;

/// <summary>
/// Defines the store the application uses to read technicians.
/// </summary>
public interface ITechnicianRepository
{
    /// <summary>
    /// Loads a technician.
    /// </summary>
    /// <param name="id">The identifier of the technician.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The technician, or <see langword="null"/> when no such record exists.</returns>
    Task<Technician?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists technicians who can take on further work.
    /// </summary>
    /// <param name="maxCount">The largest number of records to return.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The available technicians.</returns>
    Task<IReadOnlyList<Technician>> ListAvailableAsync(
        int maxCount,
        CancellationToken cancellationToken = default);
}
