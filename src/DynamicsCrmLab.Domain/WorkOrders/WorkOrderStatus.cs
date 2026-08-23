namespace DynamicsCrmLab.Domain.WorkOrders;

/// <summary>
/// Represents the lifecycle stage of a work order.
/// </summary>
/// <remarks>
/// The numeric values match the Dataverse choice column so that mapping stays a
/// straight cast. The domain owns the definition; the platform follows it.
/// </remarks>
public enum WorkOrderStatus
{
    /// <summary>Raised but not yet assigned to a technician.</summary>
    New = 1,

    /// <summary>Assigned to a technician who has not started yet.</summary>
    Assigned = 2,

    /// <summary>Being worked on.</summary>
    InProgress = 3,

    /// <summary>Finished, with a resolution recorded.</summary>
    Closed = 4
}
