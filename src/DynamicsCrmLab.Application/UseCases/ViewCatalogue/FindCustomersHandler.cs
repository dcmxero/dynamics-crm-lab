using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.Customers;
using DynamicsCrmLab.Domain.Equipments;

namespace DynamicsCrmLab.Application.UseCases.ViewCatalogue;

/// <summary>
/// Finds the customer a job is being raised for, and their equipment.
/// </summary>
/// <remarks>
/// Raising a job means naming a customer and one of their units. Without a way
/// to look either of them up, the only way to raise a job is to know an
/// identifier by heart, which nobody does.
///
/// Equipment is only ever listed within a customer: a job cannot be raised
/// against somebody else's unit, so offering one would be offering a mistake.
/// </remarks>
/// <param name="customers">The store customers are read from.</param>
/// <param name="equipment">The store equipment is read from.</param>
public sealed class FindCustomersHandler(ICustomerRepository customers, IEquipmentRepository equipment)
{
    /// <summary>The largest number of matches a single request will hand out.</summary>
    public const int MaxMatches = 50;

    /// <summary>
    /// Finds customers whose name begins with what has been typed.
    /// </summary>
    /// <param name="startingWith">
    /// What has been typed so far, or <see langword="null"/> for the first
    /// customers in name order.
    /// </param>
    /// <param name="maxCount">The largest number of matches to return.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The matching customers.</returns>
    public Task<IReadOnlyList<Customer>> HandleAsync(
        string? startingWith,
        int maxCount = 10,
        CancellationToken cancellationToken = default) =>
        customers.SearchAsync(startingWith, Math.Clamp(maxCount, 1, MaxMatches), cancellationToken);

    /// <summary>
    /// Lists one customer's equipment.
    /// </summary>
    /// <param name="customerId">The customer whose equipment to list.</param>
    /// <param name="maxCount">The largest number of records to return.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>
    /// The customer's equipment, or a failure when there is no such customer.
    /// </returns>
    public async Task<Result<IReadOnlyList<Equipment>>> EquipmentOfAsync(
        Guid customerId,
        int maxCount = MaxMatches,
        CancellationToken cancellationToken = default)
    {
        // A customer who does not exist and a customer with nothing registered
        // are different answers, and a caller choosing a unit needs to know
        // which one they got.
        if (await customers.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result.NotFound<IReadOnlyList<Equipment>>($"Customer {customerId} does not exist.");
        }

        var found = await equipment
            .ListForCustomerAsync(customerId, Math.Clamp(maxCount, 1, MaxMatches), cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(found);
    }
}
