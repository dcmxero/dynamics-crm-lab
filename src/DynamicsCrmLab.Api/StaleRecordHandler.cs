using DynamicsCrmLab.Application.Abstractions;
using Microsoft.AspNetCore.Diagnostics;

namespace DynamicsCrmLab.Api;

/// <summary>
/// Answers a request that lost a race with 409 rather than 500.
/// </summary>
/// <remarks>
/// Nothing was wrong with the request. It arrived after somebody else changed
/// the same job, and the caller can settle it by reading the job again and
/// deciding what they still want. That is a conflict, not a fault, and telling
/// the two apart is the difference between a client that retries sensibly and
/// one that shows an error page.
/// </remarks>
internal sealed class StaleRecordHandler : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is not ConcurrencyException)
        {
            return false;
        }

        await Results
            .Problem(
                title: "The work order moved on",
                detail: exception.Message,
                statusCode: StatusCodes.Status409Conflict)
            .ExecuteAsync(httpContext)
            .ConfigureAwait(false);

        return true;
    }
}
