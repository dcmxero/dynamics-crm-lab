using DynamicsCrmLab.Domain.Equipments;

namespace DynamicsCrmLab.Application.Abstractions;

/// <summary>
/// Defines the store the application uses to read equipment.
/// </summary>
public interface IEquipmentRepository
{
    /// <summary>
    /// Loads a piece of equipment.
    /// </summary>
    /// <param name="id">The identifier of the equipment.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The equipment, or <see langword="null"/> when no such record exists.</returns>
    Task<Equipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
