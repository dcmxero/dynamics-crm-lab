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
}
