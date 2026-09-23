using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Application.UseCases.AssignWorkOrder;
using DynamicsCrmLab.Application.UseCases.CloseWorkOrder;
using DynamicsCrmLab.Application.UseCases.RaiseWorkOrder;
using DynamicsCrmLab.Application.UseCases.StartWorkOrder;
using DynamicsCrmLab.Application.UseCases.ViewWorkOrders;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Identity.Web;

namespace DynamicsCrmLab.Api.WorkOrders;

/// <summary>
/// Exposes the work order use cases over HTTP.
/// </summary>
/// <remarks>
/// The endpoints hold no logic of their own. They read the request, call a use
/// case and translate the result, so the rules stay in one place and the same
/// behaviour backs the console application.
/// </remarks>
internal static class WorkOrderEndpoints
{
    /// <summary>
    /// Registers the work order routes.
    /// </summary>
    /// <param name="app">The route builder to add to.</param>
    /// <returns>The group the routes were added to.</returns>
    public static RouteGroupBuilder MapWorkOrders(this IEndpointRouteBuilder app)
    {
        // Every route below speaks for the caller, so none of them answer an
        // anonymous request. The scope is checked as well as the token: a token
        // issued for some other application says nothing about this one.
        var group = app.MapGroup("/api/work-orders")
            .WithTags("Work orders")
            .RequireAuthorization()
            .RequireScope(CallerAuthentication.Scope);

        group.MapGet("/", ListAsync)
            .WithName("ListWorkOrders")
            .WithSummary("Lists one page of work orders that have reached a given stage.");

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetWorkOrder")
            .WithSummary("Reads one work order in full.");

        group.MapPost("/", RaiseAsync)
            .WithName("RaiseWorkOrder")
            .WithSummary("Raises a work order against a customer and their equipment.");

        group.MapPost("/{id:guid}/assignment", AssignAsync)
            .WithName("AssignWorkOrder")
            .WithSummary("Puts a technician on a work order.");

        group.MapPost("/{id:guid}/start", StartAsync)
            .WithName("StartWorkOrder")
            .WithSummary("Records that the technician has started on a work order.");

        group.MapPost("/{id:guid}/closure", CloseAsync)
            .WithName("CloseWorkOrder")
            .WithSummary("Finishes a work order and records what was done.");

        return group;
    }

    private static async Task<IResult> ListAsync(
        ListWorkOrdersHandler handler,
        string? status,
        int? take,
        string? cursor,
        CancellationToken cancellationToken)
    {
        if (!WorkOrderMapping.TryParseStatus(status ?? "New", out var parsed))
        {
            return TypedResults.Problem(
                title: "Unknown status",
                detail: $"'{status}' is not a work order status.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var found = await handler
            .HandleAsync(parsed, take ?? 50, cursor, cancellationToken)
            .ConfigureAwait(false);

        return TypedResults.Ok(new WorkOrderPageResponse(
            [.. found.Items.Select(summary => summary.ToResponse())],
            found.NextCursor));
    }

    private static async Task<IResult> GetAsync(
        GetWorkOrderHandler handler,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken).ConfigureAwait(false);

        return Translate(result, view => view.ToResponse());
    }

    private static async Task<IResult> RaiseAsync(
        RaiseWorkOrderHandler handler,
        RaiseWorkOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (Missing(request) is { } missing)
        {
            return TypedResults.ValidationProblem(missing);
        }

        var command = new RaiseWorkOrderCommand(
            request.CustomerId,
            request.EquipmentId,
            [.. request.Lines.Select(line => new WorkOrderLineInput(line.Description, line.Quantity, line.UnitPrice))]);

        var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Failed(result.Failure, result.Error);
        }

        var raised = result.Value!;

        return TypedResults.Created($"/api/work-orders/{raised.WorkOrderId}", raised.ToResponse());
    }

    private static async Task<IResult> AssignAsync(
        AssignWorkOrderHandler handler,
        Guid id,
        AssignWorkOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler
            .HandleAsync(new AssignWorkOrderCommand(id, request.TechnicianId), cancellationToken)
            .ConfigureAwait(false);

        return Translate(result, assigned => assigned.ToResponse());
    }

    private static async Task<IResult> StartAsync(
        StartWorkOrderHandler handler,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await handler
            .HandleAsync(new StartWorkOrderCommand(id), cancellationToken)
            .ConfigureAwait(false);

        return Translate(result, started => started.ToResponse());
    }

    private static async Task<IResult> CloseAsync(
        CloseWorkOrderHandler handler,
        Guid id,
        CloseWorkOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler
            .HandleAsync(new CloseWorkOrderCommand(id, request.Resolution), cancellationToken)
            .ConfigureAwait(false);

        return Translate(result, closed => closed.ToResponse());
    }

    /// <summary>
    /// Turns a use case result into a response.
    /// </summary>
    /// <remarks>
    /// A missing record is 404. A broken business rule is 422: the request was
    /// understood and well formed, the state of the job simply does not allow it,
    /// which is a different thing from a malformed request.
    /// </remarks>
    private static IResult Translate<TValue, TResponse>(
        Result<TValue> result,
        Func<TValue, TResponse> toResponse) =>
        result.IsSuccess
            ? TypedResults.Ok(toResponse(result.Value!))
            : Failed(result.Failure, result.Error);

    private static ProblemHttpResult Failed(ResultFailure? failure, string? detail) =>
        failure is ResultFailure.NotFound ? NotFound(detail) : UnprocessableEntity(detail);

    /// <summary>
    /// Reports what the request left out.
    /// </summary>
    /// <remarks>
    /// A body that parses but carries nothing where charges belong is a request
    /// the caller can correct, so it is answered as one rather than allowed to
    /// fail somewhere further in as a fault.
    /// </remarks>
    /// <param name="request">The request as it was read.</param>
    /// <returns>The errors to report, or <see langword="null"/> when there are none.</returns>
    private static Dictionary<string, string[]>? Missing(RaiseWorkOrderRequest request)
    {
        if (request.Lines is null)
        {
            return new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["lines"] = ["A work order needs the charges to record against it."]
            };
        }

        return request.Lines.Any(line => line is null)
            ? new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["lines"] = ["Every charge needs a description, a quantity and a unit price."]
            }
            : null;
    }

    private static ProblemHttpResult NotFound(string? detail) =>
        TypedResults.Problem(
            title: "Not found",
            detail: detail,
            statusCode: StatusCodes.Status404NotFound);

    private static ProblemHttpResult UnprocessableEntity(string? detail) =>
        TypedResults.Problem(
            title: "The job does not allow this",
            detail: detail,
            statusCode: StatusCodes.Status422UnprocessableEntity);
}
