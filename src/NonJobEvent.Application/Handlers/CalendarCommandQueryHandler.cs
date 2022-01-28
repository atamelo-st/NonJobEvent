using NonJobEvent.Application.Api;
using NonJobEvent.Application.Api.DataAccess;
using NonJobEvent.Common;
using NonJobEvent.Domain;
using NonJobEvent.Domain.DomainEvents;

namespace NonJobEvent.Application.Handlers;

public class CalendarCommandQueryHandler :
    IQueryHandler<Queries.GetCalendarEvents, Result.OfQuery<IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>>>>,
    ICommandHandler<Commands.AddOneOffEvent, Result.OfCommand>,
    ICommandHandler<Commands.DeleteOneOffEvent, Result.OfCommand>,
    ICommandHandler<Commands.ChangeOneOffEvent, Result.OfCommand>
{
    private readonly ICalendarRepository repo;

    public CalendarCommandQueryHandler(ICalendarRepository repo)
    {
        this.repo = repo;
    }

    public async Task<Result.OfQuery<IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>>>> HandleAsync(Queries.GetCalendarEvents query)
    {
        Calendar? calendar = await repo.GetCalendarAsync(query.CalendarId, query.DateFrom, query.DateTo);

        if (calendar is null)
        {
            // TODO: think is the type can be avoided with implicit conversion
            return Result.OfQuery<IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>>>.OfFailure.NotFound();
        }

        IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> events = calendar.GetEvents(query.DateFrom, query.DateTo);

        return Result.OfQuery.OfSuccess(events);
    }

    public async Task<Result.OfCommand> HandleAsync(Commands.AddOneOffEvent command)
    {
        Calendar? calendar = await this.repo.GetCalendarAsync(command.CalendarId, command.EventDate, command.EventDate);

        if (calendar is null)
        {
            return Result.OfCommand.OfFailure.NotFound($"Calendar ID={command.CalendarId} not found.");
        }

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
            return Result.OfCommand.OfFailure.AlreadyExists();
        }

        Result.OfCommand result = await this.repo.SaveUpdatesAsync(calendar.DomainEvents);

        // TODO: dispatch domain events

        return result;
    }

    public async Task<Result.OfCommand> HandleAsync(Commands.DeleteOneOffEvent command)
    {
        // NOTE: we don't seem to have have any business logic to execute upon deleting
        // a one-off event. So we don't go through the domain model and 'publish'
        // the domain event directly from the handler
        // NOTE: dunno if this 'shortcut' is a worthwhile optimization, though..
        Result.OfCommand deleted = await this.repo.SaveUpdatesAsync(
            new List<DomainEvent> 
            { 
                new DomainEvent.OneOffEventDeleted(command.CalendarId, command.EventId)
            }
        );

        if (deleted is Result.Success)
        {
            // TODO: dispatch domain events
        }

        return deleted;
    }

    public async Task<Result.OfCommand> HandleAsync(Commands.ChangeOneOffEvent command)
    {
        // NOTE: we don't seem to have have any business logic to execute upon changing
        // a one-off event. So we don't go through the domain model and 'publish'
        // the domain event directly from the handler
        // NOTE: dunno if this 'shortcut' is a worthwhile optimization, though..
        Result.OfCommand changed = await this.repo.SaveUpdatesAsync(
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

        if (changed is Result.Success)
        {
            // TODO: dispatch domain events
        }

        return changed;
    }
}
