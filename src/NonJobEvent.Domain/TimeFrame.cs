using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NonJobEvent.Domain;

public sealed class TimeFrame
{
    public static readonly TimeFrame AllDay = new();

    private readonly TimeOnly? startTime;
    private readonly TimeOnly? endTime;

    public TimeOnly StartTime => IsAllDay is false ? startTime.Value : throw CantGetTimeForAllDayEvent();

    // TODO: track as a 'Duration' instead of time?
    public TimeOnly EndTime => IsAllDay is false ? endTime.Value : throw CantGetTimeForAllDayEvent();

    [MemberNotNullWhen(false, nameof(startTime), nameof(endTime))]
    public bool IsAllDay => startTime is null && endTime is null;

    public static TimeFrame From(TimeOnly startTime, TimeOnly endTime)
    {
        if (TryFrom(startTime, endTime, out var timeFrame, out var errorMessage) is false)
        {
            throw new ArgumentException(errorMessage);
        }

        return timeFrame;
    }

    public static bool TryFrom(
        TimeOnly? startTime,
        TimeOnly? endTime,
        [NotNullWhen(true)] out TimeFrame? timeFrame) => TryFrom(startTime, endTime, out timeFrame, out _);

    public static bool TryFrom(
        TimeOnly? startTime,
        TimeOnly? endTime,
        [NotNullWhen(true)] out TimeFrame? timeFrame,
        [NotNullWhen(false)] out string? errorMessage)
    {
        if (startTime is null || endTime is null)
        {
            timeFrame = AllDay;
            errorMessage = null;

            return true;
        }

        if (IsStartTimeBeforeEndTime(startTime.Value, endTime.Value) is false)
        {
            timeFrame = null;
            errorMessage = "Start time cannot be the same or past the end time.";

            return false;
        }

        timeFrame = new TimeFrame(startTime.Value, endTime.Value);
        errorMessage = null;

        return true;
    }

    private TimeFrame()
    {
        this.startTime = null;
        this.endTime = null;

        Debug.Assert(IsAllDay);
    }

    private TimeFrame(TimeOnly startTime, TimeOnly endTime)
    {
        this.startTime = startTime;
        this.endTime = endTime;
    }

    private static bool IsStartTimeBeforeEndTime(TimeOnly startTime, TimeOnly endTime) => startTime < endTime;

    private static InvalidOperationException CantGetTimeForAllDayEvent()
        => new("Can't get time for an all day event.");
}