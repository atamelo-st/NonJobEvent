using NonJobEvent.Common;
using NonJobEvent.Domain.DomainEvents;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NonJobEvent.Domain;

// DL: what's the benefit of having 3 instances of `public partial class Calendar`?
public partial class Calendar
{
    private readonly Dictionary<Guid, OneOffEvent> oneOffEvents;
    private readonly Dictionary<Guid, RecurringEvent> recurringEvents;
    private readonly Dictionary<Guid, HashSet<DateOnly>> recurringOccurrencesTombstones;
    private readonly Dictionary<Guid, Dictionary<DateOnly, RecurringEvent.Occurrence>> recurringOccurrencesOverrides;
    private readonly List<DomainEvent> domainEvents;

    public Guid Id { get; }

    public IReadOnlyList<DomainEvent> DomainEvents => this.domainEvents;

    public static Calendar Create(Guid id)
    {
        Calendar calendar = new(id);

        calendar.PublishDomainEvent(new DomainEvent.CalendarCreated(id));

        return calendar;
    }

    // TODO: this method doesn't seem to be really helpful. Will need to just use caledar's
    // .AddOneOffEvent & .AddRecurringEvent
    public static Calendar Load(
            Guid id,
            IReadOnlyList<OneOffEvent> oneOffEvents,
            IReadOnlyList<RecurringEvent> recurringEvents)
    {
        ArgumentNullException.ThrowIfNull(oneOffEvents, nameof(oneOffEvents));
        ArgumentNullException.ThrowIfNull(recurringEvents, nameof(recurringEvents));

        return new(id, oneOffEvents, recurringEvents);
    }

    // TODO: rename this API to smth like 'ExpandEvents'
    // TODO: create new GetEvents API that;d be streaming-friendly
    // i.e. return non-expanded RecurringEvents - so that they can be expanded 'externally' by the streaming app layer
    public IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> GetEvents(DateOnly dateFrom, DateOnly dateTo)
    {
        // TODO: add filtering logic to skip events that are ouside of [dateFrom; dateTo] range

        foreach (OneOffEvent oneOff in this.oneOffEvents.Values)
        {
            yield return OneOf.Those(oneOff);
        }

        foreach (RecurringEvent recurringEvent in this.recurringEvents.Values)
        {
            // TODO: think about how to preserve "deleted" and "overridden" flags
            IEnumerable<RecurringEvent.Occurrence> occurrences =
                recurringEvent
                    .ExpandOccurrences(dateFrom, dateTo)
                    // Skip deleted
                    .Where(OccurrenceIsNotDeleted)
                    // Apply override if exists
                    .Select(ResolveOccurrenceOverride);

            foreach (RecurringEvent.Occurrence occurrence in occurrences)
            {
                yield return OneOf.Those(occurrence);
            }
        }

        bool OccurrenceIsNotDeleted(RecurringEvent.Occurrence occurrence) 
            => !this.IsRecurringEventOccurrenceDeleted(occurrence.Parent.Id, occurrence.Date);

        RecurringEvent.Occurrence ResolveOccurrenceOverride(RecurringEvent.Occurrence occurrenceCandidate)
        {
            RecurringEvent.Occurrence resolved =
                OverrideExists(occurrenceCandidate, out RecurringEvent.Occurrence? @override) ?
                @override :
                occurrenceCandidate;

            return resolved;
        }

        bool OverrideExists(
            RecurringEvent.Occurrence targetOccurrence,
            [NotNullWhen(true)] out RecurringEvent.Occurrence? @override)
        {
            @override = null;

            if (!this.recurringOccurrencesOverrides.TryGetValue(targetOccurrence.Parent.Id, out var overrides))
            {
                return false;
            }

            if (!overrides.TryGetValue(targetOccurrence.Date, out @override))
            {
                return false;
            }

            return true;
        }
    }

    // TODO: pass data via parameters instead of OneOffEvent object
    public bool TryAddOneOffEvent(OneOffEvent oneOffEvent)
    {
        bool added = AddEvent(events: this.oneOffEvents, calendarId: this.Id, oneOffEvent, throwOnDuplicates: false);

        if (added)
        {
            this.PublishDomainEvent(new DomainEvent.OneOffEventAdded(oneOffEvent, calendar: this));
        }

        return added;
    }

    // TODO: pass data via parameters instead of OneOffEvent object
    public void AddOneOffEvent(OneOffEvent oneOffEvent)
    { 
        bool added = this.TryAddOneOffEvent(oneOffEvent);

        if (added is false)
        {
            throw DuplicateEvent(this.Id, oneOffEvent.Id);
        }
    }

    public bool TryDeleteOneOffEvent(Guid oneOffEventId)
    {
        bool removed = this.oneOffEvents.Remove(oneOffEventId);

        if (removed)
        {
            this.PublishDomainEvent(new DomainEvent.OneOffEventDeleted(oneOffEventId, CalendarId: this.Id));
        }

        return removed;
    }

    public void DeleteOneOffEvent(Guid oneOffEventId)
    {
        bool removed = this.TryDeleteOneOffEvent(oneOffEventId);

        if (removed is false)
        {
            throw EventNotFound(this.Id, oneOffEventId);
        }
    }

    public bool TryChangeOneOffEvent(
        Guid oneOffEventId,
        string? newEventTitle,
        string? newEventSummary,
        DateOnly? newEventDate,
        TimeFrame? newEventTimeFrame,
        int? newEventTimeseetCode)
    {
        if (!this.OneOffEventExists(oneOffEventId, out OneOffEvent? originalEvent))
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

        this.oneOffEvents[oneOffEventId] = changedEvent;

        this.PublishDomainEvent(
            new DomainEvent.OneOffEventChanged(
                ChangedEventId: oneOffEventId,
                CalendarId: this.Id,
                NewEventTitle: newEventTitle,
                NewEventSummary: newEventSummary,
                NewEventDate: newEventDate,
                NewEventTimeFrame: newEventTimeFrame,
                NewEventTimeseetCode: newEventTimeseetCode)
        );

        return true;
    }

    public void ChangeOneOffEvent(
        Guid oneOffEventId,
        string? newEventTitle,
        string? newEventSummary,
        DateOnly? newEventDate,
        TimeFrame? newEventTimeFrame,
        int? newEventTimeseetCode)
    {
        bool changed = this.TryChangeOneOffEvent(
            oneOffEventId,
            newEventTitle,
            newEventSummary,
            newEventDate,
            newEventTimeFrame,
            newEventTimeseetCode);

        if (!changed)
        {
            throw EventNotFound(this.Id, oneOffEventId);
        }
    }

    // TODO: pass data via parameters instead of RecurringEvent object
    public bool TryAddRecurringEvent(RecurringEvent recurringEvent)
    {
        bool added  = AddEvent(events: this.recurringEvents, calendarId: this.Id, recurringEvent, throwOnDuplicates: false);

        if (added)
        {
            this.PublishDomainEvent(new DomainEvent.RecurringEventAdded(recurringEvent, calendar: this));
        }

        return added;
    }

    // TODO: pass data via parameters instead of RecurringEvent object
    public void AddRecurringEvent(RecurringEvent recurringEvent)
    {
        bool added = this.TryAddRecurringEvent(recurringEvent);

        if (added is false)
        {
            throw EventNotFound(this.Id, recurringEvent.Id);
        }
    }

    public bool ChangeRecurringEvent(
        Guid recurringEventId,
        string? newEventTitle,
        string? newEventSummary,
        DateOnly? newEventStartDate,
        TimeFrame? newEventTimeFrame,
        int? newEventTimeseetCode,
        RecurrencePattern? newRecurrencePattern)
    {
        if (!this.RecurringEventExists(recurringEventId, out RecurringEvent? originalEvent))
        {
            return false;
        }

        RecurringEvent changedEvent = new(
            originalEvent.Id,
            newEventTitle ?? originalEvent.Title,
            newEventSummary ?? originalEvent.Summary,
            newEventStartDate ?? originalEvent.StartDate,
            newEventTimeFrame ?? originalEvent.TimeFrame,
            newEventTimeseetCode ?? originalEvent.TimeseetCode,
            newRecurrencePattern ?? originalEvent.Pattern
        );

        this.recurringEvents[recurringEventId] = changedEvent;

        CleanupTombstonesAndOverrides(changedEvent);

        this.PublishDomainEvent(
            new DomainEvent.RecurringEventChanged(
                ChangedEventId: recurringEventId,
                CalendarId: this.Id,
                NewEventTitle: newEventTitle,
                NewEventSummary: newEventSummary,
                NewEventStartDate: newEventStartDate,
                NewEventTimeFrame: newEventTimeFrame,
                NewEventTimeseetCode: newEventTimeseetCode,
                NewRecurrencePattern: newRecurrencePattern)
        );

        return true;

        void CleanupTombstonesAndOverrides(RecurringEvent changedEvent)
        {
            bool startDateChanged = newEventStartDate is not null && newEventStartDate.Value != originalEvent.StartDate;
            bool recurrencePatternChanged = newRecurrencePattern is not null && newRecurrencePattern != originalEvent.Pattern;
            bool needToCleanupTombstonesAndOverrides = startDateChanged || recurrencePatternChanged;

            if (needToCleanupTombstonesAndOverrides)
            {
                CleanupTombstones(changedEvent);
                CleanupOverrides(changedEvent);
            }
        }

        void CleanupTombstones(RecurringEvent changedEvent)
        {
            if (!this.TombstonesExist(changedEvent.Id, out HashSet<DateOnly>? tombstones))
            {
                return;
            }

            // NOTE: clean up those that don't fit the recurrence pattern
            List<DateOnly> tombstonesToCleanup = new();

            foreach (DateOnly tombstonedDate in tombstones)
            {
                // NOTE: the expectations is that range [tombstonedDate; tombstonedDate]
                // is enough for the underlying lib to be able to correctly expand the occurrence.
                bool tombstoneMissesThePattern = !changedEvent.ExpandOccurrences(tombstonedDate, tombstonedDate).Any();

                if (tombstoneMissesThePattern)
                {
                    tombstonesToCleanup.Add(tombstonedDate);
                }
            }

            foreach (DateOnly tobmstoneToCleanup in tombstonesToCleanup)
            {
                // NOTE: we go through the API so that all the corresponding events
                // are published and so all the state changes are recorded
                bool cleanedup = this.TryUnDeleteRecurringEventOccurrence(changedEvent.Id, tobmstoneToCleanup);

                Debug.Assert(cleanedup);
            }
        }

        void CleanupOverrides(RecurringEvent changedEvent)
        {
            if (!this.RecurringEventOverridesExist(changedEvent.Id, out var overrides))
            {
                return;
            }

            // NOTE: clean up those that don't fit the recurrence pattern?
            List<DateOnly> overridesToCleanup = new();

            foreach (RecurringEvent.Occurrence @override in overrides.Values)
            {
                bool overrideMissesThePattern = !changedEvent.ExpandOccurrences(@override.Date, @override.Date).Any();

                if (overrideMissesThePattern)
                {
                    overridesToCleanup.Add(@override.Date);
                }
            }

            foreach (DateOnly overrideToCleanup in overridesToCleanup)
            {
                bool cleanedup = this.TryRevertRecurringEventOccurenceOverride(changedEvent.Id, overrideToCleanup);

                Debug.Assert(cleanedup);
            }
        }
    }

    public bool TryDeleteRecurringEvent(Guid recurringEventId)
    {
        if (!this.recurringEvents.Remove(recurringEventId, out RecurringEvent? deletedRecurringEvent))
        {
            return false;
        }

        this.recurringOccurrencesTombstones.Remove(recurringEventId);
        this.recurringOccurrencesOverrides.Remove(recurringEventId);

        this.PublishDomainEvent(new DomainEvent.RecurringEventDeleted(deletedRecurringEvent, calendar: this));

        return true;
    }

    public void DeleteRecurringEvent(Guid recurringEventId)
    {
        bool deleted = this.TryDeleteRecurringEvent(recurringEventId);

        if (deleted is false)
        {
            throw EventNotFound(this.Id, recurringEventId);
        }
    }

    // TODO: return smth more meaningful than just bool ?
    // to distinguesh between 'parent doens't exist' and 'occurrecnce already deleted'
    // or rely on exceptions and just expect that the called does appropriate checks before calling the API?
    // or just compose the error message with "this OR that happened" :)
    public bool TryDeleteRecurringEventOccurrence(Guid parentRecurringEventId, DateOnly date)
    {
        if (!this.RecurringEventOccurrenceExists(parentRecurringEventId, date))
        {
            return false;
        }

        // NOTE: .Delete doesn't revert a potentially existing .Override.
        // That way, after .UnDelete, the override remains
        if (!this.TombstonesExist(parentRecurringEventId, out HashSet<DateOnly>? tombstones))
        {
            tombstones = new HashSet<DateOnly>();

            this.recurringOccurrencesTombstones.Add(parentRecurringEventId, tombstones);
        }
        // TODO: implement optimistic concurrency check for .DeleteOccurence and .OverrideOccurence
        // when saving the state to the DB - 'lock' on the parent event's version to avoid race condition
        bool deleted = tombstones.Add(date);

        if (deleted)
        {
            RecurringEvent parentRecurringEvent = this.recurringEvents[parentRecurringEventId];

            DomainEvent.RecurringEventOccurrenceDeleted recurringEventOccurrenceDeleted = new (
                parentRecurringEvent, 
                date,
                Calendar: this);

            this.PublishDomainEvent(recurringEventOccurrenceDeleted);
        }

        return deleted;
    }

    public bool TryUnDeleteRecurringEventOccurrence(Guid parentRecurringEventId, DateOnly date)
    {
        if (!this.TombstonesExist(parentRecurringEventId, out var tombstones))
        {
            return false;
        }

        bool undeleted = tombstones.Remove(date);

        if (undeleted)
        {
            RecurringEvent parentRecurringEvent = this.recurringEvents[parentRecurringEventId];

            DomainEvent.RecurringEventOccurrenceUnDeleted recurringEventOccurrenceUnDeleted = new(
                parentRecurringEvent,
                date,
                Calendar: this);

            this.PublishDomainEvent(recurringEventOccurrenceUnDeleted);
        }

        return undeleted;
    }

    public bool TryOverrideRecurringEventOccurrence(
        Guid parentRecurringEventId,
        DateOnly dateToOverride,
        RecurringEvent.Occurrence overridingOccurence)
    {
        if (!this.RecurringEventOccurrenceExists(parentRecurringEventId, dateToOverride))
        {
            return false;
        }

        // TODO: implement optimistic concurrency check for .DeleteOccurence and .OverrideOccurence
        // when saving the state to the DB - 'lock' on the parent event's version to avoid race condition
        if (this.IsRecurringEventOccurrenceDeleted(parentRecurringEventId, dateToOverride))
        {
            return false;
        }

        if (!this.recurringOccurrencesOverrides.TryGetValue(parentRecurringEventId, out var overrides))
        {
            overrides = new();

            this.recurringOccurrencesOverrides.Add(parentRecurringEventId, overrides);
        }

        overrides[dateToOverride] = overridingOccurence;

        this.PublishDomainEvent(
            new DomainEvent.RecurringEventOccurrenceOverridden(
                parentRecurringEventId,
                dateToOverride,
                calendar: this));

        return true;
    }

    public bool TryRevertRecurringEventOccurenceOverride(Guid parentRecurringEventId, DateOnly dateOfOverride)
    {
        // TODO: any race condition check need to be implemented on persisting?
        if (this.IsRecurringEventOccurrenceDeleted(parentRecurringEventId, dateOfOverride))
        {
            // to revert a deleted override it first needs to be un-deleted
            return false;
        }

        if (!this.RecurringEventOverridesExist(parentRecurringEventId, out var overrides))
        {
            return false;
        }

        bool reverted = overrides.Remove(dateOfOverride);

        if (reverted)
        {
            this.PublishDomainEvent(
                new DomainEvent.RecurringEventOccurrenceOverrideReverted(
                    parentRecurringEventId,
                    dateOfOverride,
                    calendar: this));
        }

        return reverted;
    }

    public bool RecurringEventOccurrenceExists(Guid parentRecurringEventId, DateOnly date)
    {
        if (!this.RecurringEventExists(parentRecurringEventId, out RecurringEvent? recurringEvent))
        {
            return false;
        }

        bool exists = recurringEvent.OccursOn(date);

        return exists;
    }

    public bool OneOffEventExists(Guid oneOffEvenId) => this.oneOffEvents.TryGetValue(oneOffEvenId, out _);

    public void AcknowledgeDomainEvents()
        => this.domainEvents.Clear();

    private Calendar(
        Guid id,
        IReadOnlyList<OneOffEvent>? oneOffEvents = null,
        IReadOnlyList<RecurringEvent>? recurringEvents = null)
    {
        this.Id = id;

        this.oneOffEvents = BuildEventIndex(id, oneOffEvents, AddEvent);
        this.recurringEvents = BuildEventIndex(id, recurringEvents, AddEvent);

        this.domainEvents = new List<DomainEvent>();
        this.recurringOccurrencesTombstones = new Dictionary<Guid, HashSet<DateOnly>>();
        this.recurringOccurrencesOverrides = new Dictionary<Guid, Dictionary<DateOnly, RecurringEvent.Occurrence>>();

        static Dictionary<Guid, TEvent> BuildEventIndex<TEvent>(
            Guid calendarId,
            IReadOnlyList<TEvent>? events,
            Func<Dictionary<Guid, TEvent>, Guid, TEvent, bool, bool> add) where TEvent : Event
        {
            if (events is null)
            {
                return new();
            }

            Dictionary<Guid, TEvent> index = new(events.Count);

            foreach (TEvent @event in events)
            {
                const bool throwOnDuplicates = true;

                add(index, calendarId, @event, throwOnDuplicates);
            }

            return index;
        }
    }

    private bool OneOffEventExists(Guid oneOffEvenId, [NotNullWhen(true)] out OneOffEvent? oneOffEvent)
        => this.oneOffEvents.TryGetValue(oneOffEvenId, out oneOffEvent);

    private bool RecurringEventExists(Guid recurringEventId, [NotNullWhen(true)] out RecurringEvent? recurringEvent)
        => this.recurringEvents.TryGetValue(recurringEventId, out recurringEvent);

    private bool TombstonesExist(Guid recurringEventId, [NotNullWhen(true)] out HashSet<DateOnly>? tombtones)
        => this.recurringOccurrencesTombstones.TryGetValue(recurringEventId, out tombtones);

    private bool RecurringEventOverridesExist(
        Guid recurringEventId,
        [NotNullWhen(true)] out Dictionary<DateOnly, RecurringEvent.Occurrence>? overrides)
        => this.recurringOccurrencesOverrides.TryGetValue(recurringEventId, out overrides);

    private static bool AddEvent<TEvent>(
        Dictionary<Guid, TEvent> events,
        Guid calendarId,
        TEvent @event,
        bool throwOnDuplicates) where TEvent : Event
    {
        bool added = events.TryAdd(@event.Id, @event);

        if (added is false && throwOnDuplicates)
        {
            throw DuplicateEvent(calendarId, @event.Id);
        }

        return added;
    }

    private bool IsRecurringEventOccurrenceDeleted(Guid parentRecurringEventId, DateOnly date)
    {
        if (this.TombstonesExist(parentRecurringEventId, out HashSet<DateOnly>? tombstones))
        {
            bool deleted = tombstones.Contains(date);

            return deleted;
        }

        return false;
    }

    private void PublishDomainEvent(DomainEvent domainEvent)
        => this.domainEvents.Add(domainEvent);

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

public partial class Calendar
{
    public abstract class Exception : System.Exception
    {
        public Exception(string message, System.Exception? innerException = null) : base(message, innerException) { }

        public class DuplicateEvent : Calendar.Exception
        {
            public DuplicateEvent(string message) : base(message) { }
        }

        public class EventNotFound : Calendar.Exception
        {
            public EventNotFound(string message) : base(message) { }
        }
    }

    public static Calendar.Exception.DuplicateEvent DuplicateEvent(Guid calendarId, Guid eventId)
        => new($"Event Id={eventId} already exists in calendar Id={calendarId}");

    public static Calendar.Exception.DuplicateEvent EventNotFound(Guid calendarId, Guid eventId)
        => new($"Event Id={eventId} not found in calendar Id={calendarId}");
}

// TODO: move all the private methods here?
public partial class Calendar
{

}