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

        context.Initialize(new List<Entity>
        {
            new(WorkOrder, workOrderId)
            {
                ["dcl_number"] = "WO-20260901-ABCDEF",
                ["dcl_status"] = new OptionSetValue(status)
            }
        });

        return context;
    }

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
