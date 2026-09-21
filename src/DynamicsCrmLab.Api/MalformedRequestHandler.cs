using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DynamicsCrmLab.Api;

/// <summary>
/// Answers a request the server could not read with the status that says so.
/// </summary>
/// <remarks>
/// A body that cannot be deserialized surfaces as a <see cref="BadHttpRequestException"/>
/// carrying the status it deserves. Left to the fallback handler it becomes a
/// 500, which tells the caller the server is broken when in fact the request is.
/// </remarks>
internal sealed class MalformedRequestHandler : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is not BadHttpRequestException malformed)
        {
            return false;
        }

        var problem = new ProblemDetails
        {
            Title = "The request could not be read",
            // The inner exception carries which field and position failed, which
            // is what the caller needs to correct it.
            Detail = exception.InnerException?.Message ?? malformed.Message,
            Status = malformed.StatusCode
        };

        httpContext.Response.StatusCode = malformed.StatusCode;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);

        return true;
    }
}
