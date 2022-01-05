using NonJobAppointment.Common;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NonJobAppointment.Domain;

public class Calendar
{
    private static readonly AppointmentEqualityComparer appointmentEqualityComparer = new();

    private readonly HashSet<OneOffAppointment> oneOffappointments;
    private readonly HashSet<RecurringAppointment> recurringAppointments;

    public Guid Id { get; }
    public DateOnly UtcDateFrom { get; }
    public DateOnly UtcDateTo { get; }

    public Calendar(
        Guid id,
        DateOnly utcDateFrom,
        DateOnly utcDateTo,
        IReadOnlyList<OneOffAppointment> oneOffappointments,
        IReadOnlyList<RecurringAppointment> recurringAppointments
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
            Func<HashSet<TAppointment>, TAppointment, bool, bool> add) where TAppointment : Appointment
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

    public IEnumerable<OneOf<OneOffAppointment, RecurringAppointment.Occurrence>> GetAppointments()
        => this.GetAppointments(this.UtcDateFrom, this.UtcDateTo);

    public bool AddOneOffAppointment(OneOffAppointment oneOffAppointment)
        => AddAppointment(this.oneOffappointments, oneOffAppointment, throwOnDuplicates: false);

    public bool AddRecurringAppointment(RecurringAppointment recurringAppointment)
        => AddAppointment(this.recurringAppointments, recurringAppointment, throwOnDuplicates: false);

    private static bool AddAppointment<TAppointment>(
        HashSet<TAppointment> appointments,
        TAppointment appointment,
        bool throwOnDuplicates) where TAppointment : Appointment
    {
        bool added = appointments.Add(appointment);

        if (added is not true && throwOnDuplicates)
        {
            throw AppointmentAlreadyExists(appointment.Id);
        }

        return added;
    }

    private IEnumerable<OneOf<OneOffAppointment, RecurringAppointment.Occurrence>> GetAppointments(DateOnly from, DateOnly to)
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

    private static ArgumentException AppointmentAlreadyExists(Guid id)
        => new($"An appointment with Id={id} already exists.");

    private class AppointmentEqualityComparer : IEqualityComparer<Appointment>
    {
        public bool Equals(Appointment? left, Appointment? right)
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
        public int GetHashCode([DisallowNull] Appointment appointment)
            => appointment.Id.GetHashCode();
    }
}