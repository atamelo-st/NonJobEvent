using NonJobEvent.Common;
using NonJobEvent.Domain.DomainEvents;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NonJobEvent.Domain;

public class Calendar
{
    private readonly Dictionary<Guid, OneOffEvent> oneOffEvents;
    private readonly Dictionary<Guid, RecurringEvent> recurringEvents;
    private readonly List<DomainEvent> domainEvents;

    public Guid Id { get; }
    public DateOnly UtcDateFrom { get; }
    public DateOnly UtcDateTo { get; }

    public IReadOnlyList<DomainEvent> DomainEvents => this.domainEvents;

    public Calendar(
        Guid id,
        DateOnly utcDateFrom,
        DateOnly utcDateTo,
        IReadOnlyList<OneOffEvent> oneOffEvents,
        IReadOnlyList<RecurringEvent> recurringEvents)
    {
        ArgumentNullException.ThrowIfNull(oneOffEvents, nameof(oneOffEvents));
        ArgumentNullException.ThrowIfNull(recurringEvents, nameof(recurringEvents));

        this.Id = id;
        this.UtcDateFrom = utcDateFrom;
        this.UtcDateTo = utcDateTo;
        
        this.oneOffEvents = BuildEventIndex(oneOffEvents, AddEvent);
        this.recurringEvents = BuildEventIndex(recurringEvents, AddEvent);

        this.domainEvents = new List<DomainEvent>();

        static Dictionary<Guid, TEvent> BuildEventIndex<TEvent>(
            IReadOnlyList<TEvent> events,
            Func<Dictionary<Guid, TEvent>, TEvent, bool, bool> add) where TEvent : Event
        {
            Dictionary<Guid, TEvent> index = new(events.Count);

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
            DomainEvent.OneOffEventAdded oneOffEventAdded = new(oneOffEvent, calendar: this);

            this.PublishDomainEvent(oneOffEventAdded);
        }

        return added;
    }

    public bool DeleteOneOffEvent(Guid oneOffEventId)
    {
        bool removed = this.oneOffEvents.Remove(oneOffEventId);

        if (removed)
        {
            DomainEvent.OneOffEventDeleted oneOffEventDeleted = new(oneOffEventId, CalendarId: this.Id);

            this.PublishDomainEvent(oneOffEventDeleted);
        }

        return removed;
    }

    public bool ChangeOneOffEvent(
        Guid oneOffEventId,
        string? newEventTitle,
        string? newEventSummary,
        DateOnly? newEventDate,
        TimeFrame? newEventTimeFrame,
        int? newEventTimeseetCode)
    {
        if (this.oneOffEvents.TryGetValue(oneOffEventId, out OneOffEvent? originalEvent) is false)
        {
            return false;
        }

        OneOffEvent changedEvent = new(
            originalEvent.Id,
            newEventTitle ?? originalEvent.Title,
            newEventSummary ?? originalEvent.Summary,
            newEventDate ?? originalEvent.Date,
            newEventTimeFrame ?? originalEvent.TimeFrame,
            newEventTimeseetCode ?? originalEvent.TimeseetCode
        );

        this.oneOffEvents.Remove(oneOffEventId);
        this.oneOffEvents.Add(oneOffEventId, changedEvent);

        DomainEvent.OneOffEventChanged oneOffEventChanged = new(
            ChangedEventId: oneOffEventId,
            CalendarId: this.Id,
            NewEventTitle: newEventTitle,
            NewEventSummary: newEventSummary,
            NewEventDate: newEventDate,
            NewEventTimeFrame: newEventTimeFrame,
            NewEventTimeseetCode: newEventTimeseetCode
        );

        this.PublishDomainEvent(oneOffEventChanged);

        return true;
    }

    public bool AddRecurringEvent(RecurringEvent recurringEvent)
        => AddEvent(recurringEvents, recurringEvent, throwOnDuplicates: false);

    public void AcknowledgeDomainEvents()
        => this.domainEvents.Clear();

    private static bool AddEvent<TEvent>(
        Dictionary<Guid, TEvent> events,
        TEvent @event,
        bool throwOnDuplicates) where TEvent : Event
    {
        bool added = events.TryAdd(@event.Id, @event);

        if (added is not true && throwOnDuplicates)
        {
            throw EventAlreadyExists(@event.Id);
        }

        return added;
    }

    // TODO: we need to be careful with 'from' and 'to' as we may add new events
    // to the calendar (after it has been read from the repository) with dates outside
    // that [from, to] range
    private IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> GetEvents(DateOnly from, DateOnly to)
    {
        foreach (OneOffEvent oneOff in this.oneOffEvents.Values)
        {
            yield return OneOf.Those(oneOff);
        }

        // TODO: add overrides, deletes
        foreach (RecurringEvent recurring in this.recurringEvents.Values)
        {
            IEnumerable<RecurringEvent.Occurrence> occurrences = recurring.ExpandOccurrences(from, to);

            foreach (RecurringEvent.Occurrence occurrence in occurrences)
            {
                yield return OneOf.Those(occurrence);
            }
        }
    }

    private void PublishDomainEvent(DomainEvent domainEvent)
        => this.domainEvents.Add(domainEvent);

    private static ArgumentException EventAlreadyExists(Guid id)
        => new($"Event with Id={id} already exists.");

    private class EventEqualityComparer : IEqualityComparer<Event>
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