namespace DynamicsCrmLab.Application.Abstractions;

/// <summary>
/// Represents the outcome of a use case.
/// </summary>
/// <remarks>
/// A broken business rule is an expected outcome rather than an exceptional
/// one, so it travels back as a failed result instead of an exception.
/// </remarks>
/// <typeparam name="TValue">The type produced when the use case succeeds.</typeparam>
public readonly record struct Result<TValue>
{
    internal Result(bool isSuccess, TValue? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
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
    public static Result<TValue> Success<TValue>(TValue value) => new(true, value, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <typeparam name="TValue">The type the use case would have produced.</typeparam>
    /// <param name="error">The reason the use case failed, phrased for the user.</param>
    /// <returns>A result carrying <paramref name="error"/>.</returns>
    public static Result<TValue> Failure<TValue>(string error) => new(false, default, error);
}
