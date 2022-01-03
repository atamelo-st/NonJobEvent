namespace NonJobAppointment.Domain;

public sealed record OneOffAppointment(
        Guid Id,
        string Title,
        DateOnly Date,
        TimeFrame TimeFrame,
        long TechnicianId,
        int TimeseetCode
) : Appointment(Id, Title, TimeFrame, TechnicianId, TimeseetCode);
