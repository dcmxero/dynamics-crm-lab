using System.ComponentModel.DataAnnotations;

namespace DynamicsCrmLab.Infrastructure.Dataverse;

/// <summary>
/// Represents how the application signs in to a Dataverse environment.
/// </summary>
public enum DataverseAuthMode
{
    /// <summary>
    /// Opens a browser and signs in as the developer. Suited to local work.
    /// </summary>
    Interactive = 0,

    /// <summary>
    /// Uses an Entra ID application registration. Suited to services and pipelines.
    /// </summary>
    ClientSecret = 1
}

/// <summary>
/// Represents the settings needed to reach a Dataverse environment.
/// </summary>
public sealed class DataverseOptions
{
    /// <summary>
    /// The configuration section these settings are bound from.
    /// </summary>
    public const string SectionName = "Dataverse";

    /// <summary>
    /// Gets or sets the environment address, for example https://contoso.crm4.dynamics.com.
    /// </summary>
    [Required]
    public Uri? Url { get; set; }

    /// <summary>
    /// Gets or sets the way the application signs in.
    /// </summary>
    public DataverseAuthMode AuthMode { get; set; } = DataverseAuthMode.Interactive;

    /// <summary>
    /// Gets or sets the Entra ID application identifier used by <see cref="DataverseAuthMode.ClientSecret"/>.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the secret used by <see cref="DataverseAuthMode.ClientSecret"/>.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets where the sign-in token is cached so that interactive sign-in
    /// is not repeated on every run.
    /// </summary>
    public string TokenCachePath { get; set; } = "./.token-cache";
}
