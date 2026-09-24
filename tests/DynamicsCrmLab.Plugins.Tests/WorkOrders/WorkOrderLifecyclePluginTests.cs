using System;
using System.Collections.Generic;
using DynamicsCrmLab.Plugins.WorkOrders;
using FakeXrmEasy;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DynamicsCrmLab.Plugins.Tests.WorkOrders;

public sealed class WorkOrderLifecyclePluginTests
{
    private const string WorkOrder = "dcl_workorder";
    private const string Status = "dcl_status";
    private const string Technician = "dcl_technicianid";
    private const string Resolution = "dcl_resolution";

    private const int New = 1;
    private const int Assigned = 2;
    private const int InProgress = 3;
    private const int Closed = 4;

    [Fact]
    public void Execute_RefusesClosingAJobNobodyStarted()
    {
        var refusing = () => RunPlugin(
            from: New,
            to: Closed,
            image: image => image[Resolution] = "Replaced the filter.");

        refusing.Should().Throw<InvalidPluginExecutionException>()
            .WithMessage("*in-progress*");
    }

    [Fact]
    public void Execute_RefusesReopeningAClosedJob()
    {
        var refusing = () => RunPlugin(from: Closed, to: InProgress);

        refusing.Should().Throw<InvalidPluginExecutionException>();
    }

    [Fact]
    public void Execute_RefusesPuttingAJobBackToTheStartingStage()
    {
        var refusing = () => RunPlugin(from: Assigned, to: New);

        refusing.Should().Throw<InvalidPluginExecutionException>()
            .WithMessage("*cannot be put back*");
    }

    [Fact]
    public void Execute_RefusesAssigningNobody()
    {
        var refusing = () => RunPlugin(from: New, to: Assigned);

        refusing.Should().Throw<InvalidPluginExecutionException>()
            .WithMessage("*technician is required*");
    }

    [Fact]
    public void Execute_RefusesClosingWithNoAccountOfTheWork()
    {
        var refusing = () => RunPlugin(from: InProgress, to: Closed);

        refusing.Should().Throw<InvalidPluginExecutionException>()
            .WithMessage("*resolution*");
    }

    [Fact]
    public void Execute_AllowsTheMovesTheJobDoesAllow()
    {
        var starting = () => RunPlugin(
            from: Assigned,
            to: InProgress,
            image: image => image[Technician] = new EntityReference("dcl_technician", Guid.NewGuid()));

        var closing = () => RunPlugin(
            from: InProgress,
            to: Closed,
            image: image => image[Resolution] = "Replaced the filter.");

        starting.Should().NotThrow();
        closing.Should().NotThrow();
    }

    [Fact]
    public void Execute_LeavesAChangeThatIsNotAboutTheStageAlone()
    {
        var context = new XrmFakedContext();
        var workOrderId = Guid.NewGuid();

        var target = new Entity(WorkOrder, workOrderId) { [Resolution] = "A note." };
        var image = new Entity(WorkOrder, workOrderId) { [Status] = new OptionSetValue(Closed) };

        var running = () => Run(context, target, image);

        running.Should().NotThrow();
    }

    [Fact]
    public void Execute_RefusesTheStageBeingEmptied()
    {
        // An absent stage means the change is about something else. A stage that
        // is present and empty is a statement no job can honour, and leaving it
        // written would strand the record: every later stage change reads the
        // stage it came from and would find nothing there.
        var target = new Entity(WorkOrder, Guid.NewGuid()) { [Status] = null };

        var emptying = () => Run(new XrmFakedContext(), target, ImageAt(target.Id, InProgress));

        emptying.Should().Throw<InvalidPluginExecutionException>()
            .WithMessage("*cannot be emptied*");
    }

    [Fact]
    public void Execute_RefusesAJobCreatedAlreadyFinished()
    {
        // Nothing transitions here, so the stage guard on update never sees it:
        // the record simply begins closed, with nobody on it and nothing said
        // about what was done.
        var creating = () => Create(Closed);

        creating.Should().Throw<InvalidPluginExecutionException>()
            .WithMessage("*begins as New*");
    }

    [Fact]
    public void Execute_RefusesAJobCreatedAlreadyAssigned()
    {
        var creating = () => Create(Assigned);

        creating.Should().Throw<InvalidPluginExecutionException>();
    }

    [Fact]
    public void Execute_AllowsAJobCreatedAtTheStartingStage()
    {
        var creating = () => Create(New);

        creating.Should().NotThrow();
    }

    [Fact]
    public void Execute_AllowsAJobCreatedWithNoStageAtAll()
    {
        // The column carries a default, so saying nothing is saying New.
        var creating = () => Create(status: null);

        creating.Should().NotThrow();
    }

    private static void Create(int? status)
    {
        var target = new Entity(WorkOrder, Guid.NewGuid());

        if (status is { } value)
        {
            target[Status] = new OptionSetValue(value);
        }

        var context = new XrmFakedContext();
        var pluginContext = context.GetDefaultPluginContext();

        pluginContext.MessageName = "Create";
        pluginContext.Stage = 20;
        pluginContext.Depth = 1;
        pluginContext.InputParameters = new ParameterCollection { { "Target", target } };

        context.ExecutePluginWith<WorkOrderLifecyclePlugin>(pluginContext);
    }

    private static Entity ImageAt(Guid workOrderId, int status) =>
        new(WorkOrder, workOrderId)
        {
            [Status] = new OptionSetValue(status),
            ["dcl_number"] = "WO-20260901-ABCDEF"
        };

    private static void RunPlugin(int from, int to, Action<Entity>? image = null)
    {
        var context = new XrmFakedContext();
        var workOrderId = Guid.NewGuid();

        var target = new Entity(WorkOrder, workOrderId) { [Status] = new OptionSetValue(to) };

        var preImage = new Entity(WorkOrder, workOrderId)
        {
            [Status] = new OptionSetValue(from),
            ["dcl_number"] = "WO-20260901-ABCDEF"
        };

        image?.Invoke(preImage);

        Run(context, target, preImage);
    }

    private static void Run(XrmFakedContext context, Entity target, Entity preImage)
    {
        var pluginContext = context.GetDefaultPluginContext();
        pluginContext.MessageName = "Update";
        pluginContext.Stage = 20;
        pluginContext.Depth = 1;
        pluginContext.InputParameters = new ParameterCollection { { "Target", target } };
        pluginContext.PreEntityImages = new EntityImageCollection { { "PreImage", preImage } };

        context.Initialize(new List<Entity> { preImage });

        context.ExecutePluginWith<WorkOrderLifecyclePlugin>(pluginContext);
    }
}
