using System;
using System.ServiceModel;
using DynamicsCrmLab.Domain.Common;
using Microsoft.Xrm.Sdk;

namespace DynamicsCrmLab.Plugins.Infrastructure;

/// <summary>
/// Provides the plumbing every plug-in repeats.
/// </summary>
/// <remarks>
/// Pulls the services out of the container, traces the run and turns whatever
/// goes wrong into a message the platform can show. Derived types are left with
/// nothing but their own logic.
/// </remarks>
/// <param name="pluginName">The name written to the trace log.</param>
/// <exception cref="ArgumentNullException">Thrown when the name is missing.</exception>
public abstract class PluginBase(string pluginName) : IPlugin
{
    /// <summary>
    /// Gets the name written to the trace log.
    /// </summary>
    protected string PluginName { get; } =
        pluginName ?? throw new ArgumentNullException(nameof(pluginName));

    /// <summary>
    /// Runs the plug-in.
    /// </summary>
    /// <param name="serviceProvider">The container the platform supplies.</param>
    /// <exception cref="ArgumentNullException">Thrown when the container is missing.</exception>
    /// <exception cref="InvalidPluginExecutionException">
    /// Thrown for anything the user should see, including failures translated
    /// from the platform or from a broken business rule.
    /// </exception>
    public void Execute(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        var execution = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
        var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

        // Calling back as the initiating user keeps the plug-in inside their
        // permissions rather than quietly widening them.
        var service = factory.CreateOrganizationService(execution.UserId);
        var context = new PluginContext(execution, service, tracing);

        tracing.Trace(
            "{0}: start, message={1}, stage={2}, depth={3}",
            PluginName,
            execution.MessageName,
            execution.Stage,
            execution.Depth);

        try
        {
            Execute(context);
        }
        catch (InvalidPluginExecutionException)
        {
            // Already phrased for the user.
            throw;
        }
        catch (DomainException exception)
        {
            tracing.Trace("{0}: rule broken: {1}", PluginName, exception.Message);

            throw new InvalidPluginExecutionException(exception.Message, exception);
        }
        catch (FaultException<OrganizationServiceFault> exception)
        {
            tracing.Trace("{0}: platform fault: {1}", PluginName, exception);

            throw new InvalidPluginExecutionException($"{PluginName} could not complete.", exception);
        }
        finally
        {
            tracing.Trace("{0}: end", PluginName);
        }
    }

    /// <summary>
    /// Carries out the work of the plug-in.
    /// </summary>
    /// <param name="context">What the platform supplied for this run.</param>
    protected abstract void Execute(PluginContext context);
}
