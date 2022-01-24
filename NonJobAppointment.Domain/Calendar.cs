using NonJobAppointment.Common;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NonJobAppointment.Domain;

public class Calendar
{
    private static readonly AppointmentEqualityComparer appointmentEqualityComparer = new();

    private readonly HashSet<OneOffEvent> oneOffappointments;
    private readonly HashSet<RecurringEvent> recurringAppointments;

    public Guid Id { get; }
    public DateOnly UtcDateFrom { get; }
    public DateOnly UtcDateTo { get; }

    public Calendar(
        Guid id,
        DateOnly utcDateFrom,
        DateOnly utcDateTo,
        IReadOnlyList<OneOffEvent> oneOffappointments,
        IReadOnlyList<RecurringEvent> recurringAppointments
    )
    {
        ArgumentNullException.ThrowIfNull(oneOffappointments, nameof(oneOffappointments));
        ArgumentNullException.ThrowIfNull(recurringAppointments, nameof(recurringAppointments));

        this.Id = id;
        this.UtcDateFrom = utcDateFrom;
        this.UtcDateTo = utcDateTo;
        this.oneOffappointments = BuildAppointmentIndex(oneOffappointments, AddAppointment);
        this.recurringAppointments = BuildAppointmentIndex(recurringAppointments, AddAppointment);

        static HashSet<TAppointment> BuildAppointmentIndex<TAppointment>(
            IReadOnlyList<TAppointment> appointments,
            Func<HashSet<TAppointment>, TAppointment, bool, bool> add) where TAppointment : Event
        {
            HashSet<TAppointment> index = new(appointments.Count, appointmentEqualityComparer);

            foreach (TAppointment appointment in appointments)
            {
                const bool throwOnDuplicates = true;

                add(index, appointment, throwOnDuplicates);
            }

            return index;
        }
    }

    public IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> GetAppointments()
        => this.GetAppointments(this.UtcDateFrom, this.UtcDateTo);

    public bool AddOneOffAppointment(OneOffEvent oneOffAppointment)
        => AddAppointment(this.oneOffappointments, oneOffAppointment, throwOnDuplicates: false);

    public bool AddRecurringAppointment(RecurringEvent recurringAppointment)
        => AddAppointment(this.recurringAppointments, recurringAppointment, throwOnDuplicates: false);

    private static bool AddAppointment<TAppointment>(
        HashSet<TAppointment> appointments,
        TAppointment appointment,
        bool throwOnDuplicates) where TAppointment : Event
    {
        bool added = appointments.Add(appointment);

        if (added is not true && throwOnDuplicates)
        {
            throw AppointmentAlreadyExists(appointment.Id);
        }

        return added;
    }

    private IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> GetAppointments(DateOnly from, DateOnly to)
    {
        foreach (OneOffEvent oneOff in this.oneOffappointments)
        {
            yield return OneOf.Those(oneOff);
        }

        // TODO: add overrides, deletes
        foreach (RecurringEvent recurring in this.recurringAppointments)
        {
            IEnumerable<RecurringEvent.Occurrence> occurrences = recurring.ExpandOccurrences(from, to);

            foreach (RecurringEvent.Occurrence occurrence in occurrences)
            {
                yield return OneOf.Those(occurrence);
            }
        }
    }

    private static ArgumentException AppointmentAlreadyExists(Guid id)
        => new($"An appointment with Id={id} already exists.");

    private class AppointmentEqualityComparer : IEqualityComparer<Event>
    {
        public bool Equals(Event? left, Event? right)
        {
            if (object.ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return left.Id == right.Id;
        }

        // NOTE: might need EqualityComparer<T>.Default for types other than Guid (e.g. for enums);
        public int GetHashCode([DisallowNull] Event appointment)
            => appointment.Id.GetHashCode();
    }
}