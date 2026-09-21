using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.WorkOrders;
using DynamicsCrmLab.Infrastructure.Dataverse.Mapping;
using DynamicsCrmLab.Schema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Infrastructure.Dataverse.Repositories;

/// <summary>
/// Stores work orders in Dataverse.
/// </summary>
/// <remarks>
/// The only type in the solution that knows work orders live in Dataverse.
/// </remarks>
/// <param name="client">The connection rows are read from and written to.</param>
/// <param name="currencies">The currencies the environment keeps money in.</param>
public sealed class DataverseWorkOrderRepository(IDataverseClient client, DataverseCurrencies currencies)
    : IWorkOrderRepository
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

        var (currencyId, _) = await currencies.BaseAsync(cancellationToken).ConfigureAwait(false);

        // A job and its charges are one thing. Created one request at a time, a
        // charge the platform refuses - a description past the length of the
        // column, a quantity past its limit - would leave a job standing that
        // is missing what it costs, and a plug-in would have totalled it.
        var writes = new OrganizationRequestCollection
        {
            new CreateRequest { Target = WorkOrderMapper.ToRecord(workOrder, currencyId) }
        };

        foreach (var line in workOrder.Lines)
        {
            writes.Add(new CreateRequest
            {
                Target = WorkOrderMapper.ToLineRecord(line, workOrder.Id, currencyId)
            });
        }

        await client.ExecuteAsync(
            new ExecuteTransactionRequest { Requests = writes, ReturnResponses = false },
            cancellationToken).ConfigureAwait(false);

        return workOrder.Id;
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
        var currency = await CurrencyOfAsync(record, cancellationToken).ConfigureAwait(false);

        return WorkOrderMapper.ToDomain(record, lines, currency);
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

        var restored = new List<WorkOrder>(wanted.Count);

        foreach (var record in wanted)
        {
            var currency = await CurrencyOfAsync(record, cancellationToken).ConfigureAwait(false);

            restored.Add(WorkOrderMapper.ToDomain(
                record,
                linesByWorkOrder.TryGetValue(record.Id, out var lines) ? lines : [],
                currency));
        }

        return restored;
    }

    /// <summary>
    /// Reads the currency a job holds its money in.
    /// </summary>
    /// <remarks>
    /// A row written before the currency was stated carries none, and the
    /// platform treats such an amount as being in the base currency, so that is
    /// what it is reported as.
    /// </remarks>
    private async Task<string> CurrencyOfAsync(Entity record, CancellationToken cancellationToken)
    {
        if (record.GetAttributeValue<EntityReference>(WorkOrderSchema.Currency) is { } currency)
        {
            return await currencies.CodeOfAsync(currency.Id, cancellationToken).ConfigureAwait(false);
        }

        var (_, code) = await currencies.BaseAsync(cancellationToken).ConfigureAwait(false);

        return code;
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
            // A line whose lookup came back empty belongs to no job here.
            // Charging it to the first one in the page would put somebody
            // else's money on an unrelated invoice.
            if (record.GetAttributeValue<EntityReference>(WorkOrderLineSchema.WorkOrder)?.Id is not { } owner)
            {
                continue;
            }

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
