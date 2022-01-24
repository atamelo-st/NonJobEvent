using System;
using NonJobEvent.Domain;
using NonJobEvent.Presenation.Api.DataAccess;

namespace NonJobEvent.Application.Handlers;

public class CalendarCommandQueryHandler :
    IQueryHandler<Queries.GetCalendarEvents, object>,
    ICommandHandler<Commands.AddOneOffEvent, bool>,
    ICommandHandler<Commands.DeleteOneOffEvent, bool>
{
    private readonly ICalendarRepository repo;

    public CalendarCommandQueryHandler(ICalendarRepository repo)
    {
        this.repo = repo;
    }

    public Task<object> HandleAsync(Queries.GetCalendarEvents query)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> HandleAsync(Commands.AddOneOffEvent command)
    {
        Calendar calendar = await this.repo.GetCalendarAsync(command.CalenderId, command.EventDate, command.EventDate);

        OneOffEvent oneOffEvent = new(
            command.EventId,
            command.EventTitle,
            command.EventSummary,
            command.EventDate,
            command.EventTimeFrame,
            command.EventTechnicianId,
            command.EventTimeseetCode
        );

        bool added = calendar.AddOneOffEvent(oneOffEvent);

        if (added)
        {
            await this.repo.SaveUpdatesAsync(calendar);

            // TODO: dispatch domain events
        }

        return added;
    }

    public async Task<bool> HandleAsync(Commands.DeleteOneOffEvent command)
    {
        bool deleted = await this.repo.DeleteOneOffEventAsync(command.EventId);

        if (deleted)
        {
            // NOTE: we don't seem to have have any business logic to execute upon deleting
            // a one-off event. So we don't go through the domain model and 'publish'
            // the domain event directly from the handler

            // TODO: dispatch domain events
        }

        return deleted;
    }
}
