using DynamicsCrmLab.Domain.Common;

namespace DynamicsCrmLab.Domain.WorkOrders;

/// <summary>
/// Represents a service job raised against a piece of customer equipment.
/// </summary>
/// <remarks>
/// The aggregate owns its own rules. Construction goes through
/// <see cref="Create(Guid, Guid)"/> rather than a primary constructor, because a
/// primary constructor is as accessible as the type and would let callers build
/// a work order that skips those rules.
/// </remarks>
public sealed class WorkOrder
{
    private readonly List<WorkOrderLine> _lines = [];

    private WorkOrder(Guid id, string number, Guid customerId, Guid equipmentId, WorkOrderStatus status)
    {
        Id = id;
        Number = number;
        CustomerId = customerId;
        EquipmentId = equipmentId;
        Status = status;
    }

    /// <summary>
    /// Gets the identifier of the work order.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the human-readable reference quoted to the customer.
    /// </summary>
    public string Number { get; }

    /// <summary>
    /// Gets the customer the job is billed to.
    /// </summary>
    public Guid CustomerId { get; }

    /// <summary>
    /// Gets the equipment the job was raised against.
    /// </summary>
    public Guid EquipmentId { get; }

    /// <summary>
    /// Gets the technician the job is assigned to, or <see langword="null"/> while it is unassigned.
    /// </summary>
    public Guid? TechnicianId { get; private set; }

    /// <summary>
    /// Gets the current lifecycle stage.
    /// </summary>
    public WorkOrderStatus Status { get; private set; }

    /// <summary>
    /// Gets the account of what was done, recorded when the job was closed.
    /// </summary>
    public string? Resolution { get; private set; }

    /// <summary>
    /// Gets the charges recorded against the job.
    /// </summary>
    public IReadOnlyList<WorkOrderLine> Lines => _lines;

    /// <summary>
    /// Gets the amount to invoice, derived from the lines.
    /// </summary>
    /// <remarks>
    /// The currency comes from the lines rather than from a seed value, because
    /// <see cref="Money.Zero(string)"/> is always EUR and would reject a job
    /// priced in anything else.
    /// </remarks>
    public Money TotalPrice => _lines.Count is 0
        ? Money.Zero()
        : _lines.Select(line => line.LineTotal).Aggregate(Money.Add);

    /// <summary>
    /// Raises a new work order against a piece of customer equipment.
    /// </summary>
    /// <param name="customerId">The customer the job is billed to.</param>
    /// <param name="equipmentId">The equipment the job concerns.</param>
    /// <returns>A work order in the <see cref="WorkOrderStatus.New"/> stage.</returns>
    /// <exception cref="DomainException">Thrown when the customer or the equipment is missing.</exception>
    public static WorkOrder Create(Guid customerId, Guid equipmentId)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("A work order must have a customer.");
        }

        if (equipmentId == Guid.Empty)
        {
            throw new DomainException("A work order must have a piece of equipment.");
        }

        return new WorkOrder(Guid.NewGuid(), GenerateNumber(), customerId, equipmentId, WorkOrderStatus.New);
    }

    /// <summary>
    /// Rebuilds a work order from state that was previously stored.
    /// </summary>
    /// <remarks>
    /// Meant for repositories only. It skips the rules deliberately, because the
    /// state being restored already satisfied them when it was first written.
    /// </remarks>
    /// <param name="id">The identifier of the work order.</param>
    /// <param name="number">The reference quoted to the customer.</param>
    /// <param name="customerId">The customer the job is billed to.</param>
    /// <param name="equipmentId">The equipment the job concerns.</param>
    /// <param name="technicianId">The technician responsible, if any.</param>
    /// <param name="status">The stage the job had reached.</param>
    /// <param name="resolution">The account of the work, once it was closed.</param>
    /// <param name="lines">The charges recorded against the job.</param>
    /// <returns>The restored work order.</returns>
    public static WorkOrder Restore(
        Guid id,
        string number,
        Guid customerId,
        Guid equipmentId,
        Guid? technicianId,
        WorkOrderStatus status,
        string? resolution,
        IEnumerable<WorkOrderLine> lines)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(lines);
#else
        // The helper above arrived in .NET 6; the plug-in target is older.
        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }
#endif

        var workOrder = new WorkOrder(id, number, customerId, equipmentId, status)
        {
            TechnicianId = technicianId,
            Resolution = resolution
        };

        workOrder._lines.AddRange(lines);

        return workOrder;
    }

    /// <summary>
    /// Records a charge against the job.
    /// </summary>
    /// <param name="description">The work done or the part used.</param>
    /// <param name="quantity">The number of hours or units.</param>
    /// <param name="unitPrice">The price of a single hour or unit.</param>
    /// <exception cref="DomainException">Thrown when the job has already been closed.</exception>
    public void AddLine(string description, int quantity, Money unitPrice)
    {
        EnsureNotClosed();

        _lines.Add(new WorkOrderLine(Guid.NewGuid(), description, quantity, unitPrice));
    }

    /// <summary>
    /// Hands the job to a technician and moves it to <see cref="WorkOrderStatus.Assigned"/>.
    /// </summary>
    /// <param name="technicianId">The technician taking the job.</param>
    /// <exception cref="DomainException">
    /// Thrown when no technician is given or the job has already been closed.
    /// </exception>
    public void AssignTo(Guid technicianId)
    {
        if (technicianId == Guid.Empty)
        {
            throw new DomainException("A technician is required.");
        }

        EnsureNotClosed();

        TechnicianId = technicianId;
        Status = WorkOrderStatus.Assigned;
    }

    /// <summary>
    /// Moves an assigned job to <see cref="WorkOrderStatus.InProgress"/>.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the job has not been assigned yet.</exception>
    public void StartWork()
    {
        if (Status is not WorkOrderStatus.Assigned)
        {
            throw new DomainException($"Only an assigned work order can be started, current status is {Status}.");
        }

        Status = WorkOrderStatus.InProgress;
    }

    /// <summary>
    /// Finishes the job and records what was done.
    /// </summary>
    /// <remarks>
    /// A resolution is mandatory: a closed job with no account of the work is
    /// worthless both to the customer and to whoever services the unit next.
    /// </remarks>
    /// <param name="resolution">The account of the work carried out.</param>
    /// <exception cref="DomainException">
    /// Thrown when the resolution is missing or the job is not in progress.
    /// </exception>
    public void Close(string resolution)
    {
        if (string.IsNullOrWhiteSpace(resolution))
        {
            throw new DomainException("A work order cannot be closed without a resolution.");
        }

        if (Status is not WorkOrderStatus.InProgress)
        {
            throw new DomainException($"Only an in-progress work order can be closed, current status is {Status}.");
        }

        Status = WorkOrderStatus.Closed;
        Resolution = resolution;
    }

    private void EnsureNotClosed()
    {
        if (Status is WorkOrderStatus.Closed)
        {
            throw new DomainException("A closed work order can no longer be modified.");
        }
    }

    private static string GenerateNumber() =>
        $"WO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
