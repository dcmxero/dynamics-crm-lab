using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

namespace DynamicsCrmLab.Api;

/// <summary>
/// Establishes who is calling the API.
/// </summary>
/// <remarks>
/// Every route speaks for a person: a job is raised by somebody, assigned by
/// somebody and closed by somebody. Until the caller is known the API can only
/// act as itself, which would leave the environment open to anyone who can
/// reach the port and would record every change against one service identity.
/// </remarks>
internal static class CallerAuthentication
{
    /// <summary>
    /// The scope the API publishes. A token without it may be a valid token for
    /// somebody, but it is not consent to work with their work orders.
    /// </summary>
    public const string Scope = "access_as_user";

    /// <summary>
    /// Adds bearer token validation against the configured tenant.
    /// </summary>
    /// <param name="services">The container to add to.</param>
    /// <param name="configuration">The configuration the tenant settings are bound from.</param>
    /// <returns>The same container, to allow chaining.</returns>
    public static IServiceCollection AddCallerAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"))
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();

        services.AddAuthorization();

        return services;
    }
}
