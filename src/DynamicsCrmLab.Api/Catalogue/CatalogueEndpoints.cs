using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Application.UseCases.ViewCatalogue;
using Microsoft.Identity.Web;

namespace DynamicsCrmLab.Api.Catalogue;

/// <summary>
/// Represents a customer a job can be raised for.
/// </summary>
/// <param name="Id">The identifier to quote when raising the job.</param>
/// <param name="Name">The name to show.</param>
/// <param name="Email">Where the customer is reached, shown to tell two alike apart.</param>
internal sealed record CustomerResponse(Guid Id, string Name, string Email);

/// <summary>
/// Represents a unit a job can be raised against.
/// </summary>
/// <param name="Id">The identifier to quote when raising the job.</param>
/// <param name="SerialNumber">The serial number to show.</param>
internal sealed record EquipmentResponse(Guid Id, string SerialNumber);

/// <summary>
/// Exposes the lists a caller needs before they can raise a job.
/// </summary>
/// <remarks>
/// Read-only and deliberately narrow: enough to choose from, not a general way
/// to browse the environment.
/// </remarks>
internal static class CatalogueEndpoints
{
    /// <summary>
    /// Registers the catalogue routes.
    /// </summary>
    /// <param name="app">The route builder to add to.</param>
    /// <returns>The group the routes were added to.</returns>
    public static RouteGroupBuilder MapCatalogue(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Catalogue")
            .RequireAuthorization()
            .RequireScope(CallerAuthentication.Scope);

        group.MapGet("/customers", FindAsync)
            .WithName("FindCustomers")
            .WithSummary("Finds customers whose name begins with what has been typed.");

        group.MapGet("/customers/{id:guid}/equipment", EquipmentAsync)
            .WithName("ListCustomerEquipment")
            .WithSummary("Lists the equipment registered to one customer.");

        return group;
    }

    private static async Task<IResult> FindAsync(
        FindCustomersHandler handler,
        string? name,
        int? take,
        CancellationToken cancellationToken)
    {
        var found = await handler.HandleAsync(name, take ?? 10, cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(found
            .Select(customer => new CustomerResponse(customer.Id, customer.Name, customer.Email))
            .ToArray());
    }

    private static async Task<IResult> EquipmentAsync(
        FindCustomersHandler handler,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await handler.EquipmentOfAsync(id, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return TypedResults.Problem(
                title: "Not found",
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(result.Value!
            .Select(unit => new EquipmentResponse(unit.Id, unit.SerialNumber))
            .ToArray());
    }
}
