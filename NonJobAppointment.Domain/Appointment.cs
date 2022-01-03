namespace NonJobAppointment.Domain;

public abstract record AppointmentBase
{
    public string Title { get; }
    public TimeFrame TimeFrame { get; }

    protected AppointmentBase(string title, TimeFrame timeFrame)
    {
        ArgumentNullException.ThrowIfNull(title, nameof(title));

        this.Title = title;
        this.TimeFrame = timeFrame;
    }
}

public abstract record Appointment : AppointmentBase
{
    public Guid Id { get; }
    public long TechnicianId { get; }
    public int TimeseetCode { get; }

    protected Appointment(
        Guid id,
        string title,
        TimeFrame timeFrame,
        long technicianId,
        int timeseetCode) : base(title, timeFrame)
    {
        this.Id = id;
        this.TechnicianId = technicianId;
        this.TimeseetCode = timeseetCode;
    }
}
