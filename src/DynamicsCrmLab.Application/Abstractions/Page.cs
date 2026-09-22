namespace DynamicsCrmLab.Application.Abstractions;

/// <summary>
/// Represents one page of results and where the next one starts.
/// </summary>
/// <remarks>
/// The cursor is opaque on purpose. A caller that is handed a page number, or a
/// count of rows to skip, ends up reading the same row twice or missing one
/// whenever the underlying set changes between requests; a cursor the store
/// issues carries whatever that store needs to carry on from.
/// </remarks>
/// <typeparam name="TItem">What the page holds.</typeparam>
/// <param name="Items">The results on this page.</param>
/// <param name="NextCursor">
/// What to ask for the next page with, or <see langword="null"/> when this page
/// is the last.
/// </param>
public sealed record Page<TItem>(IReadOnlyList<TItem> Items, string? NextCursor);
