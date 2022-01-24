using System;
using NonJobEvent.Domain;
using NonJobEvent.Presenation.Api.DataAccess;

namespace NonJobEvent.Application.Handlers;

public class CalendarCommandQueryHandler :
    IQueryHandler<Queries.GetCalendarEvents, object>,
    ICommandHandler<Commands.AddOneOffEvent, bool>
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
        }

        return added;
    }
}
