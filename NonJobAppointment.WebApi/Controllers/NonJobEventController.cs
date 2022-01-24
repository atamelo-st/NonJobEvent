using Microsoft.AspNetCore.Mvc;
using NonJobAppointment.Common;
using NonJobAppointment.Domain;
using NonJobAppointment.WebApi.DataAccess;

namespace NonJobAppointment.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NonJobEventController : ControllerBase
    {
        private readonly ICalendarRepository calendarRepo;
        private readonly ILogger<NonJobEventController> logger;

        public NonJobEventController(
            ICalendarRepository calendarRepo,
            ILogger<NonJobEventController> logger)
        {
            this.calendarRepo = calendarRepo;
            this.logger = logger;
        }

        [HttpPut]
        public IActionResult Create()
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public IActionResult Get(Guid calendarId, DateOnly from, DateOnly to)
        {
            // TODO: move this to a query handler
            Calendar calendar = this.calendarRepo.GetCalendar(calendarId, from, to);

            IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> appointments = 
                calendar
                    .GetAppointments()
                    .ToList();

            IEnumerable<ViewModel.Event> appointmentViewModels =
                appointments
                    .Select(appointment =>
                        appointment.TheOne switch
                        {
                            OneOffEvent oneOff => OneOffViewModel(from: oneOff),
                            RecurringEvent.Occurrence occurrence => OccurrenceViewModel(from: occurrence),
                            _ => throw BadMatch.ShouldNotHappen(),
                        })
                    .ToList();

            return Ok(appointmentViewModels);

            static ViewModel.Event OneOffViewModel(OneOffEvent from)
                => new ViewModel.Event.OneOff(from.Id, from.Title, from.Summary, from.Date, from.TechnicianId, from.TimeseetCode,
                    from.TimeFrame.IsAllDay, from.TimeFrame.StartTime, from.TimeFrame.EndTime);

            static ViewModel.Event OccurrenceViewModel(RecurringEvent.Occurrence from)
                => new ViewModel.Event.RecurringOccurrence(from.Parent.Id, from.Parent.Title, from.Summary, from.Date, from.Parent.TechnicianId,
                    from.Parent.TimeseetCode, from.TimeFrame.IsAllDay, from.TimeFrame.StartTime, from.TimeFrame.EndTime);
        }


        [HttpPost]
        public IActionResult AddOneOffEvent(
            Guid calendarId,
            string title,
            string summary,
            DateOnly date,
            TimeOnly? startTime,
            TimeOnly? endTime,
            int timesheetCode)
        {
            throw new NotImplementedException();
        }
    }
}