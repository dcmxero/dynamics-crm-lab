using System;
using Microsoft.Xrm.Sdk;

namespace DynamicsCrmLab.Plugins.Infrastructure;

/// <summary>
/// Represents what a plug-in is given when the platform invokes it.
/// </summary>
/// <remarks>
/// Wrapping the service provider means the logic can be written against a few
/// named members instead of untyped lookups, and can be tested without one.
/// </remarks>
/// <param name="execution">The execution context supplied by the platform.</param>
/// <param name="service">The organization service to call back with.</param>
/// <param name="tracing">The trace writer whose output lands in the plug-in trace log.</param>
/// <exception cref="ArgumentNullException">Thrown when any argument is missing.</exception>
public sealed class PluginContext(
    IPluginExecutionContext execution,
    IOrganizationService service,
    ITracingService tracing)
{
    /// <summary>
    /// Gets the execution context supplied by the platform.
    /// </summary>
    public IPluginExecutionContext Execution { get; } =
        execution ?? throw new ArgumentNullException(nameof(execution));

    /// <summary>
    /// Gets the organization service to call back with.
    /// </summary>
    public IOrganizationService Service { get; } =
        service ?? throw new ArgumentNullException(nameof(service));

    /// <summary>
    /// Gets the trace writer whose output lands in the plug-in trace log.
    /// </summary>
    public ITracingService Tracing { get; } =
        tracing ?? throw new ArgumentNullException(nameof(tracing));

    /// <summary>
    /// Gets the record the operation concerns, or <see langword="null"/> when
    /// the message carries none.
    /// </summary>
    public Entity? Target =>
        Execution.InputParameters.TryGetValue("Target", out var target) ? target as Entity : null;

    /// <summary>
    /// Gets a reference to the record the operation concerns, however the
    /// message carries it.
    /// </summary>
    /// <remarks>
    /// Create and Update put the record itself in Target; Delete puts only a
    /// reference to it. A step registered on all three has to read both.
    /// </remarks>
    public EntityReference? TargetReference =>
        Execution.InputParameters.TryGetValue("Target", out var target)
            ? target switch
            {
                Entity entity => entity.ToEntityReference(),
                EntityReference reference => reference,
                _ => null
            }
            : null;

    /// <summary>
    /// Gets a value indicating whether this run was triggered by another plug-in.
    /// </summary>
    /// <remarks>
    /// Recursion is prevented first by registration - filtering attributes, the
    /// right stage, writing to the target instead of issuing another update.
    /// This check is the last line of defence, not the design.
    /// </remarks>
    public bool IsNested => Execution.Depth > 1;

    /// <summary>
    /// Returns a registered image of the record as it was before the operation.
    /// </summary>
    /// <param name="name">The name the image was registered under.</param>
    /// <returns>The image, or <see langword="null"/> when none was registered.</returns>
    public Entity? PreImage(string name = "PreImage") =>
        Execution.PreEntityImages.TryGetValue(name, out var image) ? image : null;
}
