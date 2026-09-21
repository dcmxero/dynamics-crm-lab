using System;
using System.Collections.Generic;
using System.Linq;
using DynamicsCrmLab.Plugins.WorkOrders;
using FakeXrmEasy;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DynamicsCrmLab.Plugins.Tests.WorkOrders;

public sealed class WorkOrderPricingPluginTests
{
    private const string WorkOrder = "dcl_workorder";
    private const string WorkOrderLine = "dcl_workorderline";
    private const string TotalPrice = "dcl_totalprice";
    private const string ParentLookup = "dcl_workorderid";

    [Fact]
    public void Execute_AddsUpEveryLineOfTheJobTheChangedLineBelongsTo()
    {
        var workOrderId = Guid.NewGuid();
        var context = ContextWithLines(workOrderId, (2, 45m), (1, 30m));

        RunPlugin(context, LineOf(workOrderId));

        StoredTotalFor(context, workOrderId).Should().Be(120m);
    }

    [Fact]
    public void Execute_TakesTheJobFromTheImageWhenTheLineDoesNotCarryIt()
    {
        var workOrderId = Guid.NewGuid();
        var context = ContextWithLines(workOrderId, (3, 20m));

        RunPlugin(context, new Entity(WorkOrderLine, Guid.NewGuid()), preImage: LineOf(workOrderId));

        StoredTotalFor(context, workOrderId).Should().Be(60m);
    }

    [Fact]
    public void Execute_WorksOutTheTotalForADeletedLineFromTheImageAlone()
    {
        var workOrderId = Guid.NewGuid();
        var context = ContextWithLines(workOrderId, (1, 25m));

        RunPlugin(
            context,
            new EntityReference(WorkOrderLine, Guid.NewGuid()),
            preImage: LineOf(workOrderId));

        StoredTotalFor(context, workOrderId).Should().Be(25m);
    }

    [Fact]
    public void Execute_LeavesTheTotalAtZeroWhenTheLastLineIsGone()
    {
        var workOrderId = Guid.NewGuid();
        var context = ContextWithLines(workOrderId);

        RunPlugin(
            context,
            new EntityReference(WorkOrderLine, Guid.NewGuid()),
            preImage: LineOf(workOrderId));

        StoredTotalFor(context, workOrderId).Should().Be(0m);
    }

    [Fact]
    public void Execute_RetotalsBothJobsWhenALineMovesBetweenThem()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();

        var context = new XrmFakedContext();
        context.Initialize(new List<Entity>
        {
            new(WorkOrder, from) { ["dcl_number"] = "WO-20260901-AAAAAA" },
            new(WorkOrder, to) { ["dcl_number"] = "WO-20260901-BBBBBB" },

            // The line has already moved by the time the step runs.
            new(WorkOrderLine, Guid.NewGuid())
            {
                [ParentLookup] = new EntityReference(WorkOrder, to),
                ["dcl_description"] = "Technician labour",
                ["dcl_quantity"] = 1,
                ["dcl_unitprice"] = new Money(100m)
            }
        });

        RunPlugin(context, LineOf(to), preImage: LineOf(from));

        StoredTotalFor(context, to).Should().Be(100m);
        StoredTotalFor(context, from).Should().Be(0m);
    }

    [Fact]
    public void Execute_SkipsWhenAnotherPluginTriggeredTheChange()
    {
        var workOrderId = Guid.NewGuid();
        var context = ContextWithLines(workOrderId, (1, 50m));

        RunPlugin(context, LineOf(workOrderId), depth: 2);

        StoredTotalFor(context, workOrderId).Should().BeNull();
    }

    [Fact]
    public void Execute_IgnoresALineThatBelongsToNoJob()
    {
        var workOrderId = Guid.NewGuid();
        var context = ContextWithLines(workOrderId, (1, 50m));

        RunPlugin(context, new Entity(WorkOrderLine, Guid.NewGuid()));

        StoredTotalFor(context, workOrderId).Should().BeNull();
    }

    [Fact]
    public void Execute_IgnoresMessagesAboutOtherTables()
    {
        var workOrderId = Guid.NewGuid();
        var context = ContextWithLines(workOrderId, (1, 50m));

        RunPlugin(context, new Entity("account", Guid.NewGuid()));

        StoredTotalFor(context, workOrderId).Should().BeNull();
    }

    private static Entity LineOf(Guid workOrderId) =>
        new(WorkOrderLine, Guid.NewGuid())
        {
            [ParentLookup] = new EntityReference(WorkOrder, workOrderId)
        };

    private static XrmFakedContext ContextWithLines(Guid workOrderId, params (int Quantity, decimal UnitPrice)[] lines)
    {
        var context = new XrmFakedContext();

        var rows = new List<Entity>
        {
            new(WorkOrder, workOrderId) { ["dcl_number"] = "WO-20260901-ABCDEF" }
        };

        rows.AddRange(lines.Select(line => new Entity(WorkOrderLine, Guid.NewGuid())
        {
            [ParentLookup] = new EntityReference(WorkOrder, workOrderId),
            ["dcl_description"] = "Technician labour",
            ["dcl_quantity"] = line.Quantity,
            ["dcl_unitprice"] = new Money(line.UnitPrice)
        }));

        context.Initialize(rows);

        return context;
    }

    private static void RunPlugin(
        XrmFakedContext context,
        object target,
        Entity? preImage = null,
        int depth = 1)
    {
        var pluginContext = context.GetDefaultPluginContext();
        pluginContext.MessageName = "Update";
        pluginContext.Stage = 40;
        pluginContext.Depth = depth;
        pluginContext.InputParameters = new ParameterCollection { { "Target", target } };

        if (preImage != null)
        {
            pluginContext.PreEntityImages = new EntityImageCollection { { "PreImage", preImage } };
        }

        context.ExecutePluginWith<WorkOrderPricingPlugin>(pluginContext);
    }

    private static decimal? StoredTotalFor(XrmFakedContext context, Guid workOrderId) =>
        context.CreateQuery(WorkOrder)
            .Single(row => row.Id == workOrderId)
            .GetAttributeValue<Money>(TotalPrice)?.Value;
}
