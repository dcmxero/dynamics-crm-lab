using System;
using System.Collections.Generic;
using DynamicsCrmLab.Plugins.WorkOrders;
using FakeXrmEasy;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DynamicsCrmLab.Plugins.Tests.WorkOrders;

public sealed class ClosedWorkOrderLinesPluginTests
{
    private const string WorkOrder = "dcl_workorder";
    private const string WorkOrderLine = "dcl_workorderline";
    private const string ParentLookup = "dcl_workorderid";

    private const int InProgress = 3;
    private const int Closed = 4;

    [Fact]
    public void Execute_RefusesAChargeAddedToAClosedJob()
    {
        var adding = () => RunPlugin(Closed, line => line, message: "Create");

        adding.Should().Throw<InvalidPluginExecutionException>()
            .WithMessage("*is closed*");
    }

    [Fact]
    public void Execute_RefusesAChargeTakenOffAClosedJob()
    {
        var workOrderId = Guid.NewGuid();
        var context = ContextWith(workOrderId, Closed);

        var deleting = () => Run(
            context,
            new EntityReference(WorkOrderLine, Guid.NewGuid()),
            LineOf(workOrderId),
            "Delete");

        deleting.Should().Throw<InvalidPluginExecutionException>()
            .WithMessage("*is closed*");
    }

    [Fact]
    public void Execute_RefusesAChargeMovedOffAClosedJob()
    {
        // The charge is moving to a job that is open, so asking only about where
        // it lands finds nothing wrong. The job it leaves is finished, and it
        // would quietly end up cheaper than what the customer was told.
        var closed = Guid.NewGuid();
        var open = Guid.NewGuid();
        var context = ContextWith(closed, Closed, open, InProgress);

        var moving = () => Run(
            context,
            new Entity(WorkOrderLine, Guid.NewGuid())
            {
                [ParentLookup] = new EntityReference(WorkOrder, open)
            },
            LineOf(closed),
            "Update");

        moving.Should().Throw<InvalidPluginExecutionException>()
            .WithMessage("*is closed*");
    }

    [Fact]
    public void Execute_RefusesAChargeMovedOntoAClosedJob()
    {
        var open = Guid.NewGuid();
        var closed = Guid.NewGuid();
        var context = ContextWith(open, InProgress, closed, Closed);

        var moving = () => Run(
            context,
            new Entity(WorkOrderLine, Guid.NewGuid())
            {
                [ParentLookup] = new EntityReference(WorkOrder, closed)
            },
            LineOf(open),
            "Update");

        moving.Should().Throw<InvalidPluginExecutionException>()
            .WithMessage("*is closed*");
    }

    [Fact]
    public void Execute_AllowsAChargeMovedBetweenTwoJobsThatAreStillRunning()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();
        var context = ContextWith(from, InProgress, to, InProgress);

        var moving = () => Run(
            context,
            new Entity(WorkOrderLine, Guid.NewGuid())
            {
                [ParentLookup] = new EntityReference(WorkOrder, to)
            },
            LineOf(from),
            "Update");

        moving.Should().NotThrow();
    }

    [Fact]
    public void Execute_LeavesAJobThatIsStillRunningAlone()
    {
        var adding = () => RunPlugin(InProgress, line => line, message: "Create");

        adding.Should().NotThrow();
    }

    [Fact]
    public void Execute_IgnoresMessagesAboutOtherTables()
    {
        var context = new XrmFakedContext();

        var running = () => Run(context, new Entity("account", Guid.NewGuid()), null, "Create");

        running.Should().NotThrow();
    }

    private static void RunPlugin(int status, Func<Entity, Entity> shape, string message)
    {
        var workOrderId = Guid.NewGuid();
        var context = ContextWith(workOrderId, status);

        Run(context, shape(LineOf(workOrderId)), null, message);
    }

    private static Entity LineOf(Guid workOrderId) =>
        new(WorkOrderLine, Guid.NewGuid())
        {
            [ParentLookup] = new EntityReference(WorkOrder, workOrderId)
        };

    private static XrmFakedContext ContextWith(Guid workOrderId, int status)
    {
        var context = new XrmFakedContext();

        context.Initialize(new List<Entity> { JobOf(workOrderId, status) });

        return context;
    }

    private static XrmFakedContext ContextWith(Guid first, int firstStatus, Guid second, int secondStatus)
    {
        var context = new XrmFakedContext();

        context.Initialize(new List<Entity> { JobOf(first, firstStatus), JobOf(second, secondStatus) });

        return context;
    }

    private static Entity JobOf(Guid workOrderId, int status) =>
        new(WorkOrder, workOrderId)
        {
            ["dcl_number"] = "WO-20260901-ABCDEF",
            ["dcl_status"] = new OptionSetValue(status)
        };

    private static void Run(XrmFakedContext context, object target, Entity? preImage, string message)
    {
        var pluginContext = context.GetDefaultPluginContext();
        pluginContext.MessageName = message;
        pluginContext.Stage = 20;
        pluginContext.Depth = 1;
        pluginContext.InputParameters = new ParameterCollection { { "Target", target } };

        if (preImage != null)
        {
            pluginContext.PreEntityImages = new EntityImageCollection { { "PreImage", preImage } };
        }

        context.ExecutePluginWith<ClosedWorkOrderLinesPlugin>(pluginContext);
    }
}
