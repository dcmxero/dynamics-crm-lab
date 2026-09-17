using System;
using System.Collections.Generic;
using System.Linq;
using DynamicsCrmLab.Domain.WorkOrders;
using DynamicsCrmLab.Plugins.WorkOrders;
using FakeXrmEasy;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DynamicsCrmLab.Plugins.Tests.WorkOrders;

public sealed class WorkOrderClosedNotificationPluginTests
{
    private const string WorkOrder = "dcl_workorder";
    private const string Status = "dcl_status";
    private const string Number = "dcl_number";

    [Fact]
    public void Execute_RaisesAFollowUpTaskWhenTheJobIsClosed()
    {
        var context = new XrmFakedContext();
        var workOrderId = Guid.NewGuid();

        RunPlugin(
            context,
            TargetWith(workOrderId, WorkOrderStatus.Closed),
            PreImageWith(WorkOrderStatus.InProgress));

        var tasks = context.CreateQuery("task").ToList();

        tasks.Should().ContainSingle();
        tasks[0].GetAttributeValue<string>("subject").Should().Contain("WO-20260901-ABCDEF");
    }

    [Fact]
    public void Execute_RaisesNothingWhenTheStatusDidNotActuallyChange()
    {
        var context = new XrmFakedContext();

        RunPlugin(
            context,
            TargetWith(Guid.NewGuid(), WorkOrderStatus.Closed),
            PreImageWith(WorkOrderStatus.Closed));

        context.CreateQuery("task").Should().BeEmpty();
    }

    [Fact]
    public void Execute_RaisesNothingForOtherStatuses()
    {
        var context = new XrmFakedContext();

        RunPlugin(
            context,
            TargetWith(Guid.NewGuid(), WorkOrderStatus.InProgress),
            PreImageWith(WorkOrderStatus.Assigned));

        context.CreateQuery("task").Should().BeEmpty();
    }

    private static Entity TargetWith(Guid workOrderId, WorkOrderStatus status) =>
        new(WorkOrder, workOrderId) { [Status] = new OptionSetValue((int)status) };

    private static Entity PreImageWith(WorkOrderStatus status) =>
        new(WorkOrder)
        {
            [Status] = new OptionSetValue((int)status),
            [Number] = "WO-20260901-ABCDEF"
        };

    private static void RunPlugin(XrmFakedContext context, Entity target, Entity preImage)
    {
        var pluginContext = context.GetDefaultPluginContext();
        pluginContext.MessageName = "Update";
        pluginContext.Stage = 40;
        pluginContext.InputParameters = new ParameterCollection { { "Target", target } };
        pluginContext.PreEntityImages = new EntityImageCollection { { "PreImage", preImage } };

        context.ExecutePluginWith<WorkOrderClosedNotificationPlugin>(pluginContext);
    }
}
