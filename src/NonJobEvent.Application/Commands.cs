using NonJobEvent.Domain;

namespace NonJobEvent.Application;

public static partial class Commands
{
    public sealed record AddOneOffEvent(
        Guid CalendarId,
        Guid EventId,
        string EventTitle,
        string EventSummary,
        DateOnly EventDate,
        TimeFrame EventTimeFrame,
        int EventTimeseetCode
    );

    public sealed record DeleteOneOffEvent(
        Guid EventId,
        Guid CalendarId
    );

    public sealed record ChangeOneOffEvent(
        Guid CalendarId,
        Guid EventId,
        string? NewEventTitle,
        string? NewEventSummary,
        DateOnly? NewEventDate,
        TimeFrame? NewEventTimeFrame,
        int? NewEventTimeseetCode,
        uint ExpectedVersion
    );

    public sealed record AddRecurringEvent(
        Guid Id,
        string Title,
        string Summary,
        DateOnly StartDate,
        TimeFrame timeFrame,
        int timeseetCode,
        RecurrencePattern pattern
    );
}
