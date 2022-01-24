using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NonJobEvent.Application;

public static class Queries
{
    public sealed record GetCalendarEvents(Guid calendarId, DateOnly from, DateOnly to);
}