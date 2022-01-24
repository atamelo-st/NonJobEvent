namespace NonJobAppointment.Domain;

public abstract record Event
{
    public Guid Id { get; }
    public string Title { get; }
    public string Summary { get; }
    public TimeFrame TimeFrame { get; }
    public long TechnicianId { get; }
    public int TimeseetCode { get; }

    protected Event(
        Guid id,
        string title,
        string summary,
        TimeFrame timeFrame,
        long technicianId,
        int timeseetCode)
    {
        ArgumentNullException.ThrowIfNull(title, nameof(title));
        ArgumentNullException.ThrowIfNull(summary, nameof(summary));

        this.Id = id;
        this.Title = title;
        this.Summary = summary;
        this.TimeFrame = timeFrame;
        this.TechnicianId = technicianId;
        this.TimeseetCode = timeseetCode;
    }
}
