using NonJobEvent.Domain;
using NonJobEvent.Domain.DomainEvents;

namespace NonJobEvent.Presenation.Api.DataAccess;

public interface ICalendarRepository
{
    Task<Calendar> GetCalendarAsync(Guid calendarId, DateOnly from, DateOnly to);

    Task<bool> SaveUpdatesAsync(IReadOnlyList<DomainEvent> updates);
}
