using System;
using DynamicsCrmLab.Plugins.WorkOrders;
using FakeXrmEasy;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DynamicsCrmLab.Plugins.Tests.WorkOrders;

public sealed class ClosedWorkOrderPluginTests
{
    private const string WorkOrder = "dcl_workorder";
    private const string Status = "dcl_status";
    private const string Resolution = "dcl_resolution";
    private const string Number = "dcl_number";

    private const int InProgress = 3;
    private const int Closed = 4;

    [Fact]
    public void Execute_RefusesTheResolutionBeingEmptiedOnAClosedJob()
    {
        // The stage guard is asked only about the stage, so this update is not a
        // stage change at all and it alone lets the statement to the customer be
        // taken away.
        var change = new Entity(WorkOrder, Guid.NewGuid())
        {
            [Status] = new OptionSetValue(Closed),
            [Resolution] = null
        };

        var emptying = () => Run(change, WasClosed());

        emptying.Should().Throw<InvalidPluginExecutionException>()
            .WithMessage("*is closed*");
    }

    [Fact]
    public void Execute_RefusesAChangeThatDoesNotTouchTheStageAtAll()
    {
        var change = new Entity(WorkOrder, Guid.NewGuid()) { [Resolution] = "Something else entirely." };

        var changing = () => Run(change, WasClosed());

        changing.Should().Throw<InvalidPluginExecutionException>()
            .WithMessage("*is closed*");
    }

    [Fact]
    public void Execute_NamesTheJobItRefused()
    {
        var change = new Entity(WorkOrder, Guid.NewGuid()) { [Resolution] = "x" };

        var changing = () => Run(change, WasClosed());

        changing.Should().Throw<InvalidPluginExecutionException>()
            .WithMessage("*WO-20260901-ABCDEF*");
    }

    [Fact]
    public void Execute_LeavesAJobThatIsStillRunningAlone()
    {
        var change = new Entity(WorkOrder, Guid.NewGuid()) { [Resolution] = "Replaced the seal." };

        var closing = () => Run(change, Was(InProgress));

        closing.Should().NotThrow();
    }

    [Fact]
    public void Execute_SaysNothingWhenTheStageItCameFromIsUnknown()
    {
        var change = new Entity(WorkOrder, Guid.NewGuid()) { [Resolution] = "x" };

        var changing = () => Run(change, preImage: null);

        changing.Should().NotThrow();
    }

    [Fact]
    public void Execute_IgnoresMessagesAboutOtherTables()
    {
        var running = () => Run(new Entity("account", Guid.NewGuid()), WasClosed());

        running.Should().NotThrow();
    }

    private static Entity WasClosed() => Was(Closed);

    private static Entity Was(int status) =>
        new(WorkOrder, Guid.NewGuid())
        {
            [Status] = new OptionSetValue(status),
            [Number] = "WO-20260901-ABCDEF"
        };

    private static void Run(Entity target, Entity? preImage)
    {
        var context = new XrmFakedContext();
        var pluginContext = context.GetDefaultPluginContext();

        pluginContext.MessageName = "Update";
        pluginContext.Stage = 20;
        pluginContext.Depth = 1;
        pluginContext.InputParameters = new ParameterCollection { { "Target", target } };

        if (preImage != null)
        {
            pluginContext.PreEntityImages = new EntityImageCollection { { "PreImage", preImage } };
        }

        context.ExecutePluginWith<ClosedWorkOrderPlugin>(pluginContext);
    }
}
