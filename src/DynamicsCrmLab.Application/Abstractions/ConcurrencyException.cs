namespace DynamicsCrmLab.Application.Abstractions;

/// <summary>
/// Thrown when a record was changed by somebody else while this request was
/// deciding what to do with it.
/// </summary>
/// <remarks>
/// Every use case here reads a job, asks the aggregate to make a move, and
/// writes the result. Between the read and the write somebody else can have
/// moved the same job on, and a blind write would then quietly undo their work:
/// two people closing the same job at once, and the second one's account of
/// what was done replacing the first with nobody told.
///
/// It is not a broken business rule, because nothing about the request was
/// wrong. It is the same request arriving too late, and the honest answer is to
/// say so and let the caller read the job again.
/// </remarks>
public sealed class ConcurrencyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
    /// </summary>
    public ConcurrencyException()
        : this("The record was changed by somebody else. Read it again and retry.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
    /// </summary>
    /// <param name="message">What to tell the caller.</param>
    public ConcurrencyException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
    /// </summary>
    /// <param name="message">What to tell the caller.</param>
    /// <param name="innerException">The failure underneath.</param>
    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
