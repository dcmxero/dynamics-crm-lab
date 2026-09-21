using DynamicsCrmLab.Infrastructure.Dataverse;
using DynamicsCrmLab.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using static DynamicsCrmLab.Provisioning.MetadataFactory;

namespace DynamicsCrmLab.Provisioning;

/// <summary>
/// Creates the tables, columns, relationships and keys the solution needs.
/// </summary>
/// <remarks>
/// Written as code rather than clicked together in the maker portal because a
/// schema that exists only as a sequence of clicks cannot be reviewed, repeated
/// on a second environment, or rebuilt after someone deletes a column.
///
/// Every step checks first, so running this twice does nothing the second time.
/// </remarks>
/// <param name="client">The Dataverse connection.</param>
/// <param name="logger">Reports what was created and what was already there.</param>
internal sealed class SchemaProvisioner(IDataverseClient client, ILogger<SchemaProvisioner> logger)
{
    private static readonly (int Value, string Label)[] WorkOrderStages =
    [
        (1, "New"),
        (2, "Assigned"),
        (3, "In progress"),
        (4, "Closed")
    ];

    /// <summary>
    /// Brings the environment up to the schema the solution expects.
    /// </summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes once everything is in place.</returns>
    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        await EnsureEquipmentAsync(cancellationToken).ConfigureAwait(false);
        await EnsureTechnicianAsync(cancellationToken).ConfigureAwait(false);
        await EnsureWorkOrderAsync(cancellationToken).ConfigureAwait(false);
        await EnsureWorkOrderLineAsync(cancellationToken).ConfigureAwait(false);
        await EnsureRelationshipsAsync(cancellationToken).ConfigureAwait(false);
        await EnsureAlternateKeyAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureEquipmentAsync(CancellationToken cancellationToken)
    {
        await EnsureTableAsync(
            EquipmentSchema.EntityName,
            Table("dcl_Equipment", "Equipment", "Equipment", "A serviceable unit installed at a customer site."),
            Text(
                "dcl_SerialNumber",
                "Serial number",
                "The manufacturer serial number identifying the unit.",
                maxLength: 100,
                required: true),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureTechnicianAsync(CancellationToken cancellationToken)
    {
        await EnsureTableAsync(
            TechnicianSchema.EntityName,
            Table("dcl_Technician", "Technician", "Technicians", "A field technician a work order can be assigned to."),
            Text("dcl_Name", "Name", "The name shown on the schedule.", maxLength: 100, required: true),
            cancellationToken).ConfigureAwait(false);

        await EnsureColumnAsync(
            TechnicianSchema.EntityName,
            YesNo(
                "dcl_IsAvailable",
                "Available",
                "Whether the technician can take on further work.",
                defaultValue: true),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureWorkOrderAsync(CancellationToken cancellationToken)
    {
        await EnsureTableAsync(
            WorkOrderSchema.EntityName,
            Table("dcl_WorkOrder", "Work order", "Work orders", "A service job raised against customer equipment."),
            Text(
                "dcl_Number",
                "Number",
                "The reference quoted to the customer.",
                maxLength: 40,
                required: true),
            cancellationToken).ConfigureAwait(false);

        await EnsureStatusAsync(cancellationToken).ConfigureAwait(false);

        await EnsureColumnAsync(
            WorkOrderSchema.EntityName,
            Memo("dcl_Resolution", "Resolution", "The account of the work carried out."),
            cancellationToken).ConfigureAwait(false);

        await EnsureColumnAsync(
            WorkOrderSchema.EntityName,
            Currency("dcl_TotalPrice", "Total price", "The amount to invoice, written by the pricing plug-in."),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureWorkOrderLineAsync(CancellationToken cancellationToken)
    {
        await EnsureTableAsync(
            WorkOrderLineSchema.EntityName,
            Table(
                "dcl_WorkOrderLine",
                "Work order line",
                "Work order lines",
                "A single charge on a work order, either labour or material."),
            Text(
                "dcl_Description",
                "Description",
                "The work done or the part used.",
                maxLength: 200,
                required: true),
            cancellationToken).ConfigureAwait(false);

        await EnsureColumnAsync(
            WorkOrderLineSchema.EntityName,
            Whole(
                "dcl_Quantity",
                "Quantity",
                "The number of hours or units charged.",
                minimum: 1,
                maximum: 10_000,
                required: true),
            cancellationToken).ConfigureAwait(false);

        await EnsureColumnAsync(
            WorkOrderLineSchema.EntityName,
            Currency("dcl_UnitPrice", "Unit price", "The price of a single hour or unit."),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureStatusAsync(CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(WorkOrderSchema.EntityName, WorkOrderSchema.Status, cancellationToken)
            .ConfigureAwait(false))
        {
            ProvisioningLog.ColumnExists(logger, WorkOrderSchema.Status, WorkOrderSchema.EntityName);

            return;
        }

        var status = Choice("dcl_Status", "Stage", "Where the job has reached in its lifecycle.", defaultValue: 1);

        // The values must line up with the domain enum, so they are stated
        // rather than left to the publisher option value prefix.
        foreach (var (value, label) in WorkOrderStages)
        {
            status.OptionSet.Options.Add(new OptionMetadata(Label(label), value));
        }

        await EnsureColumnAsync(WorkOrderSchema.EntityName, status, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureRelationshipsAsync(CancellationToken cancellationToken)
    {
        await EnsureLookupAsync(
            "dcl_contact_equipment",
            ContactSchema.EntityName,
            EquipmentSchema.EntityName,
            "dcl_CustomerId",
            "Customer",
            "The customer the unit belongs to.",
            cancellationToken).ConfigureAwait(false);

        await EnsureLookupAsync(
            "dcl_contact_workorder",
            ContactSchema.EntityName,
            WorkOrderSchema.EntityName,
            "dcl_CustomerId",
            "Customer",
            "The customer the job is billed to.",
            cancellationToken).ConfigureAwait(false);

        await EnsureLookupAsync(
            "dcl_equipment_workorder",
            EquipmentSchema.EntityName,
            WorkOrderSchema.EntityName,
            "dcl_EquipmentId",
            "Equipment",
            "The equipment the job concerns.",
            cancellationToken).ConfigureAwait(false);

        await EnsureLookupAsync(
            "dcl_technician_workorder",
            TechnicianSchema.EntityName,
            WorkOrderSchema.EntityName,
            "dcl_TechnicianId",
            "Technician",
            "The technician responsible for the job.",
            cancellationToken).ConfigureAwait(false);

        await EnsureLookupAsync(
            "dcl_workorder_workorderline",
            WorkOrderSchema.EntityName,
            WorkOrderLineSchema.EntityName,
            "dcl_WorkOrderId",
            "Work order",
            "The work order the line belongs to.",
            cancellationToken,
            // Lines have no meaning without their job, so they go with it.
            cascadeDelete: CascadeType.Cascade).ConfigureAwait(false);
    }

    private async Task EnsureAlternateKeyAsync(CancellationToken cancellationToken)
    {
        var table = await DescribeAsync(WorkOrderSchema.EntityName, cancellationToken).ConfigureAwait(false);

        if (table?.Keys?.Any(key => key.SchemaName == "dcl_WorkOrderNumber") == true)
        {
            ProvisioningLog.KeyExists(logger);

            return;
        }

        await client.ExecuteAsync(
            new CreateEntityKeyRequest
            {
                EntityName = WorkOrderSchema.EntityName,
                SolutionUniqueName = SolutionProvisioner.SolutionUniqueName,
                EntityKey = new EntityKeyMetadata
                {
                    SchemaName = "dcl_WorkOrderNumber",
                    DisplayName = Label("Work order number"),
                    KeyAttributes = [WorkOrderSchema.Number]
                }
            },
            cancellationToken).ConfigureAwait(false);

        // An alternate key lets an integration address a job by its number
        // instead of a GUID it would otherwise have to look up first.
        ProvisioningLog.KeyCreated(logger);
    }

    private async Task EnsureTableAsync(
        string logicalName,
        EntityMetadata table,
        StringAttributeMetadata primaryColumn,
        CancellationToken cancellationToken)
    {
        if (await DescribeAsync(logicalName, cancellationToken).ConfigureAwait(false) is not null)
        {
            ProvisioningLog.TableExists(logger, logicalName);

            return;
        }

        await client.ExecuteAsync(
            new CreateEntityRequest
            {
                Entity = table,
                PrimaryAttribute = primaryColumn,
                SolutionUniqueName = SolutionProvisioner.SolutionUniqueName
            },
            cancellationToken).ConfigureAwait(false);

        ProvisioningLog.TableCreated(logger, logicalName);
    }

    private async Task EnsureColumnAsync(
        string tableName,
        AttributeMetadata column,
        CancellationToken cancellationToken)
    {
        var columnName = column.SchemaName;

        if (await ColumnExistsAsync(tableName, columnName, cancellationToken).ConfigureAwait(false))
        {
            ProvisioningLog.ColumnExists(logger, columnName, tableName);

            return;
        }

        await client.ExecuteAsync(
            new CreateAttributeRequest
            {
                EntityName = tableName,
                Attribute = column,
                SolutionUniqueName = SolutionProvisioner.SolutionUniqueName
            },
            cancellationToken).ConfigureAwait(false);

        ProvisioningLog.ColumnCreated(logger, columnName, tableName);
    }

    private async Task EnsureLookupAsync(
        string relationshipName,
        string oneSide,
        string manySide,
        string lookupSchemaName,
        string display,
        string description,
        CancellationToken cancellationToken,
        CascadeType cascadeDelete = CascadeType.RemoveLink)
    {
        if (await ColumnExistsAsync(manySide, lookupSchemaName, cancellationToken).ConfigureAwait(false))
        {
            ProvisioningLog.ColumnExists(logger, lookupSchemaName, manySide);

            return;
        }

        await client.ExecuteAsync(
            new CreateOneToManyRequest
            {
                SolutionUniqueName = SolutionProvisioner.SolutionUniqueName,
                Lookup = new LookupAttributeMetadata
                {
                    SchemaName = lookupSchemaName,
                    DisplayName = Label(display),
                    Description = Label(description)
                },
                OneToManyRelationship = new OneToManyRelationshipMetadata
                {
                    SchemaName = relationshipName,
                    ReferencedEntity = oneSide,
                    ReferencingEntity = manySide,
                    CascadeConfiguration = new CascadeConfiguration
                    {
                        Assign = CascadeType.NoCascade,
                        Delete = cascadeDelete,
                        Merge = CascadeType.NoCascade,
                        Reparent = CascadeType.NoCascade,
                        Share = CascadeType.NoCascade,
                        Unshare = CascadeType.NoCascade
                    }
                }
            },
            cancellationToken).ConfigureAwait(false);

        ProvisioningLog.LookupCreated(logger, lookupSchemaName, manySide, oneSide);
    }

    private async Task<bool> ColumnExistsAsync(
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var table = await DescribeAsync(tableName, cancellationToken).ConfigureAwait(false);

        return table?.Attributes?.Any(attribute =>
            string.Equals(attribute.LogicalName, columnName, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private async Task<EntityMetadata?> DescribeAsync(string logicalName, CancellationToken cancellationToken)
    {
        try
        {
            var response = (RetrieveEntityResponse)await client.ExecuteAsync(
                new RetrieveEntityRequest
                {
                    LogicalName = logicalName,
                    EntityFilters = EntityFilters.Attributes | EntityFilters.Relationships,
                    RetrieveAsIfPublished = true
                },
                cancellationToken).ConfigureAwait(false);

            return response.EntityMetadata;
        }
        catch (System.ServiceModel.FaultException<OrganizationServiceFault>)
        {
            // The platform answers a fault rather than an empty result when the
            // table has never existed, which is the case this is asking about.
            return null;
        }
    }
}
