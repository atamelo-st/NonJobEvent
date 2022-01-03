namespace NonJobAppointment.Domain;

public sealed record RecurringAppointment : Appointment
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

    public IEnumerable<Occurrence> ExpandOccurrences(DateOnly from, DateOnly to)
    {
        IEnumerable<Occurrence> occurrences = OccurenceGenerator.GenerateForPeriod(from, to, StartDate, Pattern);

        return occurrences;
    }

    public record Occurrence : AppointmentBase
    {
        public Key Id { get; }

        internal Occurrence(Key id, string title, TimeFrame timeFrame) : base(title, timeFrame)
        {
            Id = id;
        }

        public record Key
        {
            public RecurringAppointment Parent { get; }
            public DateOnly Date { get; }

            internal Key(RecurringAppointment parent, DateOnly date)
            {
                this.Parent = parent;
                this.Date = date;
            }
        }
    }
}

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