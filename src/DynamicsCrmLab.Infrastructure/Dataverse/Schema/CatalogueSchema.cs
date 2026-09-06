namespace DynamicsCrmLab.Infrastructure.Dataverse.Schema;

/// <summary>
/// Provides the logical names used for customers, which map to the stock
/// contact table.
/// </summary>
public static class ContactSchema
{
    /// <summary>The logical name of the table.</summary>
    public const string EntityName = "contact";

    /// <summary>The primary key column.</summary>
    public const string Id = "contactid";

    /// <summary>The display name assembled by the platform.</summary>
    public const string FullName = "fullname";

    /// <summary>The primary email address.</summary>
    public const string Email = "emailaddress1";

    /// <summary>
    /// Gets the columns needed to rebuild a customer.
    /// </summary>
    public static IReadOnlyList<string> ReadColumns { get; } = [Id, FullName, Email];
}

/// <summary>
/// Provides the logical names of the equipment table and its columns.
/// </summary>
public static class EquipmentSchema
{
    /// <summary>The logical name of the table.</summary>
    public const string EntityName = "dcl_equipment";

    /// <summary>The primary key column.</summary>
    public const string Id = "dcl_equipmentid";

    /// <summary>The manufacturer serial number identifying the unit.</summary>
    public const string SerialNumber = "dcl_serialnumber";

    /// <summary>The lookup to the customer the unit belongs to.</summary>
    public const string Customer = "dcl_customerid";

    /// <summary>
    /// Gets the columns needed to rebuild a piece of equipment.
    /// </summary>
    public static IReadOnlyList<string> ReadColumns { get; } = [Id, SerialNumber, Customer];
}

/// <summary>
/// Provides the logical names of the technician table and its columns.
/// </summary>
public static class TechnicianSchema
{
    /// <summary>The logical name of the table.</summary>
    public const string EntityName = "dcl_technician";

    /// <summary>The primary key column.</summary>
    public const string Id = "dcl_technicianid";

    /// <summary>The name shown on the schedule.</summary>
    public const string FullName = "dcl_name";

    /// <summary>Whether the technician can take on further work.</summary>
    public const string IsAvailable = "dcl_isavailable";

    /// <summary>
    /// Gets the columns needed to rebuild a technician.
    /// </summary>
    public static IReadOnlyList<string> ReadColumns { get; } = [Id, FullName, IsAvailable];
}
