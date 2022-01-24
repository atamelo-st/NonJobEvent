using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NonJobEvent.Application;

public static partial class Queries
{
    public sealed record GetCalendarEvents(Guid CalendarId, DateOnly From, DateOnly To);
}