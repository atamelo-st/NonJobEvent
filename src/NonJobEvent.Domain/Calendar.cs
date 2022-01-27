using NonJobEvent.Common;
using NonJobEvent.Domain.DomainEvents;
using System.Diagnostics.CodeAnalysis;

namespace NonJobEvent.Domain;

public class Calendar
{
    private readonly Dictionary<Guid, OneOffEvent> oneOffEvents;
    private readonly Dictionary<Guid, RecurringEvent> recurringEvents;
    private readonly Dictionary<Guid, HashSet<DateOnly>> recurringOccurrencesTombstones;
    private readonly Dictionary<Guid, Dictionary<DateTime, RecurringEvent.Occurrence>> recurringOccurrencesOverrides;
    private readonly List<DomainEvent> domainEvents;

    public Guid Id { get; }

    public IReadOnlyList<DomainEvent> DomainEvents => this.domainEvents;

    public static Calendar Create(Guid id)
    {
        // TODO: publish 'CalendarCreated' event

        return new(id);
    }

    // TODO: add overrides
    // TODO: add tombstones
    public static Calendar Load(
            Guid id,
            IReadOnlyList<OneOffEvent> oneOffEvents,
            IReadOnlyList<RecurringEvent> recurringEvents)
    {
        ArgumentNullException.ThrowIfNull(oneOffEvents, nameof(oneOffEvents));
        ArgumentNullException.ThrowIfNull(recurringEvents, nameof(recurringEvents));

        return new(id, oneOffEvents, recurringEvents);
    }

    public IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> GetEvents(DateOnly from, DateOnly to)
    {
        foreach (OneOffEvent oneOff in this.oneOffEvents.Values)
        {
            yield return OneOf.Those(oneOff);
        }

        // TODO: add overrides, deletes
        foreach (RecurringEvent recurringEvent in this.recurringEvents.Values)
        {
            IEnumerable<RecurringEvent.Occurrence> occurrences =
                recurringEvent
                    .ExpandOccurrences(from, to)
                    //Skip deleted
                    .Where(occurrence => !IsOccurrenceDeleted(occurrence));

            foreach (RecurringEvent.Occurrence occurrence in occurrences)
            {
                yield return OneOf.Those(occurrence);
            }
        }

        bool IsOccurrenceDeleted(RecurringEvent.Occurrence occurrence) 
            => this.IsOccurrenceDeleted(occurrence.Parent.Id, occurrence.Date);
    }

    public bool AddOneOffEvent(OneOffEvent oneOffEvent)
    {
        bool added = AddEvent(this.oneOffEvents, oneOffEvent, throwOnDuplicates: false);

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
    {
        bool added  = AddEvent(this.recurringEvents, recurringEvent, throwOnDuplicates: false);

        if (added)
        {
            DomainEvent.RecurringEventAdded recuringEventAdded = new(recurringEvent, calendar: this);

            this.PublishDomainEvent(recuringEventAdded);
        }

        return added;
    }

    // TODO:flesh out ChangeRecurringEvent
    public bool ChangeRecurringEvent(Guid recurringEventId)
    {
        // TODO: how to handle deletes? e.g. simply clean-up those that don't fit the recurrence pattern?
        // TODO: how to handle overrides?

        throw null!;
    }

    public bool DeleteRecurringEvent(Guid recurringEventId)
    {
        bool removed = this.recurringEvents.Remove(recurringEventId);

        if (removed is false)
        {
            return false;
        }

        this.recurringOccurrencesTombstones.Remove(recurringEventId);

        this.recurringOccurrencesOverrides.Remove(recurringEventId);

        // TODO: publish the event

        return true;
    }

    // TODO: return smth more meaningful than just bool
    // to distinguesh between 'parent doens't exist' and 'occurrecnce already deleted'
    public bool DeleteRecurringEventOccurrence(Guid parentRecurringEventId, DateOnly date)
    {
        if (!this.RecurringEventOccurrenceExists(parentRecurringEventId, date))
        {
            return false;
        }

        if (!this.TryGetTombstones(parentRecurringEventId, out HashSet<DateOnly>? tombstones))
        {
            tombstones = new HashSet<DateOnly>();

            this.recurringOccurrencesTombstones.Add(parentRecurringEventId, tombstones);
        }

        bool deleted = tombstones.Add(date);

        if (deleted)
        {
            RecurringEvent parentRecurringEvent = this.recurringEvents[parentRecurringEventId];

            DomainEvent.RecurringEventOccurrenceDeleted recurringEventOccurrenceDeleted = new (
                parentRecurringEvent, 
                date);

            this.PublishDomainEvent(recurringEventOccurrenceDeleted);
        }

        return deleted;
    }

    public bool UnDeleteRecurringEventOccurrence(Guid parentRecurringEventId, DateOnly date)
    {
        if (!this.RecurringEventOccurrenceExists(parentRecurringEventId, date))
        {
            return false;
        }

        if (!this.TryGetTombstones(parentRecurringEventId, out var tombstones))
        {
            return false;
        }

        bool undeleted = tombstones.Remove(date);

        if (undeleted)
        {
            RecurringEvent parentRecurringEvent = this.recurringEvents[parentRecurringEventId];

            DomainEvent.RecurringEventOccurrenceUnDeleted recurringEventOccurrenceUnDeleted = new(
                parentRecurringEvent,
                date);

            this.PublishDomainEvent(recurringEventOccurrenceUnDeleted);
        }

        return undeleted;
    }

    public bool OverrideRecurringEventOccurrence()
    {
        throw null!;
    }

    public bool ResetRecurringEventOccurence()
    {
        throw null!;
    }

    public bool RecurringEventOccurrenceExists(Guid parentRecurringEventId, DateOnly date)
    {
        if (!this.TryGetRecurringEvent(parentRecurringEventId, out RecurringEvent? recurringEvent))
        {
            return false;
        }

        bool exists = recurringEvent.OccursOn(date);

        return exists;
    }

    public void AcknowledgeDomainEvents()
        => this.domainEvents.Clear();

    private Calendar(
        Guid id,
        IReadOnlyList<OneOffEvent>? oneOffEvents = null,
        IReadOnlyList<RecurringEvent>? recurringEvents = null)
    {
        this.Id = id;

        this.oneOffEvents = BuildEventIndex(oneOffEvents, AddEvent);
        this.recurringEvents = BuildEventIndex(recurringEvents, AddEvent);

        this.domainEvents = new List<DomainEvent>();
        this.recurringOccurrencesTombstones = new Dictionary<Guid, HashSet<DateOnly>>();
        this.recurringOccurrencesOverrides = new Dictionary<Guid, Dictionary<DateTime, RecurringEvent.Occurrence>>();

        static Dictionary<Guid, TEvent> BuildEventIndex<TEvent>(
            IReadOnlyList<TEvent>? events,
            Func<Dictionary<Guid, TEvent>, TEvent, bool, bool> add) where TEvent : Event
        {
            if (events is null)
            {
                return new();
            }

            Dictionary<Guid, TEvent> index = new(events.Count);

            foreach (TEvent appointment in events)
            {
                const bool throwOnDuplicates = true;

                add(index, appointment, throwOnDuplicates);
            }

            return index;
        }
    }

    private bool TryGetRecurringEvent(Guid recurringEventId, [NotNullWhen(true)] out RecurringEvent? recurringEvent)
        => this.recurringEvents.TryGetValue(recurringEventId, out recurringEvent);

    private bool TryGetTombstones(Guid recurringEventId, [NotNullWhen(true)] out HashSet<DateOnly>? tombtones)
        => this.recurringOccurrencesTombstones.TryGetValue(recurringEventId, out tombtones);

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

    private bool IsOccurrenceDeleted(Guid parentRecurringEventId, DateOnly date)
    {
        if (this.TryGetTombstones(parentRecurringEventId, out HashSet<DateOnly>? tombstones))
        {
            bool deleted = tombstones.Contains(date);

            return deleted;
        }

        return false;
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