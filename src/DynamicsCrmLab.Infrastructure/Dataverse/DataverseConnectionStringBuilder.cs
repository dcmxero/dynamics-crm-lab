namespace DynamicsCrmLab.Infrastructure.Dataverse;

/// <summary>
/// Builds the connection string the Dataverse client expects.
/// </summary>
/// <remarks>
/// Kept apart from the connection itself so that the sign-in details can be
/// checked without opening a network connection.
/// </remarks>
public static class DataverseConnectionStringBuilder
{
    /// <summary>
    /// The application identifier Microsoft publishes for developer tooling.
    /// Using it means interactive sign-in needs nothing registered in Entra ID.
    /// </summary>
    private const string PublicToolingClientId = "51f81489-12ee-4a9e-aaae-a2591f45987d";

    private const string RedirectUri = "http://localhost";

    /// <summary>
    /// Builds the connection string for the given settings.
    /// </summary>
    /// <param name="options">The environment and sign-in settings.</param>
    /// <returns>A connection string accepted by the Dataverse client.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the environment address is missing, or when the client secret
    /// mode is selected without an application identifier and secret.
    /// </exception>
    public static string Build(DataverseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var url = options.Url
            ?? throw new InvalidOperationException("No environment address is configured (Dataverse:Url).");

        return options.AuthMode switch
        {
            DataverseAuthMode.ClientSecret => BuildClientSecret(options, url),
            DataverseAuthMode.Interactive => BuildInteractive(options, url),
            _ => throw new InvalidOperationException($"Unknown sign-in mode {options.AuthMode}.")
        };
    }

    private static string BuildInteractive(DataverseOptions options, Uri url) =>
        $"AuthType=OAuth;Url={url.AbsoluteUri};AppId={PublicToolingClientId};" +
        $"RedirectUri={RedirectUri};TokenCacheStorePath={options.TokenCachePath};LoginPrompt=Auto";

    private static string BuildClientSecret(DataverseOptions options, Uri url)
    {
        if (string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            throw new InvalidOperationException(
                "Client secret sign-in needs Dataverse:ClientId and Dataverse:ClientSecret.");
        }

        return $"AuthType=ClientSecret;Url={url.AbsoluteUri};" +
               $"ClientId={options.ClientId};ClientSecret={options.ClientSecret}";
    }
}
