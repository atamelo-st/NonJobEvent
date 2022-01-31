using NonJobEvent.Domain;
using NonJobEvent.Domain.DomainEvents;

namespace NonJobEvent.Application.Api.DataAccess;

public interface ICalendarRepository
{
    Task<Calendar> GetCalendarAsync(Guid calendarId, DateOnly dateFrom, DateOnly dateTo);

    Task<int> SaveUpdatesAsync(IReadOnlyList<DomainEvent> updates);
}


public abstract class DataAccess
{
    public static Exception.NotFound NotFound(
        string message,
        System.Exception? innerExeption = null) => new(message, innerExeption);

    public static Exception.AlreadyExists AlreadyExists(
        string message,
        System.Exception? innerExeption = null) => new(message, innerExeption);

    public static Exception.ConcurrencyConflict ConcurrencyConflict(
        string message,
        System.Exception? innerExeption = null) => new(message, innerExeption);

    public abstract class Exception : System.Exception
    {
        public sealed class NotFound : DataAccess.Exception
        {
            public NotFound(
                string message, 
                System.Exception? innerException = null) : base(message, innerException) { }
        }

        public sealed class AlreadyExists : DataAccess.Exception
        {
            public AlreadyExists(
                string message,
                System.Exception? innerException = null) : base(message, innerException) { }
        }

        public sealed class ConcurrencyConflict : DataAccess.Exception
        {
            public ConcurrencyConflict(
                string message,
                System.Exception? innerException = null) : base(message, innerException) { }
        }

        private Exception(
            string message,
            System.Exception? innerException) : base(message, innerException) { }
    }
}