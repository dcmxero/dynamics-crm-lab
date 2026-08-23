namespace DynamicsCrmLab.Domain.Technicians;

/// <summary>
/// Represents a field technician a work order can be assigned to.
/// </summary>
/// <param name="id">The identifier of the technician record.</param>
/// <param name="fullName">The name shown on the schedule.</param>
/// <param name="isAvailable">
/// <c>true</c> when the technician can take on further work; otherwise, <c>false</c>.
/// </param>
/// <exception cref="ArgumentException">Thrown when the name is missing.</exception>
public sealed class Technician(Guid id, string fullName, bool isAvailable)
{
    /// <summary>
    /// Gets the identifier of the technician record.
    /// </summary>
    public Guid Id { get; } = id;

    /// <summary>
    /// Gets the name shown on the schedule.
    /// </summary>
    public string FullName { get; } = string.IsNullOrWhiteSpace(fullName)
        ? throw new ArgumentException("Technician name is required.", nameof(fullName))
        : fullName;

    /// <summary>
    /// Gets a value indicating whether the technician can take on further work.
    /// </summary>
    public bool IsAvailable { get; } = isAvailable;
}
