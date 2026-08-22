namespace DynamicsCrmLab.Domain.Equipments;

/// <summary>
/// Represents a serviceable unit installed at a customer site.
/// </summary>
/// <param name="id">The identifier of the equipment record.</param>
/// <param name="serialNumber">The manufacturer serial number identifying the unit.</param>
/// <param name="customerId">The customer the unit belongs to.</param>
/// <exception cref="ArgumentException">Thrown when the serial number is missing.</exception>
public sealed class Equipment(Guid id, string serialNumber, Guid customerId)
{
    /// <summary>
    /// Gets the identifier of the equipment record.
    /// </summary>
    public Guid Id { get; } = id;

    /// <summary>
    /// Gets the manufacturer serial number identifying the unit.
    /// </summary>
    public string SerialNumber { get; } = string.IsNullOrWhiteSpace(serialNumber)
        ? throw new ArgumentException("Serial number is required.", nameof(serialNumber))
        : serialNumber;

    /// <summary>
    /// Gets the customer the unit belongs to.
    /// </summary>
    public Guid CustomerId { get; } = customerId;
}
