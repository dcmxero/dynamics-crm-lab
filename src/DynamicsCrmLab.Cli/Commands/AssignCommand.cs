using DynamicsCrmLab.Application.UseCases.AssignWorkOrder;

namespace DynamicsCrmLab.Cli.Commands;

/// <summary>
/// Puts a technician on a work order from the command line.
/// </summary>
/// <param name="handler">The use case that assigns the job.</param>
internal sealed class AssignCommand(AssignWorkOrderHandler handler) : ICliCommand
{
    /// <inheritdoc/>
    public string Name => "assign";

    /// <inheritdoc/>
    public string Usage => "assign <workOrderId> [technicianId] - omit the technician to take whoever is free";

    /// <inheritdoc/>
    public async Task<int> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Count is 0 or > 2 || !Guid.TryParse(arguments[0], out var workOrderId))
        {
            await Console.Out.WriteLineAsync($"Usage: {Usage}").ConfigureAwait(false);

            return 1;
        }

        Guid? technicianId = arguments.Count is 2 && Guid.TryParse(arguments[1], out var named) ? named : null;

        var result = await handler
            .HandleAsync(new AssignWorkOrderCommand(workOrderId, technicianId), cancellationToken)
            .ConfigureAwait(false);

        await Console.Out.WriteLineAsync(result.IsSuccess
            ? $"  assigned to {result.Value!.TechnicianName}"
            : $"Not assigned: {result.Error}").ConfigureAwait(false);

        return result.IsSuccess ? 0 : 1;
    }
}
