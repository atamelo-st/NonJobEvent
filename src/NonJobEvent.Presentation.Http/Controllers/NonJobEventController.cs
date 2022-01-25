using Microsoft.AspNetCore.Mvc;
using NonJobEvent.Application;
using NonJobEvent.Common;
using NonJobEvent.Domain;
using NonJobEvent.Presenation.Api.DataAccess;
using NonJobEvent.Presentation.Http.Controllers;

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
            Calendar calendar = await this.calendarRepo.GetCalendarAsync(query.CalendarId, query.From, query.To);

            IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> events =
                calendar
                    .GetEvents()
                    .ToList();

            IEnumerable<ViewModel.Event> eventViewModels =
                events
                    .Select(@event =>
                        @event.TheOne switch
                        {
                            OneOffEvent oneOff => OneOffViewModel(from: oneOff),
                            RecurringEvent.Occurrence occurrence => OccurrenceViewModel(from: occurrence),
                            _ => throw BadMatch.ShouldNotHappen(),
                        })
                    .ToList();

            ViewModel.CalendarSlice result = new(query.CalendarId, eventViewModels);

            return Ok(result);

            static ViewModel.Event OneOffViewModel(OneOffEvent from)
                => new ViewModel.Event.OneOff(from.Id, from.Title, from.Summary, from.Date, from.TimeseetCode,
                    from.TimeFrame.IsAllDay, from.TimeFrame.StartTime, from.TimeFrame.EndTime);

            static ViewModel.Event OccurrenceViewModel(RecurringEvent.Occurrence from)
                => new ViewModel.Event.RecurringOccurrence(from.Parent.Id, from.Parent.Title, from.Summary, from.Date,
                    from.Parent.TimeseetCode, from.TimeFrame.IsAllDay, from.TimeFrame.StartTime, from.TimeFrame.EndTime);
        }


        [HttpPut("add-oneoff-event")]
        public async Task<IActionResult> AddOneOffEvent(
            Commands.AddOneOffEvent command,
            [FromServices] CommandHandler<Commands.AddOneOffEvent, bool> addOneOffEvent)
        {
            bool added = await addOneOffEvent(command);

            // TODO: do proper result handling, version propagation, etc
            return Ok(added);
        }

        [HttpPut("delete-oneoff-event")]
        public async Task<IActionResult> DeleteOneOffEvent(
            Commands.DeleteOneOffEvent command,
            [FromServices] CommandHandler<Commands.DeleteOneOffEvent, bool> deleteOneOffEvent)
        {
            bool deleted = await deleteOneOffEvent(command);

            return Ok(deleted);
        }

        [HttpPut("change-oneoff-event")]
        public async Task<IActionResult> ChangeOneOffEvent(
            Commands.ChangeOneOffEvent command,
            [FromServices] CommandHandler<Commands.ChangeOneOffEvent, bool> changeOneOffEvent)
        {
            bool changed = await changeOneOffEvent(command);

            return Ok(changed);
        }
    }
}