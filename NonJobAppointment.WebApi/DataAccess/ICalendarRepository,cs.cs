using NonJobAppointment.Domain;

namespace NonJobAppointment.WebApi.DataAccess;

public interface ICalendarRepository
{
    Calendar Get(Guid calendarId, DateOnly from, DateOnly to);
}
