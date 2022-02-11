using NonJobEvent.Application.Api;
using NonJobEvent.Common;
using NonJobEvent.Domain;
using NonJobEvent.Domain.DomainEvents;
using System.Diagnostics;

namespace NonJobEvent.Application.Handlers;

// DL: when looking at functions that consume `IQueryHandler`/`ICommandHandler` in the controllers, I felt some cognitive overload,
// particularly with nested types (like `IQueryHandler<Queries.GetCalendarEvents, Persistence.Result<IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>>>>`).
// What do you think of explicitly-named interfaces instead?
public interface IGetCalendarEventsQueryHandler
    : IQueryHandler<Queries.GetCalendarEvents, Persistence.Result<IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>>>> { }

public interface IAddOneOffEventCommandHandler 
    : ICommandHandler<Commands.AddOneOffEvent, Persistence.Result.Version> { }

public interface IDeleteOneOffEventCommandHandler 
    : ICommandHandler<Commands.DeleteOneOffEvent, Common.Void> { }

public interface IChangeOneOffEventCommandHandler
    : ICommandHandler<Commands.ChangeOneOffEvent, Persistence.Result.Version> { }


public class CalendarCommandQueryHandler :
    IGetCalendarEventsQueryHandler,
    IAddOneOffEventCommandHandler,
    IDeleteOneOffEventCommandHandler,
    IChangeOneOffEventCommandHandler
{
    private readonly ICalendarRepository repo;

    public CalendarCommandQueryHandler(ICalendarRepository repo)
    {
        this.repo = repo;
    }

    public async Task<Persistence.Result<IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>>>>
        HandleAsync(Queries.GetCalendarEvents query)
    {
        Persistence.Result<Calendar> dataAccessResult = 
            await repo.GetCalendarAsync(
                query.CalendarId, 
                query.DateFrom,
                query.DateTo);

        Calendar calendar = dataAccessResult.Item;

        IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> events = 
            calendar.GetEvents(
                query.DateFrom,
                query.DateTo);

        return Persistence.Result.From(events, dataAccessResult.Versions);
    }

    public async Task<Persistence.Result.Version> HandleAsync(Commands.AddOneOffEvent command)
    {
        Persistence.Result<Calendar> dataAcessResult = 
            await this.repo.GetCalendarAsync(
                command.CalendarId,
                command.EventDate, 
                command.EventDate);

        Calendar calendar = dataAcessResult.Item;

        OneOffEvent oneOffEvent = new(
            command.EventId,
            command.EventTitle,
            command.EventSummary,
            command.EventDate,
            command.EventTimeFrame,
            command.EventTimeseetCode
        );

        bool added = calendar.TryAddOneOffEvent(oneOffEvent);

        if (added is false)
        {
            throw Persistence.EventAlreadyExists(command.CalendarId, command.EventId);
        }

        Persistence.Result<int> dataAccessResult = await this.repo.SaveUpdatesAsync(calendar.DomainEvents);

        int rowsAffected = dataAccessResult.Item;
        Debug.Assert(rowsAffected > 0);

        Persistence.Result.Version version = dataAccessResult.Versions.Get(command.EventId);

        // TODO: dispatch domain events

        return version;
    }

    public async Task<Common.Void> HandleAsync(Commands.DeleteOneOffEvent command)
    {
        // NOTE: we don't seem to have have any business logic to execute upon deleting
        // a one-off event. So we don't go through the domain model and 'publish'
        // the domain event directly from the handler
        // NOTE: dunno if this 'shortcut' is a worthwhile optimization, though..
        Persistence.Result<int> dataAccessResult = await this.repo.SaveUpdatesAsync(
            new List<DomainEvent> 
            { 
                new DomainEvent.OneOffEventDeleted(command.CalendarId, command.EventId)
            }
        );

        int rowsAffected = dataAccessResult.Item;
        Debug.Assert(rowsAffected > 0);

        // TODO: dispatch domain events

        return Common.Void.Self();
    }

    public async Task<Persistence.Result.Version> HandleAsync(Commands.ChangeOneOffEvent command)
    {
        // NOTE: we don't seem to have have any business logic to execute upon changing
        // a one-off event. So we don't go through the domain model and 'publish'
        // the domain event directly from the handler
        // NOTE: dunno if this 'shortcut' is a worthwhile optimization, though..
       Persistence.Result<int> dataAccessResult = await this.repo.SaveUpdatesAsync(
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

        int rowsAffected = dataAccessResult.Item;
        Debug.Assert(rowsAffected > 0);

        // TODO: dispatch domain events

        Persistence.Result.Version updatedVersion = dataAccessResult.Versions.Get(command.EventId);

        return updatedVersion;
    }
}
