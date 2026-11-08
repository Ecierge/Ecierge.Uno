namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// The result of one navigator navigating to one segment.
/// </summary>
public record struct NavigationResult
{
    private IReadOnlyList<string>? errors;

    /// <summary>
    /// Gets the segment that the navigator navigated to, or <see langword="null"/> if the navigation failed.
    /// </summary>
    public RouteSegment? SegmentNavigated { get; private set; }

    /// <summary>
    /// Gets the object that the navigation produced, for example the new view.
    /// </summary>
    public object? Result { get; private set; }

    /// <summary>
    /// Gets a value that tells that the route already corresponds to the current route, so no navigation was necessary.
    /// </summary>
    public bool IsSkipped { get; private set; }

    /// <summary>
    /// Gets a value that tells that the navigation rules refused the route.
    /// </summary>
    public bool IsDenied { get; private set; }

    /// <summary>
    /// Gets the reasons why the navigation failed, or an empty list if it did not.
    /// </summary>
    public IReadOnlyList<string> Errors
    {
        readonly get => errors ?? Array.Empty<string>();
        private set => errors = value;
    }

    /// <summary>
    /// Gets a value that tells that the navigator navigated to the segment.
    /// </summary>
    [MemberNotNullWhen(true, nameof(SegmentNavigated), nameof(Request))]
    public readonly bool Success => SegmentNavigated is not null;

    /// <summary>
    /// Gets the request that was navigated, with the data that the navigation added to it, or
    /// <see langword="null"/> if the navigation failed.
    /// </summary>
    public NavigationRequest? Request { get; init; }

    /// <summary>
    /// Creates the result of a successful navigation.
    /// </summary>
    /// <param name="request">The request that was navigated.</param>
    /// <param name="result">The object that the navigation produced.</param>
    /// <param name="isSkipped">Whether the route was already current.</param>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    public NavigationResult(NavigationRequest request, object? result = null, bool isSkipped = false)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        SegmentNavigated = request.RouteSegment;
        Result = result;
        IsSkipped = isSkipped;
        errors = null;
        IsDenied = false;
    }

    /// <summary>
    /// Creates the result of a failed navigation.
    /// </summary>
    /// <param name="errors">The reasons of the failure.</param>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
    public NavigationResult(params string[] errors) => this.errors = errors ?? throw new ArgumentNullException(nameof(errors));

    /// <summary>
    /// Creates the result of a failed navigation.
    /// </summary>
    /// <param name="errors">The reasons of the failure.</param>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
    public NavigationResult(IReadOnlyList<string> errors) => this.errors = errors ?? throw new ArgumentNullException(nameof(errors));

    /// <summary>
    /// Creates the result of a navigation that the navigation rules refused.
    /// </summary>
    /// <param name="reasons">The reasons that the rules gave.</param>
    /// <returns>The failed result.</returns>
    public static NavigationResult Denied(IReadOnlyList<string> reasons)
     => new(reasons) { IsDenied = true };
}

/// <summary>
/// The result of one navigator navigating to one segment, with a typed result object.
/// </summary>
/// <typeparam name="T">The type of the object that the navigation produced.</typeparam>
public record struct NavigationResult<T>
{
    private IReadOnlyList<string>? errors;

    /// <summary>
    /// Gets the segment that the navigator navigated to, or <see langword="null"/> if the navigation failed.
    /// </summary>
    public RouteSegment? SegmentNavigated { get; private set; }

    /// <summary>
    /// Gets the object that the navigation produced.
    /// </summary>
    public T? Result { get; private set; }

    /// <summary>
    /// Gets the reasons why the navigation failed, or an empty list if it did not.
    /// </summary>
    public IReadOnlyList<string> Errors
    {
        readonly get => errors ?? Array.Empty<string>();
        private set => errors = value;
    }

    /// <summary>
    /// Gets a value that tells that the navigator navigated to the segment.
    /// </summary>
    [MemberNotNullWhen(true, nameof(SegmentNavigated), nameof(Request))]
    public readonly bool Success => SegmentNavigated is not null;

    /// <summary>
    /// Gets the request that was navigated, or <see langword="null"/> if the navigation failed.
    /// </summary>
    public NavigationRequest? Request { get; init; }

    /// <summary>
    /// Creates the result of a successful navigation.
    /// </summary>
    /// <param name="request">The request that was navigated.</param>
    /// <param name="result">The object that the navigation produced.</param>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    public NavigationResult(NavigationRequest request, T? result = default)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        SegmentNavigated = request.RouteSegment;
        Result = result;
        errors = null;
    }

    /// <summary>
    /// Creates the result of a failed navigation.
    /// </summary>
    /// <param name="args">The reasons of the failure.</param>
    /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null"/>.</exception>
    public NavigationResult(params string[] args) => errors = args ?? throw new ArgumentNullException(nameof(args));

    /// <summary>
    /// Creates the result of a failed navigation.
    /// </summary>
    /// <param name="args">The reasons of the failure.</param>
    /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null"/>.</exception>
    public NavigationResult(IReadOnlyList<string> args) => errors = args ?? throw new ArgumentNullException(nameof(args));

    /// <summary>
    /// Converts a typed result to an untyped one.
    /// </summary>
    /// <param name="result">The typed result.</param>
    public static implicit operator NavigationResult(NavigationResult<T> result)
    {
        if (result.Success)
            return new(result.Request, result.Result);
        else
            return new(result.Errors);
    }
}
