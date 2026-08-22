using DynamicsCrmLab.Domain.Common;

namespace DynamicsCrmLab.Domain.WorkOrders;

/// <summary>
/// Represents a single charge on a work order, either labour or material.
/// </summary>
/// <param name="id">The identifier of the line.</param>
/// <param name="description">The work done or the part used.</param>
/// <param name="quantity">The number of hours or units. Must be positive.</param>
/// <param name="unitPrice">The price of a single hour or unit.</param>
/// <exception cref="DomainException">
/// Thrown when the description is missing or the quantity is not positive.
/// </exception>
public sealed class WorkOrderLine(Guid id, string description, int quantity, Money unitPrice)
{
    /// <summary>
    /// Gets the identifier of the line.
    /// </summary>
    public Guid Id { get; } = id;

    /// <summary>
    /// Gets the work done or the part used.
    /// </summary>
    public string Description { get; } = string.IsNullOrWhiteSpace(description)
        ? throw new DomainException("A work order line needs a description.")
        : description;

    /// <summary>
    /// Gets the number of hours or units charged.
    /// </summary>
    public int Quantity { get; } = quantity is > 0
        ? quantity
        : throw new DomainException("Line quantity must be positive.");

    /// <summary>
    /// Gets the price of a single hour or unit.
    /// </summary>
    public Money UnitPrice { get; } = unitPrice;

    /// <summary>
    /// Gets the charge for this line, that is the unit price times the quantity.
    /// </summary>
    public Money LineTotal => UnitPrice * Quantity;
}
