using DynamicsCrmLab.Infrastructure.Dataverse;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Provisioning;

/// <summary>
/// Creates the publisher and the solution everything else is added to.
/// </summary>
/// <remarks>
/// The publisher prefix cannot be changed later without rebuilding every
/// component that carries it, which is why it is fixed here rather than chosen
/// in a dialog each time someone sets up an environment.
/// </remarks>
/// <param name="client">The Dataverse connection.</param>
/// <param name="logger">Reports what was created and what was already there.</param>
internal sealed class SolutionProvisioner(IDataverseClient client, ILogger<SolutionProvisioner> logger)
{
    public const string PublisherPrefix = "dcl";
    public const string PublisherUniqueName = "dynamicscrmlab";
    public const string SolutionUniqueName = "DynamicsCrmLab";

    /// <summary>Option value prefix, which must match the publisher record.</summary>
    public const int OptionValuePrefix = 10_000;

    /// <summary>
    /// Makes sure the publisher and the solution exist.
    /// </summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes once both are in place.</returns>
    public async Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        var publisherId = await EnsurePublisherAsync(cancellationToken).ConfigureAwait(false);

        await EnsureSolutionAsync(publisherId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Guid> EnsurePublisherAsync(CancellationToken cancellationToken)
    {
        var existing = await FindAsync(
            "publisher",
            "uniquename",
            PublisherUniqueName,
            cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            ProvisioningLog.PublisherExists(logger, PublisherUniqueName);

            return existing.Value;
        }

        var id = await client.CreateAsync(
            new Entity("publisher")
            {
                ["uniquename"] = PublisherUniqueName,
                ["friendlyname"] = "Dynamics CRM Lab",
                ["customizationprefix"] = PublisherPrefix,
                ["customizationoptionvalueprefix"] = OptionValuePrefix
            },
            cancellationToken).ConfigureAwait(false);

        ProvisioningLog.PublisherCreated(logger, PublisherUniqueName, PublisherPrefix);

        return id;
    }

    private async Task EnsureSolutionAsync(Guid publisherId, CancellationToken cancellationToken)
    {
        var existing = await FindAsync(
            "solution",
            "uniquename",
            SolutionUniqueName,
            cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            ProvisioningLog.SolutionExists(logger, SolutionUniqueName);

            return;
        }

        await client.CreateAsync(
            new Entity("solution")
            {
                ["uniquename"] = SolutionUniqueName,
                ["friendlyname"] = "Dynamics CRM Lab",
                ["version"] = "1.0.0.0",
                ["publisherid"] = new EntityReference("publisher", publisherId)
            },
            cancellationToken).ConfigureAwait(false);

        ProvisioningLog.SolutionCreated(logger, SolutionUniqueName);
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

    /// <summary>
    /// Publishes every pending customization.
    /// </summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes once the platform has published.</returns>
    public async Task PublishAsync(CancellationToken cancellationToken = default)
    {
        ProvisioningLog.Publishing(logger);

        await client.ExecuteAsync(new PublishAllXmlRequest(), cancellationToken).ConfigureAwait(false);
    }
}
