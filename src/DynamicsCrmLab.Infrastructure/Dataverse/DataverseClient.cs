using System.ServiceModel;
using DynamicsCrmLab.Domain.Common;
using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Infrastructure.Dataverse;

/// <summary>
/// Talks to a Dataverse environment over the official client.
/// </summary>
/// <remarks>
/// <see cref="ServiceClient"/> is thread safe but expensive to create, so it is
/// opened once, lazily, and reused for the lifetime of the application.
/// </remarks>
/// <param name="options">The environment and sign-in settings.</param>
public sealed class DataverseClient(IOptions<DataverseOptions> options) : IDataverseClient, IDisposable
{
    private readonly Lazy<ServiceClient> _client = new(
        () => Connect(options.Value),
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <inheritdoc/>
    public Task<Guid> CreateAsync(Entity record, CancellationToken cancellationToken = default) =>
        Refusable(() => _client.Value.CreateAsync(record, cancellationToken));

    /// <inheritdoc/>
    public async Task<Entity?> RetrieveAsync(
        string entityName,
        Guid id,
        ColumnSet columns,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _client.Value
                .RetrieveAsync(entityName, id, columns, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (FaultException<OrganizationServiceFault> fault) when (DataverseFault.IsRecordNotFound(fault))
        {
            // Asking for a row that is not there is an ordinary answer to an
            // ordinary question, so callers get nothing rather than a fault.
            return null;
        }
    }

    /// <inheritdoc/>
    public Task<EntityCollection> RetrieveMultipleAsync(
        QueryBase query,
        CancellationToken cancellationToken = default) =>
        _client.Value.RetrieveMultipleAsync(query, cancellationToken);

    /// <inheritdoc/>
    public Task<OrganizationResponse> ExecuteAsync(
        OrganizationRequest request,
        CancellationToken cancellationToken = default) =>
        Refusable(() => _client.Value.ExecuteAsync(request, cancellationToken));

    /// <inheritdoc/>
    public Task DeleteAsync(string entityName, Guid id, CancellationToken cancellationToken = default) =>
        _client.Value.DeleteAsync(entityName, id, cancellationToken);

    /// <inheritdoc/>
    public Task UpdateAsync(Entity record, CancellationToken cancellationToken = default) =>
        Refusable(() => _client.Value.UpdateAsync(record, cancellationToken));

    /// <summary>
    /// Runs a write and reports a refusal as the broken rule it is.
    /// </summary>
    /// <remarks>
    /// A value a column will not hold, or a rule a plug-in enforces, is the
    /// platform answering the request rather than failing at it, and the layers
    /// above already know how to carry a broken rule back to the caller. They
    /// do not know what a Dataverse fault is, and should not have to.
    /// </remarks>
    private static async Task<TResult> Refusable<TResult>(Func<Task<TResult>> write)
    {
        try
        {
            return await write().ConfigureAwait(false);
        }
        catch (FaultException<OrganizationServiceFault> fault) when (DataverseFault.IsRefused(fault))
        {
            throw new DomainException(fault.Detail.Message, fault);
        }
    }

    private static async Task Refusable(Func<Task> write)
    {
        try
        {
            await write().ConfigureAwait(false);
        }
        catch (FaultException<OrganizationServiceFault> fault) when (DataverseFault.IsRefused(fault))
        {
            throw new DomainException(fault.Detail.Message, fault);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_client.IsValueCreated)
        {
            _client.Value.Dispose();
        }
    }

    private static ServiceClient Connect(DataverseOptions settings)
    {
        var client = new ServiceClient(DataverseConnectionStringBuilder.Build(settings));

        return client.IsReady
            ? client
            : throw new InvalidOperationException(
                $"Could not connect to {settings.Url}: {client.LastError}", client.LastException);
    }
}
