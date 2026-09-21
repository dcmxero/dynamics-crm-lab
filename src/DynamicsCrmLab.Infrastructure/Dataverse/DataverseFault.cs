using System.ServiceModel;
using Microsoft.Xrm.Sdk;

namespace DynamicsCrmLab.Infrastructure.Dataverse;

/// <summary>
/// Reads the meaning out of the faults Dataverse returns.
/// </summary>
/// <remarks>
/// The platform reports every failure as the same fault type and distinguishes
/// them by a numeric code, so the codes worth acting on are named here rather
/// than spelled out at the call site.
/// </remarks>
public static class DataverseFault
{
    /// <summary>
    /// The code the platform returns when a row is asked for by an identifier
    /// nothing is stored under.
    /// </summary>
    private const int ObjectDoesNotExist = unchecked((int)0x80040217);

    /// <summary>
    /// Reports whether a fault says the row asked for is not there.
    /// </summary>
    /// <param name="exception">The fault the platform raised.</param>
    /// <returns>
    /// <see langword="true"/> when the row does not exist; otherwise
    /// <see langword="false"/>.
    /// </returns>
    public static bool IsRecordNotFound(FaultException<OrganizationServiceFault>? exception) =>
        exception?.Detail?.ErrorCode == ObjectDoesNotExist;
}
