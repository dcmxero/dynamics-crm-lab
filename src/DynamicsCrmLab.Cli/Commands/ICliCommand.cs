namespace DynamicsCrmLab.Cli.Commands;

/// <summary>
/// Defines one command the console application can run.
/// </summary>
/// <remarks>
/// Adding a command means adding a class and registering it; nothing that
/// already exists has to change.
/// </remarks>
internal interface ICliCommand
{
    /// <summary>
    /// Gets the word that selects this command on the command line.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the usage line shown when no command matches.
    /// </summary>
    string Usage { get; }

    /// <summary>
    /// Runs the command.
    /// </summary>
    /// <param name="arguments">The arguments that followed the command name.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes when the command has finished.</returns>
    Task ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken);
}
