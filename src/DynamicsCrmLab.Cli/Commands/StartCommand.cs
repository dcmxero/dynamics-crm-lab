using DynamicsCrmLab.Application.UseCases.StartWorkOrder;

namespace DynamicsCrmLab.Cli.Commands;

/// <summary>
/// Records that the technician has started on a work order.
/// </summary>
/// <remarks>
/// Without this the console application can raise a job and put somebody on it
/// and then go no further, because a job cannot be closed straight from
/// assigned. It would demonstrate a lifecycle it cannot finish.
/// </remarks>
/// <param name="handler">The use case that starts the work.</param>
internal sealed class StartCommand(StartWorkOrderHandler handler) : ICliCommand
{
    /// <inheritdoc/>
    public string Name => "start";

    /// <inheritdoc/>
    public string Usage => "start <workOrderId>";

    /// <inheritdoc/>
    public async Task<int> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Count is not 1 || !Guid.TryParse(arguments[0], out var workOrderId))
        {
            await Console.Out.WriteLineAsync($"Usage: {Usage}").ConfigureAwait(false);

            return 1;
        }

        var result = await handler
            .HandleAsync(new StartWorkOrderCommand(workOrderId), cancellationToken)
            .ConfigureAwait(false);

        await Console.Out.WriteLineAsync(result.IsSuccess
            ? $"  {result.Value!.Number} is now {result.Value.Status}"
            : $"Not started: {result.Error}").ConfigureAwait(false);

        return result.IsSuccess ? 0 : 1;
    }
}
