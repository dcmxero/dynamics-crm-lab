using Microsoft.Xrm.Sdk;

namespace DynamicsCrmLab.Infrastructure.Dataverse;

/// <summary>
/// Provides a connected <see cref="IOrganizationService"/>.
/// </summary>
/// <remarks>
/// Repositories receive an open connection and stay unaware of how the sign-in
/// was performed. Named after the connection rather than the SDK factory type
/// of the same name, which it deliberately does not implement.
/// </remarks>
public interface IDataverseConnectionProvider
{
    /// <summary>
    /// Returns the shared connection, opening it on first use.
    /// </summary>
    /// <returns>A connected organization service.</returns>
    IOrganizationService GetService();
}
