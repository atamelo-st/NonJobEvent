using NonJobEvent.Common;
using NonJobEvent.Domain.DomainEvents;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NonJobEvent.Domain;

public class Calendar
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

    public IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> GetEvents(DateOnly dateFrom, DateOnly dateTo)
    {
        foreach (OneOffEvent oneOff in this.oneOffEvents.Values)
        {
            yield return OneOf.Those(oneOff);
        }

        foreach (RecurringEvent recurringEvent in this.recurringEvents.Values)
        {
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

    public bool AddOneOffEvent(OneOffEvent oneOffEvent)
    {
        bool added = AddEvent(this.oneOffEvents, oneOffEvent, throwOnDuplicates: false);

        if (added)
        {
            this.PublishDomainEvent(new DomainEvent.OneOffEventAdded(oneOffEvent, calendar: this));
        }

        return added;
    }

    public bool DeleteOneOffEvent(Guid oneOffEventId)
    {
        bool removed = this.oneOffEvents.Remove(oneOffEventId);

        if (removed)
        {
            this.PublishDomainEvent(new DomainEvent.OneOffEventDeleted(oneOffEventId, CalendarId: this.Id));
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

    public bool AddRecurringEvent(RecurringEvent recurringEvent)
    {
        bool added  = AddEvent(this.recurringEvents, recurringEvent, throwOnDuplicates: false);

        if (added)
        {
            this.PublishDomainEvent(new DomainEvent.RecurringEventAdded(recurringEvent, calendar: this));
        }

        return added;
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
                bool cleanedup = this.UnDeleteRecurringEventOccurrence(changedEvent.Id, tobmstoneToCleanup);

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
                bool cleanedup = this.RevertRecurringEventOccurenceOverride(changedEvent.Id, overrideToCleanup);

                Debug.Assert(cleanedup);
            }
        }
    }

    public bool DeleteRecurringEvent(Guid recurringEventId)
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

    // TODO: return smth more meaningful than just bool ?
    // to distinguesh between 'parent doens't exist' and 'occurrecnce already deleted'
    // or rely on exceptions and just expect that the called does appropriate checks before calling the API?
    public bool DeleteRecurringEventOccurrence(Guid parentRecurringEventId, DateOnly date)
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

    public bool UnDeleteRecurringEventOccurrence(Guid parentRecurringEventId, DateOnly date)
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

    public bool OverrideRecurringEventOccurrence(
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

    public bool RevertRecurringEventOccurenceOverride(Guid parentRecurringEventId, DateOnly dateOfOverride)
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
        this.recurringOccurrencesOverrides = new Dictionary<Guid, Dictionary<DateOnly, RecurringEvent.Occurrence>>();

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