using System.Diagnostics.CodeAnalysis;

namespace NonJobAppointment.Domain;

public readonly record struct TimeFrame
{
    private readonly bool isAllDay;

    public TimeOnly? StartTime { get; }
    public TimeOnly? EndTime { get; }
    
    [MemberNotNullWhen(false, nameof(StartTime), nameof(EndTime))]
    public bool IsAllDay => this.isAllDay;

    public TimeFrame(TimeOnly? startTime, TimeOnly? endTime)
    {
        bool isAllDay = CheckStartEndTimes(startTime, endTime);

        this.StartTime = startTime;
        this.EndTime = endTime;
        this.isAllDay = isAllDay;
    }

    public TimeFrame() : this(null, null)
    {
    }

    private static bool CheckStartEndTimes(TimeOnly? startTime, TimeOnly? endTime)
    {
        bool isAllDay = startTime is null && endTime is null;

        if (isAllDay is not true)
        {
            if (startTime is null || endTime is null)
            {
                throw new ArgumentException("For non-all-day events both start and end times should be specified.", $"{nameof(startTime)}, {nameof(endTime)}");
            }

            if (startTime >= endTime)
            {
                throw new ArgumentException("Start time cannot be the same or past the end time.", nameof(startTime));
            }
        }

        return isAllDay;
    }
}