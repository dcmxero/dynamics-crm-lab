using DynamicsCrmLab.Infrastructure.Dataverse;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Infrastructure.Tests.Dataverse;

/// <summary>
/// Stands in for Dataverse and records what the repository asked for.
/// </summary>
internal sealed class FakeDataverseClient : IDataverseClient
{
    private readonly Dictionary<string, Queue<EntityCollection>> _pages = [];

    public List<Entity> Created { get; } = [];

    public List<Entity> Updated { get; } = [];

    public List<QueryExpression> Queries { get; } = [];

    public ColumnSet? RetrievedColumns { get; private set; }

    public Entity? RetrieveResult { get; set; }

    public void EnqueuePage(string entityName, EntityCollection page)
    {
        if (!_pages.TryGetValue(entityName, out var queue))
        {
            queue = new Queue<EntityCollection>();
            _pages[entityName] = queue;
        }

        queue.Enqueue(page);
    }

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
        if (query is not QueryExpression expression)
        {
            return Task.FromResult(new EntityCollection());
        }

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

        var queued = _pages.TryGetValue(expression.EntityName, out var queue) && queue.Count > 0
            ? queue.Dequeue()
            : new EntityCollection();

        return Task.FromResult(queued);
    }

    public Task UpdateAsync(Entity record, CancellationToken cancellationToken = default)
    {
        Updated.Add(record);
        return Task.CompletedTask;
    }
}
