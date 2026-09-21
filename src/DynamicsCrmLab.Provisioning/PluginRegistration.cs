namespace DynamicsCrmLab.Provisioning;

/// <summary>
/// Represents when the platform runs a step, in the platform's own numbering.
/// </summary>
internal enum PipelineStage
{
    /// <summary>Before the platform has validated anything, outside the transaction.</summary>
    PreValidation = 10,

    /// <summary>Inside the transaction, before the record is written.</summary>
    PreOperation = 20,

    /// <summary>Inside the transaction, after the record is written.</summary>
    PostOperation = 40
}

/// <summary>
/// Represents whether the caller waits for the step.
/// </summary>
internal enum ExecutionMode
{
    /// <summary>The step runs in the caller's transaction and the caller waits.</summary>
    Synchronous = 0,

    /// <summary>The step is queued and the caller carries on.</summary>
    Asynchronous = 1
}

/// <summary>
/// Describes one plug-in step as it is to be registered.
/// </summary>
/// <remarks>
/// The registration is the other half of a plug-in: the same class registered
/// on a different message or stage is a different piece of behaviour. Keeping
/// the description here means the two halves are reviewed together and an
/// environment can be rebuilt without anyone remembering what was clicked.
/// </remarks>
/// <param name="TypeName">The full name of the plug-in class.</param>
/// <param name="Message">The message the step runs on, such as Create or Update.</param>
/// <param name="EntityName">The logical name of the table the step runs against.</param>
/// <param name="Stage">When in the pipeline the step runs.</param>
/// <param name="Mode">Whether the caller waits for the step.</param>
/// <param name="FilteringAttributes">
/// The columns whose change triggers the step, or <see langword="null"/> for every column.
/// </param>
/// <param name="PreImageAttributes">
/// The columns to capture before the write, or <see langword="null"/> for no image.
/// </param>
internal sealed record PluginStep(
    string TypeName,
    string Message,
    string EntityName,
    PipelineStage Stage,
    ExecutionMode Mode,
    string[]? FilteringAttributes = null,
    string[]? PreImageAttributes = null)
{
    /// <summary>
    /// Gets the name the step is stored and shown under.
    /// </summary>
    public string Name => $"{TypeName}: {Message} of {EntityName}";
}
