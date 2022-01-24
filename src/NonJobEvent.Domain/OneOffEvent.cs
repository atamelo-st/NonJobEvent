namespace NonJobEvent.Domain;

public sealed record OneOffEvent(
        Guid Id,
        string Title,
        string Summary,
        DateOnly Date,
        TimeFrame TimeFrame,
        int TimeseetCode
) : Event(Id, Title, Summary, TimeFrame, TimeseetCode);
