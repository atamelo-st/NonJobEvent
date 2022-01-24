using NonJobAppointment.Domain;

namespace NonJobAppointment.WebApi.DataAccess;

public interface ICalendarRepository
{
    Calendar GetCalendar(Guid calendarId, DateOnly from, DateOnly to);
}
