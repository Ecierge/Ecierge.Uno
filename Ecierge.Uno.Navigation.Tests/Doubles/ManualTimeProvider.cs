namespace Ecierge.Uno.Navigation.Tests.Doubles;

/// <summary>
/// A time provider that moves only when a test says so.
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private long timestamp;

    /// <inheritdoc/>
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <inheritdoc/>
    public override long GetTimestamp() => Interlocked.Read(ref timestamp);

    /// <summary>
    /// Moves the time on.
    /// </summary>
    /// <param name="time">How far.</param>
    public void Advance(TimeSpan time) => Interlocked.Add(ref timestamp, time.Ticks);
}
