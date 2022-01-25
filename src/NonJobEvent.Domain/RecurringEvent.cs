namespace NonJobEvent.Domain;

public sealed record RecurringEvent : Event
{
    public DateOnly StartDate { get; }
    public RecurrencePattern Pattern { get; }
    public EndCondition WhenToEnd { get; }

    public RecurringEvent(
        Guid id,
        string title,
        string summary,
        DateOnly startDate,
        TimeFrame timeFrame,
        int timeseetCode,
        RecurrencePattern pattern,
        EndCondition endCondition) : base(id, title, summary, timeFrame, timeseetCode)
    {
        ArgumentNullException.ThrowIfNull(pattern, nameof(pattern));
        ArgumentNullException.ThrowIfNull(endCondition, nameof(endCondition));

        this.StartDate = startDate;
        this.Pattern = pattern;
        this.WhenToEnd = endCondition;
    }

    public IEnumerable<Occurrence> ExpandOccurrences(DateOnly from, DateOnly to)
    {
        IEnumerable<Occurrence> occurrences = OccurenceGenerator.GenerateForPeriod(from, to, StartDate, Pattern);

        return occurrences;
    }

    public sealed record Occurrence
    {
        public Key Id { get; }
        public string Title { get; }
        public string Summary { get; }
        public TimeFrame TimeFrame { get; }
        public RecurringEvent Parent => Id.Parent;
        public DateOnly Date => Id.Date;

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
            public RecurringEvent Parent { get; }
            public DateOnly Date { get; }

            internal Key(RecurringEvent parent, DateOnly date)
            {
                ArgumentNullException.ThrowIfNull(parent, nameof(parent));

                this.Parent = parent;
                this.Date = date;
            }
        }
    }

    public sealed record EndCondition
    {

    }
}

public class OccurenceGenerator
{
    public static IEnumerable<RecurringEvent.Occurrence> GenerateForPeriod(
        DateOnly from,
        DateOnly to,
        DateOnly startDate,
        RecurrencePattern pattern)
    {
        throw new NotImplementedException();
    }
}