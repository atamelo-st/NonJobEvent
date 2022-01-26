using EWSoftware.PDI;

namespace NonJobEvent.Domain;

public sealed record RecurringEvent : Event
{
    public DateOnly StartDate { get; }
    public RecurrencePattern Pattern { get; }

    public RecurringEvent(
        Guid id,
        string title,
        string summary,
        DateOnly startDate,
        TimeFrame timeFrame,
        int timeseetCode,
        RecurrencePattern pattern) : base(id, title, summary, timeFrame, timeseetCode)
    {
        ArgumentNullException.ThrowIfNull(pattern, nameof(pattern));

        this.StartDate = startDate;
        this.Pattern = pattern;
    }

    public IEnumerable<Occurrence> ExpandOccurrences(DateOnly from, DateOnly to)
    {
        // TODO: check that 'to - from <= some_time'

        IEnumerable<DateTime> dateTimes = OccurenceGenerator.GenerateDatesForPeriod(from, to, this.StartDate, this.Pattern);

        foreach (DateTime dateTime in dateTimes)
        {
            DateOnly date = DateOnly.FromDateTime(dateTime);

            yield return new Occurrence(parent: this, date, this.Title, this.Summary, this.TimeFrame);
        }
    }

    public bool OccursOn(DateOnly specificDay)
    {
        // TODO: not sure if +-1 day is needed, ideally we should be abel to pass just [specificDay; specificDay] range
        DateOnly dayBefore = specificDay.AddDays(-1);
        DateOnly dayAfter = specificDay.AddDays(+1);

        IEnumerable<DateTime> dates =
            OccurenceGenerator.GenerateDatesForPeriod(from: dayBefore, to: dayAfter, this.StartDate, this.Pattern);

        bool occurs = dates.Any(date => DateOnly.FromDateTime(date) == specificDay);

        return occurs;
    }

    public sealed record Occurrence
    {
        public RecurringEvent Parent { get; }
        public DateOnly Date { get; }
        public string Title { get; }
        public string Summary { get; }
        public TimeFrame TimeFrame { get; }

        internal Occurrence(RecurringEvent parent, DateOnly date, string title, string summary, TimeFrame timeFrame)
        {
            ArgumentNullException.ThrowIfNull(parent, nameof(parent));
            ArgumentNullException.ThrowIfNull(title, nameof(title));
            ArgumentNullException.ThrowIfNull(summary, nameof(summary));
            ArgumentNullException.ThrowIfNull(timeFrame, nameof(timeFrame));

            this.Parent = parent;
            this.Title = title;
            this.Summary = summary;
            this.TimeFrame = timeFrame;
        }

        //public readonly record struct Key
        //{
        //    public RecurringEvent Parent { get; }
        //    public DateOnly Date { get; }

        //    internal Key(RecurringEvent parent, DateOnly date)
        //    {
        //        ArgumentNullException.ThrowIfNull(parent, nameof(parent));

        //        this.Parent = parent;
        //        this.Date = date;
        //    }
        //}
    }
}

public class OccurenceGenerator
{
    public static IEnumerable<DateTime> GenerateDatesForPeriod(
        DateOnly from,
        DateOnly to,
        DateOnly startDate,
        RecurrencePattern pattern)
    {
        DateTime startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
        DateTime fromDateTime = from.ToDateTime(TimeOnly.MinValue);
        DateTime toDateTime = to.ToDateTime(TimeOnly.MinValue);

        Recurrence recurrence = new(pattern.Value) { StartDateTime = startDateTime };

        DateTimeCollection dates =  recurrence.InstancesBetween(fromDateTime, toDateTime);

        return dates;
    }
}