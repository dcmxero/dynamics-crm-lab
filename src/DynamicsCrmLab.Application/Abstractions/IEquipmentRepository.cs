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

    /// <summary>
    /// Lists the equipment belonging to one customer.
    /// </summary>
    /// <remarks>
    /// A job is raised against a customer's own unit, so the choice is only ever
    /// made within one customer. Listing all the equipment in the environment
    /// would offer units the job could not be raised against.
    /// </remarks>
    /// <param name="customerId">The customer whose equipment to list.</param>
    /// <param name="maxCount">The largest number of records to return.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The customer's equipment, in serial number order.</returns>
    Task<IReadOnlyList<Equipment>> ListForCustomerAsync(
        Guid customerId,
        int maxCount,
        CancellationToken cancellationToken = default);
}
