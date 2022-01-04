using Microsoft.AspNetCore.Mvc;
using NonJobAppointment.Common;
using NonJobAppointment.Domain;
using NonJobAppointment.WebApi.DataAccess;

namespace NonJobAppointment.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NonJobAppointmentController : ControllerBase
    {
        private readonly ICalendarRepository calendarRepo;
        private readonly ILogger<NonJobAppointmentController> logger;

        public NonJobAppointmentController(
            ICalendarRepository calendarRepo,
            ILogger<NonJobAppointmentController> logger)
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
            Calendar calendar = this.calendarRepo.Get(calendarId, from, to);

            IEnumerable<OneOf<OneOffAppointment, RecurringAppointment.Occurrence>> appointments = 
                calendar
                    .GetAppointments()
                    .ToList();

            IEnumerable<ViewModel.Appointment> appointmentViewModels =
                appointments
                    .Select(appointment =>
                        appointment.TheOne switch
                        {
                            OneOffAppointment oneOff => OneOffViewModel(from: oneOff),
                            RecurringAppointment.Occurrence occurrence => OccurrenceViewModel(from: occurrence),
                            _ => throw BadMatch.ShouldNotHappen(),
                        })
                    .ToList();

            return Ok(appointmentViewModels);

            static ViewModel.Appointment OneOffViewModel(OneOffAppointment from)
                => new ViewModel.Appointment.OneOff(from.Id, from.Title, from.Summary, from.Date, from.TechnicianId, from.TimeseetCode,
                    from.TimeFrame.IsAllDay, from.TimeFrame.StartTime, from.TimeFrame.EndTime);

            static ViewModel.Appointment OccurrenceViewModel(RecurringAppointment.Occurrence from)
                => new ViewModel.Appointment.RecurringOccurrence(from.Parent.Id, from.Parent.Title, from.Summary, from.Date, from.Parent.TechnicianId,
                    from.Parent.TimeseetCode, from.TimeFrame.IsAllDay, from.TimeFrame.StartTime, from.TimeFrame.EndTime);
        }
    }
}