namespace NonJobAppointment.WebApi.ViewModels;

public record OneOffAppointmentViewModel(
    Guid Id,
    string Name,
    string Summary,
    DateOnly Date,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    long TechnicianId,
    long TimesheetCodeId
)
{
    public bool IsAllDay => this.StartTime is null && this.EndTime is null;
}
