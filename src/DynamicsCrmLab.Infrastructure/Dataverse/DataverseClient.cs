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
        _client.Value.CreateAsync(record, cancellationToken);

    /// <inheritdoc/>
    public Task<Entity> RetrieveAsync(
        string entityName,
        Guid id,
        ColumnSet columns,
        CancellationToken cancellationToken = default) =>
        _client.Value.RetrieveAsync(entityName, id, columns, cancellationToken);

    /// <inheritdoc/>
    public Task<EntityCollection> RetrieveMultipleAsync(
        QueryBase query,
        CancellationToken cancellationToken = default) =>
        _client.Value.RetrieveMultipleAsync(query, cancellationToken);

    /// <inheritdoc/>
    public Task UpdateAsync(Entity record, CancellationToken cancellationToken = default) =>
        _client.Value.UpdateAsync(record, cancellationToken);

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
