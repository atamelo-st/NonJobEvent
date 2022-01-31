using NonJobEvent.Application.Api;
using NonJobEvent.Application.Api.DataAccess;
using NonJobEvent.Common;
using NonJobEvent.Domain;
using NonJobEvent.Domain.DomainEvents;

namespace NonJobEvent.Application.Handlers;

public class CalendarCommandQueryHandler :
    IQueryHandler<Queries.GetCalendarEvents, IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>>>,
    ICommandHandler<Commands.AddOneOffEvent, bool>,
    ICommandHandler<Commands.DeleteOneOffEvent, bool>,
    ICommandHandler<Commands.ChangeOneOffEvent, bool>
{
    private readonly ICalendarRepository repo;

    public CalendarCommandQueryHandler(ICalendarRepository repo)
    {
        this.repo = repo;
    }

    public async Task<IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>>> HandleAsync(Queries.GetCalendarEvents query)
    {
        Calendar calendar = await repo.GetCalendarAsync(query.CalendarId, query.DateFrom, query.DateTo);

        IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> events = calendar.GetEvents(query.DateFrom, query.DateTo);

        return events;
    }

    public async Task<bool> HandleAsync(Commands.AddOneOffEvent command)
    {
        Calendar calendar = await this.repo.GetCalendarAsync(command.CalendarId, command.EventDate, command.EventDate);

        OneOffEvent oneOffEvent = new(
            command.EventId,
            command.EventTitle,
            command.EventSummary,
            command.EventDate,
            command.EventTimeFrame,
            command.EventTimeseetCode
        );

        bool added = calendar.AddOneOffEvent(oneOffEvent);

        if (added is false)
        {
            return false;
        }

        int recordsAffected = await this.repo.SaveUpdatesAsync(calendar.DomainEvents);

        // TODO: dispatch domain events

        return recordsAffected > 0;
    }

    public async Task<bool> HandleAsync(Commands.DeleteOneOffEvent command)
    {
        // NOTE: we don't seem to have have any business logic to execute upon deleting
        // a one-off event. So we don't go through the domain model and 'publish'
        // the domain event directly from the handler
        // NOTE: dunno if this 'shortcut' is a worthwhile optimization, though..
        int recordsAffected = await this.repo.SaveUpdatesAsync(
            new List<DomainEvent> 
            { 
                new DomainEvent.OneOffEventDeleted(command.CalendarId, command.EventId)
            }
        );

        bool deleted = recordsAffected > 0;

        if (deleted)
        {
            // TODO: dispatch domain events
        }

        return deleted;
    }

    public async Task<bool> HandleAsync(Commands.ChangeOneOffEvent command)
    {
        // NOTE: we don't seem to have have any business logic to execute upon changing
        // a one-off event. So we don't go through the domain model and 'publish'
        // the domain event directly from the handler
        // NOTE: dunno if this 'shortcut' is a worthwhile optimization, though..
        int recordsAffected = await this.repo.SaveUpdatesAsync(
            new List<DomainEvent>
            {
                new DomainEvent.OneOffEventChanged(
                    command.EventId,
                    command.CalendarId,
                    command.NewEventTitle,
                    command.NewEventSummary,
                    command.NewEventDate,
                    command.NewEventTimeFrame,
                    command.NewEventTimeseetCode)
            }
        );

        bool changed = recordsAffected > 0;

        if (changed)
        {
            // TODO: dispatch domain events
        }

        return changed;
    }
}
