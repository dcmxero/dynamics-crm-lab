using DynamicsCrmLab.Infrastructure.Dataverse;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Infrastructure.Tests.Dataverse;

/// <summary>
/// Stands in for Dataverse and records what the repository asked for.
/// </summary>
internal sealed class FakeDataverseClient : IDataverseClient
{
    private readonly Queue<EntityCollection> _pages = new();

    public List<Entity> Created { get; } = [];

    public List<Entity> Updated { get; } = [];

    public List<QueryExpression> Queries { get; } = [];

    public ColumnSet? RetrievedColumns { get; private set; }

    public Entity? RetrieveResult { get; set; }

    public void EnqueuePage(EntityCollection page) => _pages.Enqueue(page);

    public Task<Guid> CreateAsync(Entity record, CancellationToken cancellationToken = default)
    {
        Created.Add(record);
        return Task.FromResult(record.Id == Guid.Empty ? Guid.NewGuid() : record.Id);
    }

    public Task<Entity> RetrieveAsync(
        string entityName,
        Guid id,
        ColumnSet columns,
        CancellationToken cancellationToken = default)
    {
        RetrievedColumns = columns;
        return Task.FromResult(RetrieveResult ?? new Entity(entityName, id));
    }

    public Task<EntityCollection> RetrieveMultipleAsync(
        QueryBase query,
        CancellationToken cancellationToken = default)
    {
        if (query is QueryExpression expression)
        {
            // The repository mutates one query object between pages, so the page
            // number and cookie have to be captured as they were at call time.
            Queries.Add(new QueryExpression(expression.EntityName)
            {
                ColumnSet = expression.ColumnSet,
                PageInfo = new PagingInfo
                {
                    Count = expression.PageInfo.Count,
                    PageNumber = expression.PageInfo.PageNumber,
                    PagingCookie = expression.PageInfo.PagingCookie
                }
            });
        }

        return Task.FromResult(_pages.Count > 0 ? _pages.Dequeue() : new EntityCollection());
    }

    public Task UpdateAsync(Entity record, CancellationToken cancellationToken = default)
    {
        Updated.Add(record);
        return Task.CompletedTask;
    }
}
