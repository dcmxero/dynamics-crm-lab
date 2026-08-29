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
}
