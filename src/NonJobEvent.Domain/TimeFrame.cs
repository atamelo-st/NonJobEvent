using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NonJobEvent.Domain;

public sealed record TimeFrame
{
    public static readonly TimeFrame AllDay = new(isAllDay: true);

    private readonly TimeOnly startTime;
    private readonly TimeOnly endTime;
    private readonly TimeSpan duration;
    private readonly bool isAllDay;

    public TimeOnly StartTime => this.IsAllDay ? throw CantGetTimeForAllDayEvent() : startTime;
    public TimeOnly EndTime => this.IsAllDay ? throw CantGetTimeForAllDayEvent() : endTime;
    public TimeSpan Duration => this.IsAllDay ? throw CantGetTimeForAllDayEvent() : duration;
    public bool IsAllDay => this.isAllDay;

    // DL: could you use `TimeSpan` here instead of requiring `duration` to be passed in as number of minutes?
    public static TimeFrame From(TimeOnly startTime, uint durationInMinutes)
    {
        if (!TryFrom(startTime, durationInMinutes, out var timeFrame, out var errorMessage))
        {
            throw new ArgumentException(errorMessage);
        }

        return timeFrame;
    }

    public static bool TryFrom(
        TimeOnly startTime,
        uint durationInMinutes,
        [NotNullWhen(true)] out TimeFrame? timeFrame) => TryFrom(startTime, durationInMinutes, out timeFrame, out _);

    public static bool TryFrom(
        TimeOnly startTime,
        uint durationInMinutes,
        [NotNullWhen(true)] out TimeFrame? timeFrame,
        [NotNullWhen(false)] out string? errorMessage)
    {
        TimeOnly endTime = startTime.AddMinutes(durationInMinutes, out int wrappedDays);

        // DL: why couldn't an event carry over until the next day? Something like an emergency job that starts at 11 PM and ends at 1 AM?
        if (wrappedDays > 0)
        {
            timeFrame = null;
            errorMessage = $"Time frame can't cary over on next day. Start time: {startTime}, duration {durationInMinutes} minutes.";

            return false;
        }

        TimeSpan duration = TimeSpan.FromMinutes(durationInMinutes);
        timeFrame = new TimeFrame(startTime, endTime, duration);
        errorMessage = null;

        return true;
    }

    private TimeFrame(TimeOnly startTime, TimeOnly endTime, TimeSpan duration)
    : this(isAllDay: false)
    {
        this.startTime = startTime;
        this.endTime = endTime;
        this.duration = duration;
    }

    private TimeFrame(bool isAllDay) => this.isAllDay = isAllDay;

    // TODO: find out business reqs as to what value should the props
    // have when IsAllDay is true
    private static InvalidOperationException CantGetTimeForAllDayEvent()
        => new("Can't get time for an all day event.");
}