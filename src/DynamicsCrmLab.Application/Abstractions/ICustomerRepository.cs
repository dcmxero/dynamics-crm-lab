using DynamicsCrmLab.Domain.Customers;

namespace DynamicsCrmLab.Application.Abstractions;

/// <summary>
/// Defines the store the application uses to read customers.
/// </summary>
public interface ICustomerRepository
{
    /// <summary>
    /// Loads a customer.
    /// </summary>
    /// <param name="id">The identifier of the customer.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The customer, or <see langword="null"/> when no such record exists.</returns>
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists customers whose name begins with what has been typed so far.
    /// </summary>
    /// <param name="startingWith">
    /// What to match the start of the name against, or <see langword="null"/>
    /// for the first customers in name order.
    /// </param>
    /// <param name="maxCount">The largest number of records to return.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The matching customers, in name order.</returns>
    Task<IReadOnlyList<Customer>> SearchAsync(
        string? startingWith,
        int maxCount,
        CancellationToken cancellationToken = default);
}
