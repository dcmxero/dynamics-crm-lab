namespace DynamicsCrmLab.Domain.Common;

/// <summary>
/// Represents a violated business rule.
/// </summary>
/// <remarks>
/// Distinct from the framework exception types so that outer layers can tell a
/// broken rule apart from a technical failure and report it to the user.
/// </remarks>
public sealed class DomainException : Exception
{
    /// <summary>
    /// Initialises the exception with the rule that was violated.
    /// </summary>
    /// <param name="message">The rule that was violated, phrased for the user.</param>
    public DomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initialises the exception with the rule that was violated and whatever
    /// reported it.
    /// </summary>
    /// <remarks>
    /// A rule the platform enforces reaches the application as a fault. The
    /// message belongs to the user and the fault belongs in the log, so both
    /// travel together.
    /// </remarks>
    /// <param name="message">The rule that was violated, phrased for the user.</param>
    /// <param name="innerException">What reported the broken rule.</param>
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
