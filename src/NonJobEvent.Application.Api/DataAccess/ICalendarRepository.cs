using NonJobEvent.Domain;
using NonJobEvent.Domain.DomainEvents;

namespace NonJobEvent.Application.Api.DataAccess;

public interface ICalendarRepository
{
    Task<Calendar?> GetCalendarAsync(Guid calendarId, DateOnly dateFrom, DateOnly dateTo);

    Task<Result.OfCommand> SaveUpdatesAsync(IReadOnlyList<DomainEvent> updates);
}
