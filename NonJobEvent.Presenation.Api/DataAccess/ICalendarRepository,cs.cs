using NonJobEvent.Domain;

namespace NonJobEvent.Presenation.Api.DataAccess;

public interface ICalendarRepository
{
    Calendar GetCalendar(Guid calendarId, DateOnly from, DateOnly to);
}
