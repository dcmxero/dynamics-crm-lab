using DynamicsCrmLab.Infrastructure.Dataverse;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Provisioning.Tests;

/// <summary>
/// Answers queries with a fixed result and keeps the query it was given.
/// </summary>
internal sealed class StubDataverseClient(EntityCollection result) : IDataverseClient
{
    public QueryBase? Query { get; private set; }

    public Task<EntityCollection> RetrieveMultipleAsync(
        QueryBase query,
        CancellationToken cancellationToken = default)
    {
        Query = query;

        return Task.FromResult(result);
    }

    public Task<Guid> CreateAsync(Entity record, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Entity> RetrieveAsync(
        string entityName,
        Guid id,
        ColumnSet columns,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<OrganizationResponse> ExecuteAsync(
        OrganizationRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task UpdateAsync(Entity record, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
