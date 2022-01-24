using System;
using NonJobEvent.Presenation.Api.DataAccess;

namespace NonJobEvent.Application.Handlers;

public class CalendarCommandQueryHandler
{
    private readonly ICalendarRepository repo;

    public CalendarCommandQueryHandler(ICalendarRepository repo)
    {
        this.repo = repo;
    }
}
