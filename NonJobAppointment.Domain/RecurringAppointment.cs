namespace NonJobAppointment.Domain;

public sealed record RecurringAppointment : Appointment
{
    public DateOnly StartDate { get; }
    public RecurrencePattern Pattern { get; }

    public RecurringAppointment(
        Guid id,
        string title,
        string summary,
        DateOnly startDate,
        TimeFrame timeFrame,
        long technicianId,
        int timeseetCode,
        RecurrencePattern pattern) : base(id, title, summary, timeFrame, technicianId, timeseetCode)
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

    public record Occurrence
    {
        public Key Id { get; }
        public string Title { get; }
        public string Summary { get; }
        public TimeFrame TimeFrame { get; }
        public RecurringAppointment Parent => this.Id.Parent;
        public DateOnly Date => this.Id.Date;

        internal Occurrence(Key id, string title, string summary, TimeFrame timeFrame)
        {
            ArgumentNullException.ThrowIfNull(id, nameof(id));
            ArgumentNullException.ThrowIfNull(title, nameof(title));
            ArgumentNullException.ThrowIfNull(summary, nameof(summary));

            this.Id = id;
            this.Title = title;
            this.Summary = summary;
            this.TimeFrame = timeFrame;
        }

        public readonly record struct Key
        {
            public RecurringAppointment Parent { get; }
            public DateOnly Date { get; }

            internal Key(RecurringAppointment parent, DateOnly date)
            {
                ArgumentNullException.ThrowIfNull(parent, nameof(parent));

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