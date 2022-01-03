namespace NonJobAppointment.Domain;


public abstract record Appointment
{
    public Guid Id { get; }
    public string Title { get; }
    public TimeFrame TimeFrame { get; }
    public long TechnicianId { get; }
    public int TimeseetCode { get; }

    protected Appointment(
        Guid id,
        string title,
        TimeFrame timeFrame,
        long technicianId,
        int timeseetCode)
    {
        ArgumentNullException.ThrowIfNull(title, nameof(title));

        this.Id = id;
        this.Title = title;
        this.TimeFrame = timeFrame;
        this.TechnicianId = technicianId;
        this.TimeseetCode = timeseetCode;
    }
}

public record OneOffAppointment(
        Guid Id,
        string Title,
        DateOnly Date,
        TimeFrame TimeFrame,
        long TechnicianId,
        int TimeseetCode
) : Appointment(Id, Title, TimeFrame, TechnicianId, TimeseetCode);

public record RecurrencePattern();

public class OccurenceGenerator
{
    public static IEnumerable<RecurringAppointment.Occurrence> GenerateForPeriod(
        DateOnly from,
        DateOnly to,
        DateOnly startDate,
        RecurrencePattern pattern)
    {
        throw new NotImplementedException();
    }
}

public record RecurringAppointment : Appointment
{
    public DateOnly StartDate { get; }
    public RecurrencePattern Pattern { get; }

    public RecurringAppointment(
        Guid id,
        string title,
        DateOnly startDate,
        TimeFrame timeFrame,
        long technicianId,
        int timeseetCode,
        RecurrencePattern pattern) : base(id, title, timeFrame, technicianId, timeseetCode)
    {
        ArgumentNullException.ThrowIfNull(pattern, nameof(pattern));

        this.StartDate = startDate;
        this.Pattern = pattern;
    }

    public record Occurrence(string Title, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime);

    public IEnumerable<Occurrence> ExpandOccurrences(DateOnly from, DateOnly to)
    {
        IEnumerable<Occurrence> occurrences = OccurenceGenerator.GenerateForPeriod(from, to, StartDate, Pattern);

        return occurrences;
    }
}

public readonly record struct TimeFrame
{
    private readonly bool isAllDay;

    public TimeOnly? StartTime { get; }
    public TimeOnly? EndTime { get; }
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
                throw new ArgumentException("For non-all-day events both start and end times should be specified.");
            }

            if (startTime >= endTime)
            {
                throw new ArgumentException("Start time can't be the same or past the end time.", nameof(startTime));
            }
        }

        return isAllDay;
    }
}