namespace NonJobEvent.Domain;

public abstract record Event
{
    public Guid Id { get; }
    public string Title { get; }
    public string Summary { get; }
    public TimeFrame TimeFrame { get; }
    public int TimeseetCode { get; }

    protected Event(
        Guid id,
        string title,
        string summary,
        TimeFrame timeFrame,
        int timeseetCode)
    {
        ArgumentNullException.ThrowIfNull(title, nameof(title));
        ArgumentNullException.ThrowIfNull(summary, nameof(summary));

        this.Id = id;
        this.Title = title;
        this.Summary = summary;
        this.TimeFrame = timeFrame;
        this.TimeseetCode = timeseetCode;
    }
}
