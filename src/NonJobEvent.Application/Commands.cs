using NonJobEvent.Domain;

namespace NonJobEvent.Application;

public static partial class Commands
{
    public sealed record AddOneOffEvent(
        Guid CalenderId,
        Guid EventId,
        string EventTitle,
        string EventSummary,
        DateOnly EventDate,
        TimeFrame EventTimeFrame,
        int EventTimeseetCode
    );

    public sealed record DeleteOneOffEvent(
        Guid EventId
    );
}
