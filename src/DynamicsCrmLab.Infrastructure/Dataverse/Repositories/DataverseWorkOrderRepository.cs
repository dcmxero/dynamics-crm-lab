using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.WorkOrders;
using DynamicsCrmLab.Infrastructure.Dataverse.Mapping;
using DynamicsCrmLab.Schema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Infrastructure.Dataverse.Repositories;

/// <summary>
/// Stores work orders in Dataverse.
/// </summary>
/// <remarks>
/// The only type in the solution that knows work orders live in Dataverse.
/// </remarks>
/// <param name="client">The connection rows are read from and written to.</param>
public sealed class DataverseWorkOrderRepository(IDataverseClient client) : IWorkOrderRepository
{
    /// <summary>
    /// Dataverse returns at most this many rows in one response, whatever the
    /// caller asks for.
    /// </summary>
    private const int MaxPageSize = 5000;

    /// <inheritdoc/>
    public async Task<Guid> AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workOrder);

        var id = await client
            .CreateAsync(WorkOrderMapper.ToRecord(workOrder), cancellationToken)
            .ConfigureAwait(false);

        foreach (var line in workOrder.Lines)
        {
            await client
                .CreateAsync(WorkOrderMapper.ToLineRecord(line, id), cancellationToken)
                .ConfigureAwait(false);
        }

        return id;
    }

    /// <inheritdoc/>
    public async Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await client
            .RetrieveAsync(
                WorkOrderSchema.EntityName,
                id,
                new ColumnSet([.. WorkOrderSchema.ReadColumns]),
                cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            return null;
        }

        var lines = await ReadLinesAsync(id, cancellationToken).ConfigureAwait(false);

        return WorkOrderMapper.ToDomain(record, lines);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workOrder);

        return client.UpdateAsync(WorkOrderMapper.ToUpdateRecord(workOrder), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkOrder>> ListByStatusAsync(
        WorkOrderStatus status,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(WorkOrderSchema.EntityName)
        {
            ColumnSet = new ColumnSet([.. WorkOrderSchema.ReadColumns]),
            Criteria = new FilterExpression
            {
                Conditions = { new ConditionExpression(WorkOrderSchema.Status, ConditionOperator.Equal, (int)status) }
            },
            Orders = { new OrderExpression("createdon", OrderType.Descending) },
            PageInfo = new PagingInfo { Count = Math.Min(maxCount, MaxPageSize), PageNumber = 1 }
        };

        var records = new List<Entity>();

        while (records.Count < maxCount)
        {
            var page = await client.RetrieveMultipleAsync(query, cancellationToken).ConfigureAwait(false);

            records.AddRange(page.Entities);

            if (!page.MoreRecords)
            {
                break;
            }

            query.PageInfo.PageNumber++;

            // Without the cookie Dataverse restarts the scan, which silently
            // returns duplicates and skips rows on large result sets.
            query.PageInfo.PagingCookie = page.PagingCookie;
        }

        var wanted = records.Take(maxCount).ToList();

        // One query for every line of every job on the page, rather than one
        // query per job. The latter costs a round trip per row and runs into
        // the service protection limits on any realistic result set.
        var linesByWorkOrder = await ReadLinesAsync(
            wanted.Select(record => record.Id).ToList(),
            cancellationToken).ConfigureAwait(false);

        return
        [
            .. wanted.Select(record => WorkOrderMapper.ToDomain(
                record,
                linesByWorkOrder.TryGetValue(record.Id, out var lines) ? lines : []))
        ];
    }

    private async Task<IReadOnlyList<Entity>> ReadLinesAsync(Guid workOrderId, CancellationToken cancellationToken)
    {
        var lines = await ReadLinesAsync([workOrderId], cancellationToken).ConfigureAwait(false);

        return lines.TryGetValue(workOrderId, out var found) ? found : [];
    }

    private async Task<Dictionary<Guid, List<Entity>>> ReadLinesAsync(
        List<Guid> workOrderIds,
        CancellationToken cancellationToken)
    {
        if (workOrderIds.Count is 0)
        {
            return [];
        }

        var query = new QueryExpression(WorkOrderLineSchema.EntityName)
        {
            ColumnSet = new ColumnSet([.. WorkOrderLineSchema.ReadColumns]),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression(
                        WorkOrderLineSchema.WorkOrder,
                        ConditionOperator.In,
                        [.. workOrderIds.Cast<object>()])
                }
            }
        };

        var page = await client.RetrieveMultipleAsync(query, cancellationToken).ConfigureAwait(false);

        var byWorkOrder = new Dictionary<Guid, List<Entity>>();

        foreach (var record in page.Entities)
        {
            var owner = record.GetAttributeValue<EntityReference>(WorkOrderLineSchema.WorkOrder)?.Id
                        ?? workOrderIds[0];

            if (!byWorkOrder.TryGetValue(owner, out var lines))
            {
                lines = [];
                byWorkOrder[owner] = lines;
            }

            lines.Add(record);
        }

        return byWorkOrder;
    }
}
