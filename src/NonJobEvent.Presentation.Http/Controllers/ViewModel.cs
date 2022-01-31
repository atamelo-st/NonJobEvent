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

        // TODO: optimize the payload: include only overrides, 'nest' the overrides inside the 'parent'
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

    public record CalendarSlice(Guid CalendarId, IEnumerable<ViewModel.Event> events) : ViewModel;
}
