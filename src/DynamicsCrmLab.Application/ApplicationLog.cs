using DynamicsCrmLab.Domain.Common;
using Microsoft.Extensions.Logging;

namespace DynamicsCrmLab.Application;

/// <summary>
/// Provides the log messages emitted by the application layer.
/// </summary>
/// <remarks>
/// Written as source-generated <see cref="LoggerMessageAttribute"/> methods so
/// that formatting is skipped entirely when the level is disabled.
/// </remarks>
internal static partial class ApplicationLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Work order {Number} raised for customer {CustomerId}.")]
    public static partial void WorkOrderRaised(ILogger logger, string number, Guid customerId);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Work order for customer {CustomerId} was rejected: {Reason}")]
    public static partial void WorkOrderRejected(ILogger logger, Guid customerId, string reason);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Work order {Number} assigned to {TechnicianName}.")]
    public static partial void WorkOrderAssigned(ILogger logger, string number, string technicianName);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Work started on work order {Number}.")]
    public static partial void WorkOrderStarted(ILogger logger, string number);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "Work order {Number} closed, {TotalPrice} to invoice.")]
    public static partial void WorkOrderClosed(ILogger logger, string number, Money totalPrice);
}
