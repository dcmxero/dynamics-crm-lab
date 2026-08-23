namespace DynamicsCrmLab.Domain.Customers;

/// <summary>
/// Represents a customer of the service company.
/// </summary>
/// <param name="id">The identifier of the customer record.</param>
/// <param name="name">The company or person the work is billed to.</param>
/// <param name="email">The address used for correspondence.</param>
/// <exception cref="ArgumentException">Thrown when the name is missing.</exception>
public sealed class Customer(Guid id, string name, string email)
{
    /// <summary>
    /// Gets the identifier of the customer record.
    /// </summary>
    public Guid Id { get; } = id;

    /// <summary>
    /// Gets the company or person the work is billed to.
    /// </summary>
    public string Name { get; } = string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("Customer name is required.", nameof(name))
        : name;

    /// <summary>
    /// Gets the address used for correspondence.
    /// </summary>
    public string Email { get; } = email;
}
