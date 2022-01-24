namespace NonJobEvent.Presentation.Http.Controllers;

public abstract partial record ViewModel
{
    public abstract record Event
    {
        // TODO: add version number for optimistic concurrency control

        public record OneOff(
            Guid Id,
            string Title,
            string Summary,
            DateOnly Date,
            long TimesheetCodeId,
            bool IsAllDay,
            TimeOnly? StartTime = null,
            TimeOnly? EndTime = null
        ) : Event;

        public record RecurringOccurrence(
            Guid ParentId,
            string Title,
            string Summary,
            DateOnly Date,
            long TimesheetCodeId,
            bool IsAllDay,
            TimeOnly? StartTime = null,
            TimeOnly? EndTime = null
        ) : Event;
    }

    public record CalendarSlice(Guid CalendarId, IEnumerable<Event> events) : ViewModel;
}
