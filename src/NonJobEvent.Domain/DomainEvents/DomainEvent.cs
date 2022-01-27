using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NonJobEvent.Domain.DomainEvents;

public abstract partial record DomainEvent
{
    public Guid Id { get; }

    private DomainEvent()
    {
        this.Id = Guid.NewGuid();
    }

    // TODO: think if it's really needed
    public sealed record CalendarCreated(Guid CalendarId) : DomainEvent;

    // TODO: consider flattening event payload instead of directly using domain classes
    public sealed record OneOffEventAdded(OneOffEvent AddedEvent, Calendar calendar) : DomainEvent;

    public sealed record OneOffEventDeleted(Guid DeletedEventId, Guid CalendarId) : DomainEvent;

    // NOTE: 'null' communicates 'not changed'. In case 'null' is a ligit value, 
    // we'll need to communicate 'not changed' with an extra field (flags?)
    public sealed record OneOffEventChanged(
        Guid ChangedEventId, 
        Guid CalendarId,
        string? NewEventTitle,
        string? NewEventSummary,
        DateOnly? NewEventDate,
        TimeFrame? NewEventTimeFrame,
        int? NewEventTimeseetCode
    ) : DomainEvent;

    // TODO: consider flattening event payload instead of directly using domain classes
    public sealed record RecurringEventAdded(RecurringEvent AddedEvent, Calendar calendar) : DomainEvent;

    public sealed record RecurringEventDeleted(RecurringEvent DeletedEvent, Calendar calendar) : DomainEvent;

    // NOTE: 'null' communicates 'not changed'. In case 'null' is a ligit value, 
    // we'll need to communicate 'not changed' with an extra field (flags?)
    public sealed record RecurringEventChanged(
        Guid ChangedEventId,
        Guid CalendarId,
        string? NewEventTitle,
        string? NewEventSummary,
        DateOnly? NewEventStartDate,
        TimeFrame? NewEventTimeFrame,
        int? NewEventTimeseetCode,
        RecurrencePattern? NewRecurrencePattern
    ) : DomainEvent;

    public sealed record RecurringEventOccurrenceDeleted(RecurringEvent ParentRecurringEvent, DateOnly Date, Calendar Calendar)
        : DomainEvent;

    public sealed record RecurringEventOccurrenceUnDeleted(RecurringEvent ParentRecurringEvent, DateOnly Date, Calendar Calendar)
        : DomainEvent;

    public sealed record RecurringEventOccurrenceOverridden(Guid ParentRecurringEventId, DateOnly Date, Calendar calendar)
        : DomainEvent;

    public sealed record RecurringEventOccurrenceOverrideReverted(Guid ParentRecurringEventId, DateOnly Date, Calendar calendar)
        : DomainEvent;
}
