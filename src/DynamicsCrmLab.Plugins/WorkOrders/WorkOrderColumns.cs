namespace DynamicsCrmLab.Plugins.WorkOrders;

/// <summary>
/// Provides the logical names the work order plug-ins read and write.
/// </summary>
internal static class WorkOrderColumns
{
    /// <summary>The logical name of the work order table.</summary>
    public const string EntityName = "dcl_workorder";

    /// <summary>The choice column holding the lifecycle stage.</summary>
    public const string Status = "dcl_status";

    /// <summary>The reference quoted to the customer.</summary>
    public const string Number = "dcl_number";

    /// <summary>The amount to invoice, written by the pricing plug-in.</summary>
    public const string TotalPrice = "dcl_totalprice";
}

/// <summary>
/// Provides the logical names of the work order line table.
/// </summary>
internal static class WorkOrderLineColumns
{
    /// <summary>The logical name of the line table.</summary>
    public const string EntityName = "dcl_workorderline";

    /// <summary>The lookup to the work order the line belongs to.</summary>
    public const string WorkOrder = "dcl_workorderid";

    /// <summary>The work done or the part used.</summary>
    public const string Description = "dcl_description";

    /// <summary>The number of hours or units charged.</summary>
    public const string Quantity = "dcl_quantity";

    /// <summary>The price of a single hour or unit.</summary>
    public const string UnitPrice = "dcl_unitprice";
}
