// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");


public record OneOffAppointment(
    string Title,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    long TechnicianId,
    int TimeseetCode
 );

public record RecurrencePattern();

public class OccurenceGenerator
{
    public static IEnumerable<RecurringAppointment.Occurrence> GenerateForPeriod(
        DateOnly from,
        DateOnly to,
        DateOnly startDate,
        RecurrencePattern pattern)
    {
        throw new NotImplementedException();
    }
}

public record RecurringAppointment(
    string Title,
    DateOnly StartDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    long TechnicianId,
    int TimeseetCode,
    RecurrencePattern Pattern)
{
    public record Occurrence(string Title, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime);

    public IEnumerable<Occurrence> ExpandOccurrences(DateOnly from, DateOnly to)
    {
        IEnumerable<Occurrence> occurrences = OccurenceGenerator.GenerateForPeriod(from, to, this.StartDate, this.Pattern);

        return occurrences;
    }
}