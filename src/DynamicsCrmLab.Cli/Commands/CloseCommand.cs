using DynamicsCrmLab.Application.UseCases.CloseWorkOrder;

namespace DynamicsCrmLab.Cli.Commands;

/// <summary>
/// Finishes a work order from the command line.
/// </summary>
/// <param name="handler">The use case that closes the job.</param>
internal sealed class CloseCommand(CloseWorkOrderHandler handler) : ICliCommand
{
    /// <inheritdoc/>
    public string Name => "close";

    /// <inheritdoc/>
    public string Usage => "close <workOrderId> <resolution>";

    /// <inheritdoc/>
    public async Task<int> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Count < 2 || !Guid.TryParse(arguments[0], out var workOrderId))
        {
            await Console.Out.WriteLineAsync($"Usage: {Usage}").ConfigureAwait(false);

            return 1;
        }

        var resolution = string.Join(' ', arguments.Skip(1));

        var result = await handler
            .HandleAsync(new CloseWorkOrderCommand(workOrderId, resolution), cancellationToken)
            .ConfigureAwait(false);

        await Console.Out.WriteLineAsync(result.IsSuccess
            ? $"  {result.Value!.Number} closed, {result.Value.TotalPrice} to invoice"
            : $"Not closed: {result.Error}").ConfigureAwait(false);

        return result.IsSuccess ? 0 : 1;
    }
}
