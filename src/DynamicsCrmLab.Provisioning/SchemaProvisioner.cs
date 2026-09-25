using DynamicsCrmLab.Infrastructure.Dataverse;
using DynamicsCrmLab.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

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
/// <param name="language">Reads the language the environment accepts labels in.</param>
/// <param name="logger">Reports what was created and what was already there.</param>
internal sealed class SchemaProvisioner(
    IDataverseClient client,
    OrganizationLanguage language,
    ILogger<SchemaProvisioner> logger)
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
        var metadata = new MetadataFactory(
            await language.BaseCodeAsync(cancellationToken).ConfigureAwait(false));

        await EnsureEquipmentAsync(metadata, cancellationToken).ConfigureAwait(false);
        await EnsureTechnicianAsync(metadata, cancellationToken).ConfigureAwait(false);
        await EnsureWorkOrderAsync(metadata, cancellationToken).ConfigureAwait(false);
        await EnsureWorkOrderLineAsync(metadata, cancellationToken).ConfigureAwait(false);
        await EnsureRelationshipsAsync(metadata, cancellationToken).ConfigureAwait(false);
        await EnsureAlternateKeyAsync(metadata, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureEquipmentAsync(MetadataFactory metadata, CancellationToken cancellationToken)
    {
        await EnsureTableAsync(
            EquipmentSchema.EntityName,
            metadata.Table(
                "dcl_Equipment",
                "Equipment",
                "Equipment",
                "A serviceable unit installed at a customer site."),
            metadata.Text(
                "dcl_SerialNumber",
                "Serial number",
                "The manufacturer serial number identifying the unit.",
                maxLength: 100,
                required: true),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureTechnicianAsync(MetadataFactory metadata, CancellationToken cancellationToken)
    {
        await EnsureTableAsync(
            TechnicianSchema.EntityName,
            metadata.Table(
                "dcl_Technician",
                "Technician",
                "Technicians",
                "A field technician a work order can be assigned to."),
            metadata.Text("dcl_Name", "Name", "The name shown on the schedule.", maxLength: 100, required: true),
            cancellationToken).ConfigureAwait(false);

        await EnsureColumnAsync(
            TechnicianSchema.EntityName,
            metadata.YesNo(
                "dcl_IsAvailable",
                "Available",
                "Whether the technician can take on further work.",
                defaultValue: true),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureWorkOrderAsync(MetadataFactory metadata, CancellationToken cancellationToken)
    {
        await EnsureTableAsync(
            WorkOrderSchema.EntityName,
            metadata.Table(
                "dcl_WorkOrder",
                "Work order",
                "Work orders",
                "A service job raised against customer equipment.",
                // The plug-in raises a follow-up task against a closed job, and
                // a task can only regard a table that accepts activities.
                hasActivities: true),
            metadata.Text(
                "dcl_Number",
                "Number",
                "The reference quoted to the customer.",
                maxLength: 40,
                required: true),
            cancellationToken).ConfigureAwait(false);

        await EnsureActivitiesAsync(WorkOrderSchema.EntityName, cancellationToken).ConfigureAwait(false);

        await EnsureStatusAsync(metadata, cancellationToken).ConfigureAwait(false);

        await EnsureColumnAsync(
            WorkOrderSchema.EntityName,
            metadata.Memo("dcl_Resolution", "Resolution", "The account of the work carried out."),
            cancellationToken).ConfigureAwait(false);

        await EnsureColumnAsync(
            WorkOrderSchema.EntityName,
            metadata.Text(
                "dcl_RequestKey",
                "Request key",
                "What the caller called the request that raised this job.",
                maxLength: 100,
                required: true),
            cancellationToken).ConfigureAwait(false);

        await EnsureColumnAsync(
            WorkOrderSchema.EntityName,
            metadata.Currency(
                "dcl_TotalPrice",
                "Total price",
                "The amount to invoice, written by the pricing plug-in."),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureWorkOrderLineAsync(MetadataFactory metadata, CancellationToken cancellationToken)
    {
        await EnsureTableAsync(
            WorkOrderLineSchema.EntityName,
            metadata.Table(
                "dcl_WorkOrderLine",
                "Work order line",
                "Work order lines",
                "A single charge on a work order, either labour or material."),
            metadata.Text(
                "dcl_Description",
                "Description",
                "The work done or the part used.",
                maxLength: 200,
                required: true),
            cancellationToken).ConfigureAwait(false);

        await EnsureColumnAsync(
            WorkOrderLineSchema.EntityName,
            metadata.Whole(
                "dcl_Quantity",
                "Quantity",
                "The number of hours or units charged.",
                minimum: 1,
                maximum: 10_000,
                required: true),
            cancellationToken).ConfigureAwait(false);

        await EnsureColumnAsync(
            WorkOrderLineSchema.EntityName,
            metadata.Currency("dcl_UnitPrice", "Unit price", "The price of a single hour or unit."),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Turns activities on for a table that was created without them.
    /// </summary>
    /// <remarks>
    /// Tables created before the plug-in needed this are already in
    /// environments, and the switch can be thrown afterwards, so it is brought
    /// up to date rather than left to whoever created the table first.
    /// </remarks>
    private async Task EnsureActivitiesAsync(string logicalName, CancellationToken cancellationToken)
    {
        var table = await DescribeAsync(logicalName, cancellationToken).ConfigureAwait(false);

        if (table is null || table.HasActivities == true)
        {
            return;
        }

        table.HasActivities = true;

        await client.ExecuteAsync(
            new UpdateEntityRequest
            {
                Entity = table,
                HasActivities = true,
                SolutionUniqueName = SolutionProvisioner.SolutionUniqueName
            },
            cancellationToken).ConfigureAwait(false);

        ProvisioningLog.ActivitiesEnabled(logger, logicalName);
    }

    private async Task EnsureStatusAsync(MetadataFactory metadata, CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(WorkOrderSchema.EntityName, WorkOrderSchema.Status, cancellationToken)
            .ConfigureAwait(false))
        {
            ProvisioningLog.ColumnExists(logger, WorkOrderSchema.Status, WorkOrderSchema.EntityName);

            return;
        }

        var status = metadata.Choice(
            "dcl_Status",
            "Stage",
            "Where the job has reached in its lifecycle.",
            defaultValue: 1);

        // The values must line up with the domain enum, so they are stated
        // rather than left to the publisher option value prefix.
        foreach (var (value, label) in WorkOrderStages)
        {
            status.OptionSet.Options.Add(new OptionMetadata(metadata.Label(label), value));
        }

        await EnsureColumnAsync(WorkOrderSchema.EntityName, status, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureRelationshipsAsync(MetadataFactory metadata, CancellationToken cancellationToken)
    {
        await EnsureLookupAsync(
            metadata,
            "dcl_contact_equipment",
            ContactSchema.EntityName,
            EquipmentSchema.EntityName,
            "dcl_CustomerId",
            "Customer",
            "The customer the unit belongs to.",
            cancellationToken).ConfigureAwait(false);

        await EnsureLookupAsync(
            metadata,
            "dcl_contact_workorder",
            ContactSchema.EntityName,
            WorkOrderSchema.EntityName,
            "dcl_CustomerId",
            "Customer",
            "The customer the job is billed to.",
            cancellationToken).ConfigureAwait(false);

        await EnsureLookupAsync(
            metadata,
            "dcl_equipment_workorder",
            EquipmentSchema.EntityName,
            WorkOrderSchema.EntityName,
            "dcl_EquipmentId",
            "Equipment",
            "The equipment the job concerns.",
            cancellationToken).ConfigureAwait(false);

        await EnsureLookupAsync(
            metadata,
            "dcl_technician_workorder",
            TechnicianSchema.EntityName,
            WorkOrderSchema.EntityName,
            "dcl_TechnicianId",
            "Technician",
            "The technician responsible for the job.",
            cancellationToken).ConfigureAwait(false);

        await EnsureLookupAsync(
            metadata,
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

    private async Task EnsureAlternateKeyAsync(MetadataFactory metadata, CancellationToken cancellationToken)
    {
        // An alternate key lets an integration address a job by its number
        // instead of a GUID it would otherwise have to look up first.
        await EnsureKeyAsync(
            metadata,
            "dcl_WorkOrderNumber",
            "Work order number",
            WorkOrderSchema.Number,
            cancellationToken).ConfigureAwait(false);

        // And this one is what makes raising a job idempotent. Two requests
        // carrying the same key cannot both become a job, and the platform
        // enforces that rather than a check the second request could slip past.
        await EnsureKeyAsync(
            metadata,
            "dcl_WorkOrderRequestKey",
            "Work order request key",
            WorkOrderSchema.RequestKey,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureKeyAsync(
        MetadataFactory metadata,
        string schemaName,
        string displayName,
        string column,
        CancellationToken cancellationToken)
    {
        var table = await DescribeAsync(WorkOrderSchema.EntityName, cancellationToken).ConfigureAwait(false);

        if (table?.Keys?.Any(key => key.SchemaName == schemaName) == true)
        {
            ProvisioningLog.KeyExists(logger, schemaName);

            return;
        }

        await client.ExecuteAsync(
            new CreateEntityKeyRequest
            {
                EntityName = WorkOrderSchema.EntityName,
                SolutionUniqueName = SolutionProvisioner.SolutionUniqueName,
                EntityKey = new EntityKeyMetadata
                {
                    SchemaName = schemaName,
                    DisplayName = metadata.Label(displayName),
                    KeyAttributes = [column]
                }
            },
            cancellationToken).ConfigureAwait(false);

        ProvisioningLog.KeyCreated(logger, schemaName);
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
        MetadataFactory metadata,
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
                    DisplayName = metadata.Label(display),
                    Description = metadata.Label(description)
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
