using NonJobEvent.Domain;

namespace NonJobEvent.Presenation.Api.DataAccess;

public interface ICalendarRepository
{
    Task<Calendar> GetCalendarAsync(Guid calendarId, DateOnly from, DateOnly to);

    Task SaveUpdatesAsync(Calendar calendar);
}
