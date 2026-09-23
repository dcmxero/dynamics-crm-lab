using DynamicsCrmLab.Infrastructure.Dataverse;
using Microsoft.Identity.Web;

namespace DynamicsCrmLab.Api;

/// <summary>
/// Exchanges the caller's token for one the environment accepts.
/// </summary>
/// <remarks>
/// The token the caller presents was issued for this API and the environment
/// will not take it. On-behalf-of asks the tenant for a second token for the
/// same person, so the environment sees them rather than the API, and their own
/// roles decide what they may do.
/// </remarks>
/// <param name="tokenAcquisition">Performs the exchange against the tenant.</param>
internal sealed class CallerDataverseToken(ITokenAcquisition tokenAcquisition) : IDataverseAccessToken
{
    /// <inheritdoc/>
    public Task<string> ForAsync(Uri environmentUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environmentUrl);

        // Dataverse names its delegated scope after the environment itself, so
        // a token for one environment is no good for another.
        var scope = $"{environmentUrl.GetLeftPart(UriPartial.Authority)}/user_impersonation";

        return tokenAcquisition.GetAccessTokenForUserAsync([scope]);
    }
}
