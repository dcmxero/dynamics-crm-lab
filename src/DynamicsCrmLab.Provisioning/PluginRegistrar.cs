using DynamicsCrmLab.Infrastructure.Dataverse;
using DynamicsCrmLab.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Provisioning;

/// <summary>
/// Uploads the plug-in package and registers the steps that run it.
/// </summary>
/// <remarks>
/// The registration tool does this through a window, which leaves no record of
/// what was set and no way to repeat it. Registering from code puts the stage,
/// the mode, the filtering attributes and the images under review alongside the
/// plug-in they belong to.
///
/// A package rather than a bare assembly because the sandbox loads the one
/// assembly it is given and nothing beside it, and these plug-ins share the
/// domain with the rest of the solution.
///
/// Every step checks first, so running this twice only refreshes the package.
/// </remarks>
/// <param name="client">The Dataverse connection.</param>
/// <param name="logger">Reports what was registered and what was already there.</param>
internal sealed class PluginRegistrar(IDataverseClient client, ILogger<PluginRegistrar> logger)
{
    private const string AssemblyName = "DynamicsCrmLab.Plugins";

    /// <summary>The name the package is stored under, carrying the publisher prefix.</summary>
    private const string PackageUniqueName = $"{SolutionProvisioner.PublisherPrefix}_{AssemblyName}";

    /// <summary>The version the package is published under, matching the build.</summary>
    private const string PackageVersion = "1.0.0";

    /// <summary>Captures the record as it was before the write.</summary>
    private const int PreImageType = 0;

    private static readonly PluginStep[] Steps =
    [
        // A create has nothing to take an image of, and the line it creates
        // carries the job it belongs to anyway.
        new(
            $"{AssemblyName}.WorkOrders.WorkOrderPricingPlugin",
            "Create",
            WorkOrderLineSchema.EntityName,
            PipelineStage.PostOperation,
            ExecutionMode.Synchronous),
        new(
            $"{AssemblyName}.WorkOrders.WorkOrderPricingPlugin",
            "Update",
            WorkOrderLineSchema.EntityName,
            PipelineStage.PostOperation,
            ExecutionMode.Synchronous,
            PreImageAttributes: [WorkOrderLineSchema.WorkOrder]),
        new(
            $"{AssemblyName}.WorkOrders.WorkOrderPricingPlugin",
            "Delete",
            WorkOrderLineSchema.EntityName,
            PipelineStage.PostOperation,
            ExecutionMode.Synchronous,
            PreImageAttributes: [WorkOrderLineSchema.WorkOrder]),
        new(
            $"{AssemblyName}.WorkOrders.WorkOrderClosedNotificationPlugin",
            "Update",
            WorkOrderSchema.EntityName,
            PipelineStage.PostOperation,
            ExecutionMode.Asynchronous,
            FilteringAttributes: [WorkOrderSchema.Status],
            PreImageAttributes: [WorkOrderSchema.Status, WorkOrderSchema.Number])
    ];

    /// <summary>
    /// Brings the plug-in package and its steps up to date.
    /// </summary>
    /// <param name="packagePath">The built plug-in package to upload.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes once every step is registered.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the package has not been built, when the platform did not
    /// read a plug-in out of it, or when it does not know a message a step
    /// asks for.
    /// </exception>
    public async Task RegisterAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(packagePath))
        {
            throw new InvalidOperationException(
                $"The plug-in package is not at {packagePath}. Build {AssemblyName} in Release first.");
        }

        await EnsurePackageAsync(packagePath, cancellationToken).ConfigureAwait(false);

        foreach (var typeName in Steps.Select(step => step.TypeName).Distinct(StringComparer.Ordinal))
        {
            // Importing the package is what creates the plug-in types; they are
            // read back rather than written.
            var typeId = await TypeIdAsync(typeName, cancellationToken).ConfigureAwait(false);

            foreach (var step in Steps.Where(step => step.TypeName == typeName))
            {
                await EnsureStepAsync(typeId, step, cancellationToken).ConfigureAwait(false);
            }

            await RemoveUndeclaredStepsAsync(typeId, typeName, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Removes steps of a plug-in that the code no longer declares.
    /// </summary>
    /// <remarks>
    /// Without this a step that has been moved to another message or table
    /// stays behind and keeps running, which is the failure the registration
    /// tool makes easy: nobody remembers what is registered.
    /// </remarks>
    private async Task RemoveUndeclaredStepsAsync(
        Guid typeId,
        string typeName,
        CancellationToken cancellationToken)
    {
        var declared = Steps
            .Where(step => step.TypeName == typeName)
            .Select(step => step.Name)
            .ToHashSet(StringComparer.Ordinal);

        var query = new QueryExpression("sdkmessageprocessingstep")
        {
            ColumnSet = new ColumnSet("name"),
            Criteria = new FilterExpression
            {
                Conditions = { new ConditionExpression("eventhandler", ConditionOperator.Equal, typeId) }
            }
        };

        var registered = await client.RetrieveMultipleAsync(query, cancellationToken).ConfigureAwait(false);

        foreach (var step in registered.Entities)
        {
            var name = step.GetAttributeValue<string>("name") ?? string.Empty;

            if (declared.Contains(name))
            {
                continue;
            }

            await client.DeleteAsync("sdkmessageprocessingstep", step.Id, cancellationToken).ConfigureAwait(false);

            ProvisioningLog.StepRemoved(logger, name);
        }
    }

    private async Task EnsurePackageAsync(string packagePath, CancellationToken cancellationToken)
    {
        var content = Convert.ToBase64String(
            await File.ReadAllBytesAsync(packagePath, cancellationToken).ConfigureAwait(false));

        var record = new Entity("pluginpackage")
        {
            ["uniquename"] = PackageUniqueName,
            // The platform reads the package file name out of this, and refuses
            // a name that does not start with the publisher prefix.
            ["name"] = PackageUniqueName,
            ["content"] = content,
            ["version"] = PackageVersion
        };

        var existing = await FindAsync("pluginpackage", "uniquename", PackageUniqueName, cancellationToken)
            .ConfigureAwait(false);

        if (existing is { } packageId)
        {
            // Uploading the content again is how a changed plug-in reaches the
            // platform; the steps around it stay as they are.
            record.Id = packageId;
            await client.UpdateAsync(record, cancellationToken).ConfigureAwait(false);

            ProvisioningLog.PackageUpdated(logger, PackageUniqueName);

            return;
        }

        await client.CreateAsync(record, cancellationToken).ConfigureAwait(false);

        ProvisioningLog.PackageRegistered(logger, PackageUniqueName);
    }

    private async Task<Guid> TypeIdAsync(string typeName, CancellationToken cancellationToken) =>
        await FindAsync("plugintype", "typename", typeName, cancellationToken).ConfigureAwait(false)
        ?? throw new InvalidOperationException(
            $"The platform did not read {typeName} out of the package.");

    private async Task EnsureStepAsync(Guid typeId, PluginStep step, CancellationToken cancellationToken)
    {
        var existing = await FindAsync(
            "sdkmessageprocessingstep",
            "name",
            step.Name,
            cancellationToken).ConfigureAwait(false);

        if (existing is { } stepId)
        {
            ProvisioningLog.StepExists(logger, step.Name);

            await EnsurePreImageAsync(stepId, step, cancellationToken).ConfigureAwait(false);

            return;
        }

        var messageId = await MessageIdAsync(step.Message, cancellationToken).ConfigureAwait(false);
        var filterId = await MessageFilterIdAsync(messageId, step.EntityName, cancellationToken)
            .ConfigureAwait(false);

        var record = new Entity("sdkmessageprocessingstep")
        {
            ["name"] = step.Name,
            ["eventhandler"] = new EntityReference("plugintype", typeId),
            ["sdkmessageid"] = new EntityReference("sdkmessage", messageId),
            ["sdkmessagefilterid"] = new EntityReference("sdkmessagefilter", filterId),
            ["stage"] = new OptionSetValue((int)step.Stage),
            ["mode"] = new OptionSetValue((int)step.Mode),
            ["rank"] = 1,
            ["supporteddeployment"] = new OptionSetValue(0),
            ["invocationsource"] = new OptionSetValue(0)
        };

        if (step.FilteringAttributes is { Length: > 0 } filtering)
        {
            // Without this the step runs on every save of the table, including
            // saves that change nothing it cares about.
            record["filteringattributes"] = string.Join(",", filtering);
        }

        var created = await client.CreateAsync(record, cancellationToken).ConfigureAwait(false);

        ProvisioningLog.StepRegistered(logger, step.Name, step.Stage, step.Mode);

        await EnsurePreImageAsync(created, step, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsurePreImageAsync(Guid stepId, PluginStep step, CancellationToken cancellationToken)
    {
        if (step.PreImageAttributes is not { Length: > 0 } columns)
        {
            return;
        }

        var query = new QueryExpression("sdkmessageprocessingstepimage")
        {
            ColumnSet = new ColumnSet(false),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression(
                        "sdkmessageprocessingstepid",
                        ConditionOperator.Equal,
                        stepId)
                }
            },
            TopCount = 1
        };

        var found = await client.RetrieveMultipleAsync(query, cancellationToken).ConfigureAwait(false);

        if (found.Entities.Count > 0)
        {
            ProvisioningLog.ImageExists(logger, step.Name);

            return;
        }

        await client.CreateAsync(
            new Entity("sdkmessageprocessingstepimage")
            {
                ["sdkmessageprocessingstepid"] =
                    new EntityReference("sdkmessageprocessingstep", stepId),
                ["imagetype"] = new OptionSetValue(PreImageType),
                ["name"] = "PreImage",
                ["entityalias"] = "PreImage",
                ["attributes"] = string.Join(",", columns),
                ["messagepropertyname"] = "Target"
            },
            cancellationToken).ConfigureAwait(false);

        ProvisioningLog.ImageRegistered(logger, step.Name);
    }

    private async Task<Guid> MessageIdAsync(string message, CancellationToken cancellationToken) =>
        await FindAsync("sdkmessage", "name", message, cancellationToken).ConfigureAwait(false)
        ?? throw new InvalidOperationException($"The platform does not know the message {message}.");

    private async Task<Guid> MessageFilterIdAsync(
        Guid messageId,
        string entityName,
        CancellationToken cancellationToken)
    {
        var query = new QueryExpression("sdkmessagefilter")
        {
            ColumnSet = new ColumnSet(false),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("sdkmessageid", ConditionOperator.Equal, messageId),
                    new ConditionExpression("primaryobjecttypecode", ConditionOperator.Equal, entityName)
                }
            },
            TopCount = 1
        };

        var found = await client.RetrieveMultipleAsync(query, cancellationToken).ConfigureAwait(false);

        return found.Entities.Count is 0
            ? throw new InvalidOperationException(
                $"The platform has no filter for that message on {entityName}.")
            : found.Entities[0].Id;
    }

    private async Task<Guid?> FindAsync(
        string entityName,
        string column,
        string value,
        CancellationToken cancellationToken)
    {
        var query = new QueryExpression(entityName)
        {
            ColumnSet = new ColumnSet(false),
            Criteria = new FilterExpression
            {
                Conditions = { new ConditionExpression(column, ConditionOperator.Equal, value) }
            },
            TopCount = 1
        };

        var found = await client.RetrieveMultipleAsync(query, cancellationToken).ConfigureAwait(false);

        return found.Entities.Count is 0 ? null : found.Entities[0].Id;
    }
}
