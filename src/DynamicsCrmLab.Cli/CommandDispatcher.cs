using DynamicsCrmLab.Cli.Commands;

namespace DynamicsCrmLab.Cli;

/// <summary>
/// Picks the command named on the command line and runs it.
/// </summary>
/// <remarks>
/// Knows no command by name: it receives whatever was registered.
/// </remarks>
/// <param name="commands">The commands available to the application.</param>
internal sealed class CommandDispatcher(IEnumerable<ICliCommand> commands)
{
    private readonly IReadOnlyDictionary<string, ICliCommand> _commands =
        commands.ToDictionary(command => command.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Runs the command named by the first argument.
    /// </summary>
    /// <param name="args">The raw command line arguments.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A process exit code.</returns>
    public async Task<int> DispatchAsync(string[] args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length is 0 || !_commands.TryGetValue(args[0], out var command))
        {
            await PrintUsageAsync(_commands.Values).ConfigureAwait(false);

            return args.Length is 0 ? 0 : 1;
        }

        return await command.ExecuteAsync([.. args.Skip(1)], cancellationToken).ConfigureAwait(false);
    }

    private static Task PrintUsageAsync(IEnumerable<ICliCommand> available)
    {
        var lines = available
            .OrderBy(command => command.Name, StringComparer.Ordinal)
            .Select(command => $"  {command.Usage}");

        return Console.Out.WriteLineAsync("Commands:" + Environment.NewLine + string.Join(Environment.NewLine, lines));
    }
}
