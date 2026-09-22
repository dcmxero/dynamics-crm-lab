using System.Globalization;
using DynamicsCrmLab.Application.UseCases.RaiseWorkOrder;

namespace DynamicsCrmLab.Cli.Commands;

/// <summary>
/// Raises a work order from the command line.
/// </summary>
/// <param name="handler">The use case that raises the job.</param>
internal sealed class RaiseCommand(RaiseWorkOrderHandler handler) : ICliCommand
{
    private const int ArgumentCount = 5;

    /// <inheritdoc/>
    public string Name => "raise";

    /// <inheritdoc/>
    public string Usage => "raise <customerId> <equipmentId> <description> <quantity> <unitPrice>";

    /// <inheritdoc/>
    public async Task<int> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Count != ArgumentCount
            || !Guid.TryParse(arguments[0], out var customerId)
            || !Guid.TryParse(arguments[1], out var equipmentId)
            || !int.TryParse(arguments[3], CultureInfo.InvariantCulture, out var quantity)
            || !decimal.TryParse(arguments[4], NumberStyles.Number, CultureInfo.InvariantCulture, out var unitPrice))
        {
            await Console.Out.WriteLineAsync($"Usage: {Usage}").ConfigureAwait(false);

            return 1;
        }

        var result = await handler
            .HandleAsync(
                new RaiseWorkOrderCommand(
                    customerId,
                    equipmentId,
                    [new WorkOrderLineInput(arguments[2], quantity, unitPrice)]),
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await Console.Out.WriteLineAsync($"Not raised: {result.Error}").ConfigureAwait(false);

            return 1;
        }

        var raised = result.Value!;

        await Console.Out.WriteLineAsync($"  number  {raised.Number}").ConfigureAwait(false);
        await Console.Out.WriteLineAsync($"  id      {raised.WorkOrderId}").ConfigureAwait(false);
        await Console.Out.WriteLineAsync($"  status  {raised.Status}").ConfigureAwait(false);
        await Console.Out.WriteLineAsync($"  total   {raised.TotalPrice}").ConfigureAwait(false);

        return 0;
    }
}
