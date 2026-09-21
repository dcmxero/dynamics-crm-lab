using DynamicsCrmLab.Domain.WorkOrders;
using DynamicsCrmLab.Infrastructure.Dataverse.Repositories;
using DynamicsCrmLab.Schema;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Xunit;
using DomainMoney = DynamicsCrmLab.Domain.Common.Money;

namespace DynamicsCrmLab.Infrastructure.Tests.Dataverse;

public sealed class DataverseWorkOrderRepositoryTests
{
    private readonly FakeDataverseClient _client = new();
    private readonly DataverseWorkOrderRepository _repository;

    public DataverseWorkOrderRepositoryTests() => _repository = new DataverseWorkOrderRepository(_client);

    [Fact]
    public async Task AddAsync_WritesTheWorkOrderAndEachOfItsLines()
    {
        var workOrder = WorkOrder.Create(Guid.NewGuid(), Guid.NewGuid());
        workOrder.AddLine("Technician labour", 2, DomainMoney.Of(45m));
        workOrder.AddLine("Filter", 1, DomainMoney.Of(30m));

        await _repository.AddAsync(workOrder);

        _client.Created.Should().HaveCount(3);
        _client.Created[0].LogicalName.Should().Be(WorkOrderSchema.EntityName);
        _client.Created.Skip(1).Should().OnlyContain(e => e.LogicalName == WorkOrderLineSchema.EntityName);
    }

    [Fact]
    public async Task GetByIdAsync_RebuildsTheWorkOrderFromTheRow()
    {
        var workOrderId = Guid.NewGuid();
        _client.RetrieveResult = new Entity(WorkOrderSchema.EntityName, workOrderId)
        {
            [WorkOrderSchema.Number] = "WO-20260901-ABCDEF",
            [WorkOrderSchema.Status] = new OptionSetValue((int)WorkOrderStatus.Assigned)
        };

        var workOrder = await _repository.GetByIdAsync(workOrderId);

        workOrder.Should().NotBeNull();
        workOrder!.Number.Should().Be("WO-20260901-ABCDEF");
        workOrder.Status.Should().Be(WorkOrderStatus.Assigned);
    }

    [Fact]
    public async Task UpdateAsync_WritesOnlyTheMutableColumns()
    {
        var workOrder = WorkOrder.Create(Guid.NewGuid(), Guid.NewGuid());
        workOrder.AssignTo(Guid.NewGuid());

        await _repository.UpdateAsync(workOrder);

        _client.Updated.Should().ContainSingle();
        _client.Updated[0].Attributes.Should().NotContainKey(WorkOrderSchema.Number);
    }

    [Fact]
    public async Task ListByStatusAsync_FiltersByTheRequestedStage()
    {
        _client.EnqueuePage(WorkOrderSchema.EntityName, PageOf(rows: 1, moreRecords: false));

        var found = await _repository.ListByStatusAsync(WorkOrderStatus.New, maxCount: 10);

        found.Should().ContainSingle();
        _client.Queries[0].EntityName.Should().Be(WorkOrderSchema.EntityName);
    }

    [Fact]
    public async Task GetByIdAsync_AsksForTheColumnsItNeedsRatherThanAllOfThem()
    {
        await _repository.GetByIdAsync(Guid.NewGuid());

        _client.RetrievedColumns!.AllColumns.Should().BeFalse();
        _client.RetrievedColumns.Columns.Should().BeEquivalentTo(WorkOrderSchema.ReadColumns);
    }

    [Fact]
    public async Task ListByStatusAsync_AsksForTheColumnsItNeedsRatherThanAllOfThem()
    {
        _client.EnqueuePage(WorkOrderSchema.EntityName, PageOf(rows: 0, moreRecords: false));

        await _repository.ListByStatusAsync(WorkOrderStatus.New, maxCount: 10);

        _client.Queries[0].ColumnSet.AllColumns.Should().BeFalse();
    }

    [Fact]
    public async Task ListByStatusAsync_CarriesThePagingCookieOntoTheNextPage()
    {
        _client.EnqueuePage(WorkOrderSchema.EntityName, PageOf(rows: 1, moreRecords: true));
        _client.EnqueuePage(WorkOrderSchema.EntityName, PageOf(rows: 1, moreRecords: false));

        await _repository.ListByStatusAsync(WorkOrderStatus.New, maxCount: 10);

        var pageQueries = _client.Queries
            .Where(q => q.EntityName == WorkOrderSchema.EntityName)
            .ToList();

        pageQueries.Should().HaveCountGreaterThan(1);
        pageQueries[1].PageInfo.PageNumber.Should().Be(2);
        pageQueries[1].PageInfo.PagingCookie.Should().Be("<cookie page=\"1\" />");
    }

    [Fact]
    public async Task ListByStatusAsync_ReadsTheLinesOfEveryJobInASingleQuery()
    {
        _client.EnqueuePage(WorkOrderSchema.EntityName, PageOf(rows: 3, moreRecords: false));

        await _repository.ListByStatusAsync(WorkOrderStatus.New, maxCount: 10);

        _client.Queries
            .Count(q => q.EntityName == WorkOrderLineSchema.EntityName)
            .Should().Be(1);
    }

    [Fact]
    public async Task ListByStatusAsync_LeavesALineThatBelongsToNoJobOffEveryJob()
    {
        var page = PageOf(rows: 2, moreRecords: false);
        _client.EnqueuePage(WorkOrderSchema.EntityName, page);

        var lines = new EntityCollection();
        lines.Entities.Add(new Entity(WorkOrderLineSchema.EntityName, Guid.NewGuid())
        {
            // The lookup came back empty, so the charge belongs to nobody here.
            [WorkOrderLineSchema.Description] = "Orphan",
            [WorkOrderLineSchema.Quantity] = 1,
            [WorkOrderLineSchema.UnitPrice] = new Money(500m)
        });
        _client.EnqueuePage(WorkOrderLineSchema.EntityName, lines);

        var found = await _repository.ListByStatusAsync(WorkOrderStatus.New, maxCount: 10);

        found.Should().OnlyContain(workOrder => workOrder.Lines.Count == 0);
    }

    private static EntityCollection PageOf(int rows, bool moreRecords)
    {
        var page = new EntityCollection { MoreRecords = moreRecords, PagingCookie = "<cookie page=\"1\" />" };

        for (var index = 0; index < rows; index++)
        {
            page.Entities.Add(new Entity(WorkOrderSchema.EntityName, Guid.NewGuid())
            {
                [WorkOrderSchema.Number] = $"WO-2026090{index}-AAAAAA",
                [WorkOrderSchema.Status] = new OptionSetValue((int)WorkOrderStatus.New)
            });
        }

        return page;
    }
}
