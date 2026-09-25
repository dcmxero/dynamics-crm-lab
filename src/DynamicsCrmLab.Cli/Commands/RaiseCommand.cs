using System.Globalization;
using DynamicsCrmLab.Application.UseCases.RaiseWorkOrder;

namespace DynamicsCrmLab.Cli.Commands;

/// <summary>
/// Raises a work order from the command line.
/// </summary>
/// <param name="handler">The use case that raises the job.</param>
internal sealed class RaiseCommand(RaiseWorkOrderHandler handler) : ICliCommand
{
    private const int WithoutKey = 5;
    private const int WithKey = 6;

    /// <inheritdoc/>
    public string Name => "raise";

    /// <inheritdoc/>
    public string Usage =>
        "raise <customerId> <equipmentId> <description> <quantity> <unitPrice> [requestKey]";

    /// <inheritdoc/>
    public async Task<int> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        // A script that reruns after a failure is the same caller asking again,
        // and naming the request is how it says so.
        if (arguments.Count is not (WithoutKey or WithKey)
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
                    [new WorkOrderLineInput(arguments[2], quantity, unitPrice)],
                    arguments.Count is WithKey ? arguments[5] : null),
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

        if (!raised.WasRaisedNow)
        {
            await Console.Out
                .WriteLineAsync("  this request had already raised it, so nothing was raised now")
                .ConfigureAwait(false);
        }

        return 0;
    }
}
