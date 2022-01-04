namespace NonJobAppointment.WebApi.Controllers;

public abstract record ViewModel
{
    public abstract record Appointment
    {
        public record OneOff(
            Guid Id,
            string Title,
            string Summary,
            DateOnly Date,
            long TechnicianId,
            long TimesheetCodeId,
            bool IsAllDay,
            TimeOnly? StartTime = null,
            TimeOnly? EndTime = null
        ) : Appointment;

        public record RecurringOccurrence(
            Guid ParentId,
            string Title,
            string Summary,
            DateOnly Date,
            long TechnicianId,
            long TimesheetCodeId,
            bool IsAllDay,
            TimeOnly? StartTime = null,
            TimeOnly? EndTime = null
        ) : Appointment;
    }


}
