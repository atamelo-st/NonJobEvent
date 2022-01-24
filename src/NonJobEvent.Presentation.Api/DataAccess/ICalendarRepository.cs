using NonJobEvent.Domain;

namespace NonJobEvent.Presenation.Api.DataAccess;

public interface ICalendarRepository
{
    Task<Calendar> GetCalendarAsync(Guid calendarId, DateOnly from, DateOnly to);

    Task<bool> DeleteOneOffEventAsync(Guid oneOffEventId);

    Task SaveUpdatesAsync(Calendar calendar);
}
