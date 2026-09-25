namespace DynamicsCrmLab.Schema;

/// <summary>
/// Provides the logical names of the work order table and its columns.
/// </summary>
/// <remarks>
/// Dataverse addresses columns by name, so a typo would only surface at run
/// time. Collecting the names here makes a schema change a one-file edit.
/// The <c>dcl_</c> prefix is the publisher prefix of the solution.
/// </remarks>
public static class WorkOrderSchema
{
    /// <summary>The logical name of the table.</summary>
    public const string EntityName = "dcl_workorder";

    /// <summary>The primary key column.</summary>
    public const string Id = "dcl_workorderid";

    /// <summary>The reference quoted to the customer.</summary>
    public const string Number = "dcl_number";

    /// <summary>The lookup to the customer the job is billed to.</summary>
    public const string Customer = "dcl_customerid";

    /// <summary>The lookup to the equipment the job concerns.</summary>
    public const string Equipment = "dcl_equipmentid";

    /// <summary>The lookup to the technician responsible for the job.</summary>
    public const string Technician = "dcl_technicianid";

    /// <summary>The choice column holding the lifecycle stage.</summary>
    public const string Status = "dcl_status";

    /// <summary>The account of the work carried out.</summary>
    public const string Resolution = "dcl_resolution";

    /// <summary>The currency the total is held in.</summary>
    public const string Currency = "transactioncurrencyid";

    /// <summary>The amount to invoice, written by the pricing plug-in.</summary>
    public const string TotalPrice = "dcl_totalprice";

    /// <summary>
    /// The row version the platform stamps on every change, used to tell a
    /// write that lost a race from one that did not.
    /// </summary>
    public const string RowVersion = "versionnumber";

    /// <summary>
    /// Gets the columns needed to rebuild the aggregate.
    /// </summary>
    public static IReadOnlyList<string> ReadColumns { get; } =
        [Id, Number, Customer, Equipment, Technician, Status, Resolution, Currency, RowVersion];
}

/// <summary>
/// Provides the logical names of the work order line table and its columns.
/// </summary>
public static class WorkOrderLineSchema
{
    /// <summary>The logical name of the table.</summary>
    public const string EntityName = "dcl_workorderline";

    /// <summary>The primary key column.</summary>
    public const string Id = "dcl_workorderlineid";

    /// <summary>The lookup to the work order the line belongs to.</summary>
    public const string WorkOrder = "dcl_workorderid";

    /// <summary>The work done or the part used.</summary>
    public const string Description = "dcl_description";

    /// <summary>The number of hours or units charged.</summary>
    public const string Quantity = "dcl_quantity";

    /// <summary>The price of a single hour or unit.</summary>
    public const string UnitPrice = "dcl_unitprice";

    /// <summary>The currency the price is held in.</summary>
    public const string Currency = "transactioncurrencyid";

    /// <summary>
    /// Gets the columns needed to rebuild a line.
    /// </summary>
    public static IReadOnlyList<string> ReadColumns { get; } =
        [Id, WorkOrder, Description, Quantity, UnitPrice, Currency];
}
