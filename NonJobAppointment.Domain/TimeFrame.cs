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

    private TimeFrame()
    {
        this.startTime = null;
        this.endTime = null;

        Debug.Assert(IsAllDay);
    }

    public TimeFrame(TimeOnly startTime, TimeOnly endTime)
    {
        if (startTime >= endTime)
        {
            throw new ArgumentException("Start time cannot be the same or past the end time.", nameof(startTime));
        }

        this.startTime = startTime;
        this.endTime = endTime;
    }

    private static InvalidOperationException CantGetTimeForAllDayEvent()
        => new("Can't get time for an all day event.");
}