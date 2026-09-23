namespace DynamicsCrmLab.Infrastructure.Dataverse;

/// <summary>
/// Supplies the token a connection signs in with.
/// </summary>
/// <remarks>
/// The other sign-in modes describe themselves fully in configuration, so the
/// connection can build its own credentials. Signing in as the caller cannot:
/// the token belongs to whoever is holding the request, which is something only
/// the host knows. The host supplies it through this port and the connection
/// stays unaware of how it was obtained.
/// </remarks>
public interface IDataverseAccessToken
{
    /// <summary>
    /// Acquires a token for an environment.
    /// </summary>
    /// <param name="environmentUrl">The environment the token is for.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A bearer token accepted by that environment.</returns>
    Task<string> ForAsync(Uri environmentUrl, CancellationToken cancellationToken = default);
}
