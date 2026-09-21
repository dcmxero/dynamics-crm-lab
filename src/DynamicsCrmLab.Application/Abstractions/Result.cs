namespace DynamicsCrmLab.Application.Abstractions;

/// <summary>
/// Represents why a use case did not produce a value.
/// </summary>
public enum ResultFailure
{
    /// <summary>
    /// A record the use case needed is not there.
    /// </summary>
    NotFound = 0,

    /// <summary>
    /// Every record is there, but a business rule refuses the request.
    /// </summary>
    RuleBroken = 1
}

/// <summary>
/// Represents the outcome of a use case.
/// </summary>
/// <remarks>
/// A broken business rule is an expected outcome rather than an exceptional
/// one, so it travels back as a failed result instead of an exception. The kind
/// of failure travels with it, because callers answer the two differently and
/// reading the kind out of the message would tie them to its wording.
/// </remarks>
/// <typeparam name="TValue">The type produced when the use case succeeds.</typeparam>
public readonly record struct Result<TValue>
{
    internal Result(bool isSuccess, TValue? value, string? error, ResultFailure? failure)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Failure = failure;
    }

    /// <summary>
    /// Gets a value indicating whether the use case completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the produced value, or the default when the use case failed.
    /// </summary>
    public TValue? Value { get; }

    /// <summary>
    /// Gets the reason the use case failed, phrased for the user, or
    /// <see langword="null"/> when it succeeded.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets the kind of failure, or <see langword="null"/> when the use case
    /// succeeded.
    /// </summary>
    public ResultFailure? Failure { get; }
}

/// <summary>
/// Provides factory methods for <see cref="Result{TValue}"/>.
/// </summary>
public static class Result
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <typeparam name="TValue">The type produced by the use case.</typeparam>
    /// <param name="value">The produced value.</param>
    /// <returns>A result carrying <paramref name="value"/>.</returns>
    public static Result<TValue> Success<TValue>(TValue value) => new(true, value, null, null);

    /// <summary>
    /// Creates a result for a record that is not there.
    /// </summary>
    /// <typeparam name="TValue">The type the use case would have produced.</typeparam>
    /// <param name="error">What is missing, phrased for the user.</param>
    /// <returns>A result carrying <paramref name="error"/>.</returns>
    public static Result<TValue> NotFound<TValue>(string error) =>
        new(false, default, error, ResultFailure.NotFound);

    /// <summary>
    /// Creates a result for a request a business rule refuses.
    /// </summary>
    /// <typeparam name="TValue">The type the use case would have produced.</typeparam>
    /// <param name="error">Which rule refuses it, phrased for the user.</param>
    /// <returns>A result carrying <paramref name="error"/>.</returns>
    public static Result<TValue> RuleBroken<TValue>(string error) =>
        new(false, default, error, ResultFailure.RuleBroken);
}
