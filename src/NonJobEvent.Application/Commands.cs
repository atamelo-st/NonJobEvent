using NonJobEvent.Domain;

namespace NonJobEvent.Application;

public static class Commands
{
    public sealed record AddOneOffEvent(
        Guid CalenderId,
        Guid EventId,
        string EventTitle,
        string EventSummary,
        DateOnly EventDate,
        TimeFrame EventTimeFrame,
        long EventTechnicianId,
        int EventTimeseetCode
    );

    public sealed record DeleteOneOffEvent(
        Guid EventId
    );
}
