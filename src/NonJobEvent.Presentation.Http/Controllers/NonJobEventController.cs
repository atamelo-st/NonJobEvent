using Microsoft.AspNetCore.Mvc;
using NonJobEvent.Application;
using NonJobEvent.Common;
using NonJobEvent.Domain;
using NonJobEvent.Presenation.Api.DataAccess;

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

        [HttpGet("get-calendar-events")]
        public async Task<IActionResult> Get(Queries.GetCalendarEvents query)
        {
            // TODO: move this to a query handler
            Calendar calendar = await this.calendarRepo.GetCalendarAsync(query.calendarId, query.from, query.to);

            IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> appointments = 
                calendar
                    .GetEvents()
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


        [HttpPost("add-oneoff-event")]
        public async Task<IActionResult> AddOneOffEvent(
            Commands.AddOneOffEvent command,
            [FromServices] CommandHandler<Commands.AddOneOffEvent, bool> addOneOffEvent)
        {
            bool result = await addOneOffEvent(command);

            // TODO: do proper result handling, version propagation, etc
            return Ok(result);
        }
    }
}