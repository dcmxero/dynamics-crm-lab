using DynamicsCrmLab.Domain.WorkOrders;
using DynamicsCrmLab.Infrastructure.Dataverse.Repositories;
using DynamicsCrmLab.Infrastructure.Dataverse.Schema;
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
        _client.EnqueuePage(PageOf(rows: 1, moreRecords: false));

        var found = await _repository.ListByStatusAsync(WorkOrderStatus.New, maxCount: 10);

        found.Should().ContainSingle();
        _client.Queries[0].EntityName.Should().Be(WorkOrderSchema.EntityName);
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
