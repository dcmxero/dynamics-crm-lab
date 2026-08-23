namespace DynamicsCrmLab.Domain.Common;

/// <summary>
/// Represents a violated business rule.
/// </summary>
/// <remarks>
/// Distinct from the framework exception types so that outer layers can tell a
/// broken rule apart from a technical failure and report it to the user.
/// </remarks>
/// <param name="message">The rule that was violated, phrased for the user.</param>
public sealed class DomainException(string message) : Exception(message);
