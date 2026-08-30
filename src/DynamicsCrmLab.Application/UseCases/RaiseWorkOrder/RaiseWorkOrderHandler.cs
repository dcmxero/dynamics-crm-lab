using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.Common;
using DynamicsCrmLab.Domain.WorkOrders;
using Microsoft.Extensions.Logging;

namespace DynamicsCrmLab.Application.UseCases.RaiseWorkOrder;

/// <summary>
/// Raises a work order against a customer and a piece of their equipment.
/// </summary>
/// <remarks>
/// The handler orchestrates: it loads what the job refers to, lets the domain
/// build the aggregate and hands the result to the store. The rules themselves
/// stay in <see cref="WorkOrder"/>.
/// </remarks>
/// <param name="workOrders">The store work orders are written to.</param>
/// <param name="customers">The store customers are read from.</param>
/// <param name="equipment">The store equipment is read from.</param>
/// <param name="logger">Records the outcome of the use case.</param>
public sealed class RaiseWorkOrderHandler(
    IWorkOrderRepository workOrders,
    ICustomerRepository customers,
    IEquipmentRepository equipment,
    ILogger<RaiseWorkOrderHandler> logger)
{
    /// <summary>
    /// Raises the work order described by the command.
    /// </summary>
    /// <param name="command">The job to raise.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>
    /// The raised work order, or a failed result when the customer or the
    /// equipment cannot be found, or when the job breaks a business rule.
    /// </returns>
    public async Task<Result<RaiseWorkOrderResult>> HandleAsync(
        RaiseWorkOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var customer = await customers.GetByIdAsync(command.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return Result.Failure<RaiseWorkOrderResult>($"Customer {command.CustomerId} does not exist.");
        }

        var unit = await equipment.GetByIdAsync(command.EquipmentId, cancellationToken).ConfigureAwait(false);
        if (unit is null)
        {
            return Result.Failure<RaiseWorkOrderResult>($"Equipment {command.EquipmentId} does not exist.");
        }

        WorkOrder workOrder;
        try
        {
            workOrder = WorkOrder.Create(customer.Id, unit.Id);

            foreach (var line in command.Lines)
            {
                workOrder.AddLine(line.Description, line.Quantity, Money.Of(line.UnitPrice, line.Currency));
            }
        }
        catch (DomainException exception)
        {
            // A broken rule is an ordinary answer to the request, not a failure
            // of the program, so the caller gets it as a result.
            ApplicationLog.WorkOrderRejected(logger, command.CustomerId, exception.Message);

            return Result.Failure<RaiseWorkOrderResult>(exception.Message);
        }

        var id = await workOrders.AddAsync(workOrder, cancellationToken).ConfigureAwait(false);

        ApplicationLog.WorkOrderRaised(logger, workOrder.Number, customer.Id);

        return Result.Success(
            new RaiseWorkOrderResult(id, workOrder.Number, workOrder.Status, workOrder.TotalPrice));
    }
}
