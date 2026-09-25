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
            return Result.NotFound<RaiseWorkOrderResult>($"Customer {command.CustomerId} does not exist.");
        }

        var unit = await equipment.GetByIdAsync(command.EquipmentId, cancellationToken).ConfigureAwait(false);
        if (unit is null)
        {
            return Result.NotFound<RaiseWorkOrderResult>($"Equipment {command.EquipmentId} does not exist.");
        }

        // A job is billed to a customer and carried out on their equipment. A unit
        // belonging to somebody else would invoice one customer for work on
        // another customer's property, which no later step would catch.
        if (unit.CustomerId != customer.Id)
        {
            var mismatch = $"Equipment {unit.SerialNumber} does not belong to {customer.Name}.";

            ApplicationLog.WorkOrderRejected(logger, command.CustomerId, mismatch);

            return Result.RuleBroken<RaiseWorkOrderResult>(mismatch);
        }

        WorkOrder workOrder;
        StoredWorkOrder stored;

        // Without a key from the caller, this request is unlike any other, and
        // saying so with a fresh one keeps the store's rule the same for
        // everybody rather than something only some requests obey.
        var requestKey = string.IsNullOrWhiteSpace(command.RequestKey)
            ? Guid.NewGuid().ToString()
            : command.RequestKey;

        try
        {
            workOrder = WorkOrder.Create(customer.Id, unit.Id);

            foreach (var line in command.Lines)
            {
                workOrder.AddLine(line.Description, line.Quantity, Money.Of(line.UnitPrice, line.Currency));
            }

            // The store enforces rules of its own, and a rule it refuses is the
            // same kind of answer as one the aggregate refuses.
            stored = await workOrders.AddAsync(workOrder, requestKey, cancellationToken).ConfigureAwait(false);
        }
        // A value the domain refuses to build from - a negative price - arrives as
        // an argument exception rather than a broken rule, but to the caller it
        // is the same kind of answer: the request was understood and refused.
        catch (Exception exception) when (exception is DomainException or ArgumentException)
        {
            // A broken rule is an ordinary answer to the request, not a failure
            // of the program, so the caller gets it as a result.
            ApplicationLog.WorkOrderRejected(logger, command.CustomerId, exception.Message);

            return Result.RuleBroken<RaiseWorkOrderResult>(exception.Message);
        }

        if (!stored.WasRaisedNow)
        {
            // The job the earlier request raised is the answer to this one. Its
            // stage and its charges may have moved on since, so nothing about
            // the aggregate built here is reported back as though it were fresh.
            ApplicationLog.WorkOrderAlreadyRaised(logger, requestKey, stored.Number);

            return Result.Success(new RaiseWorkOrderResult(
                stored.WorkOrderId,
                stored.Number,
                workOrder.Status,
                workOrder.TotalPrice,
                WasRaisedNow: false));
        }

        ApplicationLog.WorkOrderRaised(logger, workOrder.Number, customer.Id);

        return Result.Success(new RaiseWorkOrderResult(
            stored.WorkOrderId,
            workOrder.Number,
            workOrder.Status,
            workOrder.TotalPrice));
    }
}
