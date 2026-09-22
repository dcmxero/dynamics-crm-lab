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
    /// <remarks>
    /// The exit code is the answer anything scripting this reads. A command
    /// that reports a refusal on the console and exits successfully tells a
    /// pipeline the opposite of what it told the person watching.
    /// </remarks>
    /// <param name="arguments">The arguments that followed the command name.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>Zero when the command did what was asked; otherwise non-zero.</returns>
    Task<int> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken);
}
