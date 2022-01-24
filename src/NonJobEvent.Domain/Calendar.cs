using NonJobEvent.Common;
using NonJobEvent.Domain.DomainEvents;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NonJobEvent.Domain;

public class Calendar
{
    private static readonly EventEqualityComparer eventEqualityComparer = new();

    private readonly HashSet<OneOffEvent> oneOffEvents;
    private readonly HashSet<RecurringEvent> recurringEvents;

    private readonly List<DomainEvent> domainEvents;

    public IReadOnlyList<DomainEvent> DomainEvents => this.domainEvents;

    public Guid Id { get; }
    public DateOnly UtcDateFrom { get; }
    public DateOnly UtcDateTo { get; }



    public Calendar(
        Guid id,
        DateOnly utcDateFrom,
        DateOnly utcDateTo,
        IReadOnlyList<OneOffEvent> oneOffEvents,
        IReadOnlyList<RecurringEvent> recurringEvents
    )
    {
        ArgumentNullException.ThrowIfNull(oneOffEvents, nameof(oneOffEvents));
        ArgumentNullException.ThrowIfNull(recurringEvents, nameof(recurringEvents));

        this.Id = id;
        this.UtcDateFrom = utcDateFrom;
        this.UtcDateTo = utcDateTo;
        
        this.oneOffEvents = BuildEventIndex(oneOffEvents, AddEvent);
        this.recurringEvents = BuildEventIndex(recurringEvents, AddEvent);

        this.domainEvents = new List<DomainEvent>();

        static HashSet<TEvent> BuildEventIndex<TEvent>(
            IReadOnlyList<TEvent> events,
            Func<HashSet<TEvent>, TEvent, bool, bool> add) where TEvent : Event
        {
            HashSet<TEvent> index = new(events.Count, eventEqualityComparer);

            foreach (TEvent appointment in events)
            {
                const bool throwOnDuplicates = true;

                add(index, appointment, throwOnDuplicates);
            }

            return index;
        }
    }

    public IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> GetEvents()
        => GetEvents(UtcDateFrom, UtcDateTo);

    public bool AddOneOffEvent(OneOffEvent oneOffEvent)
    {
        bool added = AddEvent(oneOffEvents, oneOffEvent, throwOnDuplicates: false);

        if (added)
        {
            DomainEvent.OneOffEventAdded oneOffEventAdded = new(oneOffEvent);

            this.PublishDomainEvent(oneOffEventAdded);
        }

        return added;
    }

    public bool AddRecurringEvent(RecurringEvent recurringEvent)
        => AddEvent(recurringEvents, recurringEvent, throwOnDuplicates: false);

    public void AcknowledgeDomainEvents() => this.domainEvents.Clear();

    private static bool AddEvent<TEvent>(
        HashSet<TEvent> events,
        TEvent @event,
        bool throwOnDuplicates) where TEvent : Event
    {
        bool added = events.Add(@event);

        if (added is not true && throwOnDuplicates)
        {
            throw EventAlreadyExists(@event.Id);
        }

        return added;
    }

    private IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> GetEvents(DateOnly from, DateOnly to)
    {
        foreach (OneOffEvent oneOff in oneOffEvents)
        {
            yield return OneOf.Those(oneOff);
        }

        // TODO: add overrides, deletes
        foreach (RecurringEvent recurring in recurringEvents)
        {
            IEnumerable<RecurringEvent.Occurrence> occurrences = recurring.ExpandOccurrences(from, to);

            foreach (RecurringEvent.Occurrence occurrence in occurrences)
            {
                yield return OneOf.Those(occurrence);
            }
        }
    }

    private void PublishDomainEvent(DomainEvent domainEvent) => this.domainEvents.Add(domainEvent);

    private static ArgumentException EventAlreadyExists(Guid id)
        => new($"Event with Id={id} already exists.");

    private class EventEqualityComparer : IEqualityComparer<Event>
    {
        public bool Equals(Event? left, Event? right)
        {
            if (ReferenceEquals(left, right))
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