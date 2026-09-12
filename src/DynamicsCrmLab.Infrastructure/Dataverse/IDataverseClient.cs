using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Infrastructure.Dataverse;

/// <summary>
/// Defines the handful of Dataverse operations the repositories actually use.
/// </summary>
/// <remarks>
/// Narrower than the SDK service interface on purpose: repositories depend only
/// on what they call, and a test double is a few lines rather than two dozen
/// members that throw.
/// </remarks>
public interface IDataverseClient
{
    /// <summary>
    /// Creates a row.
    /// </summary>
    /// <param name="record">The row to create.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The identifier assigned to the new row.</returns>
    Task<Guid> CreateAsync(Entity record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a single row.
    /// </summary>
    /// <param name="entityName">The logical name of the table.</param>
    /// <param name="id">The identifier of the row.</param>
    /// <param name="columns">The columns to return.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The row.</returns>
    Task<Entity> RetrieveAsync(
        string entityName,
        Guid id,
        ColumnSet columns,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a query.
    /// </summary>
    /// <param name="query">The query to run.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>One page of matching rows.</returns>
    Task<EntityCollection> RetrieveMultipleAsync(
        QueryBase query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a named Dataverse operation, such as WhoAmI or a custom API.
    /// </summary>
    /// <param name="request">The operation to run.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The response the platform returned.</returns>
    Task<OrganizationResponse> ExecuteAsync(
        OrganizationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes changed columns back to a row.
    /// </summary>
    /// <param name="record">The row carrying the columns to write.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes once the row has been written.</returns>
    Task UpdateAsync(Entity record, CancellationToken cancellationToken = default);
}
