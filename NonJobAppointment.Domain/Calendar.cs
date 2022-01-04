using NonJobAppointment.Common;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NonJobAppointment.Domain;

public class Calendar
{
    private static readonly RecurringAppointmentEqualityComparer recurringAppointmentEqualityComparer = new();

    private readonly IReadOnlyList<OneOffAppointment> oneOffappointments;
    private readonly HashSet<RecurringAppointment> recurringAppointments;

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
        this.recurringAppointments = BuildRecurringAppointmentIndex(recurringAppointments);

        static HashSet<RecurringAppointment> BuildRecurringAppointmentIndex(IReadOnlyList<RecurringAppointment> recurringAppointments)
        {
            HashSet<RecurringAppointment> index = new(recurringAppointments.Count, recurringAppointmentEqualityComparer);

            foreach (RecurringAppointment recurringAppointment in recurringAppointments)
            {
                if (index.Add(recurringAppointment) is not true)
                {
                    throw RecurringAppointmentAlreadyExists(id: recurringAppointment.Id);
                }
            }

            return index;
        }
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

    private static ArgumentException RecurringAppointmentAlreadyExists(Guid id)
        => new($"Recurring appointment with Id={id} already exists.");

    private class RecurringAppointmentEqualityComparer : IEqualityComparer<RecurringAppointment>
    {
        public bool Equals(RecurringAppointment? left, RecurringAppointment? right)
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

        public int GetHashCode([DisallowNull] RecurringAppointment recurringAppointment)
            => EqualityComparer<Guid>.Default.GetHashCode(recurringAppointment.Id);
    }
}