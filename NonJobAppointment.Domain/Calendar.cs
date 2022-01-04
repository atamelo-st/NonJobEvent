using NonJobAppointment.Common;

namespace NonJobAppointment.Domain;

public class Calendar
{
    private readonly IReadOnlyList<OneOffAppointment> oneOffappointments;
    private readonly IReadOnlyList<RecurringAppointment> recurringAppointments;

    public Guid Id { get; }
    public DateOnly DateFrom { get; }
    public DateOnly DateTo { get; }

    public Calendar(
        Guid id,
        DateOnly dateFrom,
        DateOnly dateTo,
        IReadOnlyList<OneOffAppointment> oneOffappointments,
        IReadOnlyList<RecurringAppointment> recurringAppointments
    )
    {
        ArgumentNullException.ThrowIfNull(oneOffappointments, nameof(oneOffappointments));
        ArgumentNullException.ThrowIfNull(recurringAppointments, nameof(recurringAppointments));
        this.Id = id;
        this.DateFrom = dateFrom;
        this.DateTo = dateTo;
        this.oneOffappointments = oneOffappointments;
        this.recurringAppointments = recurringAppointments;
    }

    public IEnumerable<OneOf<OneOffAppointment, RecurringAppointment.Occurrence>> GetAppointments()
        => this.GetAppointments(this.DateFrom, this.DateTo);

    // TODO: do we really need the override taking in dates??
    public IEnumerable<OneOf<OneOffAppointment, RecurringAppointment.Occurrence>> GetAppointments(DateOnly from, DateOnly to)
    {
        foreach (OneOffAppointment oneOff in this.oneOffappointments)
        {
            yield return OneOf.Those(oneOff);
        }

        // TODO: add overrides, deletes
        foreach (RecurringAppointment recurring in this.recurringAppointments)
        {
            IEnumerable<RecurringAppointment.Occurrence> occurrences = recurring.ExpandOccurrences(from, to);

            foreach (RecurringAppointment.Occurrence occurrence in occurrences)
            {
                yield return OneOf.Those(occurrence);
            }
        }
    }
}