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

    /// <summary>
    /// Reports whether a fault says the platform refused a value.
    /// </summary>
    /// <remarks>
    /// A value outside the range a column allows, or a rule a plug-in enforces,
    /// is the platform answering the request rather than failing at it. The
    /// message it carries is written for a person, so it travels with it.
    /// </remarks>
    /// <param name="exception">The fault the platform raised.</param>
    /// <returns>
    /// <see langword="true"/> when the platform refused the request; otherwise
    /// <see langword="false"/>.
    /// </returns>
    public static bool IsRefused(FaultException<OrganizationServiceFault>? exception) =>
        exception?.Detail?.ErrorCode is ValueOutOfRange or BusinessRuleRefused;

    /// <summary>
    /// Reports whether a fault says the row was changed by somebody else since
    /// it was read.
    /// </summary>
    /// <param name="exception">The fault the platform raised.</param>
    /// <returns>
    /// <see langword="true"/> when the update lost a race; otherwise
    /// <see langword="false"/>.
    /// </returns>
    public static bool IsStale(FaultException<OrganizationServiceFault>? exception) =>
        exception?.Detail?.ErrorCode == ConcurrencyVersionMismatch;

    /// <summary>
    /// Reports whether a fault says a record already exists under a key this
    /// one tried to claim.
    /// </summary>
    /// <param name="exception">The fault the platform raised.</param>
    /// <returns>
    /// <see langword="true"/> when an alternate key was already taken;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsDuplicateKey(FaultException<OrganizationServiceFault>? exception) =>
        exception?.Detail?.ErrorCode == DuplicateRecord;

    /// <summary>The code the platform returns for a value a column will not hold.</summary>
    private const int ValueOutOfRange = unchecked((int)0x8004432F);

    /// <summary>The code a plug-in raising a rule of its own comes back as.</summary>
    private const int BusinessRuleRefused = unchecked((int)0x80040265);

    /// <summary>The code the platform returns when the row version no longer matches.</summary>
    private const int ConcurrencyVersionMismatch = unchecked((int)0x80060882);

    /// <summary>The code the platform returns when an alternate key is already taken.</summary>
    private const int DuplicateRecord = unchecked((int)0x80060892);
}
