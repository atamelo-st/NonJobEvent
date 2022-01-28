namespace NonJobEvent.Application.Api;

public static partial class Queries
{
    public sealed record GetCalendarEvents(Guid CalendarId, DateOnly DateFrom, DateOnly DateTo);
}