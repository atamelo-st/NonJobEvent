using NonJobEvent.Domain;

namespace NonJobEvent.Application;

public static class Commands
{
    public sealed record AddOneOffEvent(
        Guid Id,
        string Title,
        string Summary,
        DateOnly Date,
        TimeFrame TimeFrame,
        long TechnicianId,
        int TimeseetCode
    );
}
