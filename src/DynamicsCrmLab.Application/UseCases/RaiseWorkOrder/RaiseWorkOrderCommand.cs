namespace DynamicsCrmLab.Application.UseCases.RaiseWorkOrder;

/// <summary>
/// Represents a request to raise a work order.
/// </summary>
/// <param name="CustomerId">The customer the job is billed to.</param>
/// <param name="EquipmentId">The equipment the job concerns.</param>
/// <param name="Lines">The charges to record against the job.</param>
/// <param name="RequestKey">
/// What the caller calls this request, so that sending it twice raises one job.
/// Left out, a key is made up, which is the same as saying this request is
/// unlike any other.
/// </param>
public sealed record RaiseWorkOrderCommand(
    Guid CustomerId,
    Guid EquipmentId,
    IReadOnlyList<WorkOrderLineInput> Lines,
    string? RequestKey = null);

/// <summary>
/// Represents a single charge supplied when raising a work order.
/// </summary>
/// <param name="Description">The work done or the part used.</param>
/// <param name="Quantity">The number of hours or units.</param>
/// <param name="UnitPrice">The price of a single hour or unit.</param>
/// <param name="Currency">The three-letter ISO currency code.</param>
public sealed record WorkOrderLineInput(
    string Description,
    int Quantity,
    decimal UnitPrice,
    string Currency = "EUR");
