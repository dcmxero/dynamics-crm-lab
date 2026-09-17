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

    [Fact]
    public void Execute_AddsUpTheLinesOfTheWorkOrder()
    {
        var workOrderId = Guid.NewGuid();
        var context = ContextWithLines(workOrderId, (2, 45m), (1, 30m));

        var target = new Entity(WorkOrder, workOrderId);
        RunPlugin(context, target, message: "Update");

        TotalWrittenFor(context, workOrderId).Should().Be(120m);
    }

    [Fact]
    public void Execute_LeavesTheTotalAtZeroWhenThereAreNoLines()
    {
        var workOrderId = Guid.NewGuid();
        var context = ContextWithLines(workOrderId);

        RunPlugin(context, new Entity(WorkOrder, workOrderId), message: "Update");

        TotalWrittenFor(context, workOrderId).Should().Be(0m);
    }

    [Fact]
    public void Execute_IgnoresMessagesAboutOtherTables()
    {
        var context = new XrmFakedContext();
        var target = new Entity("account", Guid.NewGuid());

        RunPlugin(context, target, message: "Update");

        target.Contains(TotalPrice).Should().BeFalse();
    }

    private static XrmFakedContext ContextWithLines(Guid workOrderId, params (int Quantity, decimal UnitPrice)[] lines)
    {
        var context = new XrmFakedContext();

        var rows = new List<Entity>
        {
            new(WorkOrder, workOrderId) { ["dcl_number"] = "WO-20260901-ABCDEF" }
        };

        rows.AddRange(lines.Select(line => new Entity(WorkOrderLine, Guid.NewGuid())
        {
            ["dcl_workorderid"] = new EntityReference(WorkOrder, workOrderId),
            ["dcl_description"] = "Technician labour",
            ["dcl_quantity"] = line.Quantity,
            ["dcl_unitprice"] = new Money(line.UnitPrice)
        }));

        context.Initialize(rows);

        return context;
    }

    private static void RunPlugin(XrmFakedContext context, Entity target, string message)
    {
        var pluginContext = context.GetDefaultPluginContext();
        pluginContext.MessageName = message;
        pluginContext.InputParameters = new ParameterCollection { { "Target", target } };

        context.ExecutePluginWith<WorkOrderPricingPlugin>(pluginContext);
    }

    private static decimal TotalWrittenFor(XrmFakedContext context, Guid workOrderId) =>
        context.CreateQuery(WorkOrder)
            .Single(row => row.Id == workOrderId)
            .GetAttributeValue<Money>(TotalPrice)?.Value ?? 0m;
}
