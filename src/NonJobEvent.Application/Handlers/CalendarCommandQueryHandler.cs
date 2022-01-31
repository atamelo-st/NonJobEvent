using NonJobEvent.Application.Api;
using NonJobEvent.Application.Api.DataAccess;
using NonJobEvent.Common;
using NonJobEvent.Domain;
using NonJobEvent.Domain.DomainEvents;
using System.Diagnostics;

namespace NonJobEvent.Application.Handlers;

public class CalendarCommandQueryHandler :
    IQueryHandler<Queries.GetCalendarEvents, DataAccess.Result<IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>>>>,
    ICommandHandler<Commands.AddOneOffEvent, DataAccess.Result.Version>,
    ICommandHandler<Commands.DeleteOneOffEvent, Common.Void>,
    ICommandHandler<Commands.ChangeOneOffEvent, DataAccess.Result.Version>
{
    private readonly ICalendarRepository repo;

    public CalendarCommandQueryHandler(ICalendarRepository repo)
    {
        this.repo = repo;
    }

    public async Task<DataAccess.Result<IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>>>>
        HandleAsync(Queries.GetCalendarEvents query)
    {
        DataAccess.Result<Calendar> dataAccessResult = 
            await repo.GetCalendarAsync(
                query.CalendarId, 
                query.DateFrom,
                query.DateTo);

        Calendar calendar = dataAccessResult.Item;

        IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> events = 
            calendar.GetEvents(
                query.DateFrom,
                query.DateTo);

        return DataAccess.Result.From(events, dataAccessResult.Versions);
    }

    public async Task<DataAccess.Result.Version> HandleAsync(Commands.AddOneOffEvent command)
    {
        DataAccess.Result<Calendar> dataAcessResult = 
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

        bool added = calendar.AddOneOffEvent(oneOffEvent);

        if (added is false)
        {
            throw DataAccess.AlreadyExists($"Event with Id={command.EventId} already exists in calendar Id={command.CalendarId}");
        }

        DataAccess.Result<int> dataAccessResult = await this.repo.SaveUpdatesAsync(calendar.DomainEvents);

        int rowsAffected = dataAccessResult.Item;
        Debug.Assert(rowsAffected > 0);

        DataAccess.Result.Version version = dataAccessResult.Versions.Get(command.EventId);

        // TODO: dispatch domain events

        return version;
    }

    public async Task<Common.Void> HandleAsync(Commands.DeleteOneOffEvent command)
    {
        // NOTE: we don't seem to have have any business logic to execute upon deleting
        // a one-off event. So we don't go through the domain model and 'publish'
        // the domain event directly from the handler
        // NOTE: dunno if this 'shortcut' is a worthwhile optimization, though..
        DataAccess.Result<int> dataAccessResult = await this.repo.SaveUpdatesAsync(
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

    public async Task<DataAccess.Result.Version> HandleAsync(Commands.ChangeOneOffEvent command)
    {
        // NOTE: we don't seem to have have any business logic to execute upon changing
        // a one-off event. So we don't go through the domain model and 'publish'
        // the domain event directly from the handler
        // NOTE: dunno if this 'shortcut' is a worthwhile optimization, though..
       DataAccess.Result<int> dataAccessResult = await this.repo.SaveUpdatesAsync(
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

        DataAccess.Result.Version updatedVersion = dataAccessResult.Versions.Get(command.EventId);

        return updatedVersion;
    }
}
