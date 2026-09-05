using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace DynamicsCrmLab.Infrastructure.Dataverse;

/// <summary>
/// Holds one shared connection to a Dataverse environment.
/// </summary>
/// <remarks>
/// <see cref="ServiceClient"/> is thread safe but expensive to create, so it is
/// opened once, lazily, and reused for the lifetime of the application.
/// </remarks>
/// <param name="options">The environment and sign-in settings.</param>
public sealed class DataverseConnectionProvider(IOptions<DataverseOptions> options)
    : IDataverseConnectionProvider, IDisposable
{
    private readonly Lazy<ServiceClient> _client = new(
        () => Connect(options.Value),
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <inheritdoc/>
    public IOrganizationService GetService() => _client.Value;

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
