using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NonJobAppointment.Domain;

public sealed class TimeFrame
{
    public static readonly TimeFrame AllDay = new();

    private readonly TimeOnly? startTime;
    private readonly TimeOnly? endTime;

    public TimeOnly StartTime => this.IsAllDay is false ? this.startTime.Value : throw CantGetTimeForAllDayEvent();

    // TODO: track as a 'Duration' instead of time?
    public TimeOnly EndTime => this.IsAllDay is false ? this.endTime.Value : throw CantGetTimeForAllDayEvent();

    [MemberNotNullWhen(false, nameof(startTime), nameof(endTime))]
    public bool IsAllDay => this.startTime is null && this.endTime is null;

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
            errorMessage = ErrorMessages.StartTimePastEndTime;

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
        => new(ErrorMessages.CantGetTimeForAllDayEvent);

    private static class ErrorMessages
    {
        public static readonly string CantGetTimeForAllDayEvent = "Can't get time for an all day event.";
        public static readonly string StartTimePastEndTime = "Start time cannot be the same or past the end time.";
    }
}