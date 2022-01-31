using Microsoft.AspNetCore.Mvc;
using NonJobEvent.Application;
using NonJobEvent.Application.Api;
using NonJobEvent.Application.Api.DataAccess;
using NonJobEvent.Common;
using NonJobEvent.Domain;
using NonJobEvent.Presentation.Http.Controllers;
using Void = NonJobEvent.Common.Void;

namespace NonJobAppointment.WebApi.Controllers
{
    // TODO: add exception filter
    // TODO: add controller base class?
    [ApiController]
    [Route("[controller]")]
    public class NonJobEventController : ControllerBase
    {
        private readonly ILogger<NonJobEventController> logger;

        public NonJobEventController(
            ILogger<NonJobEventController> logger)
        {
            this.logger = logger;
        }

        [HttpPut]
        public IActionResult Create()
        {
            throw new NotImplementedException();
        }

        [HttpGet("get-calendar-events")]
        public async Task<IActionResult> Get(
            Queries.GetCalendarEvents query,
            [FromServices] QueryHandler<
                Queries.GetCalendarEvents,
                DataAccess.Result<IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>>>> getCalendarEvents)
        {
            var eventData = await getCalendarEvents(query);

            ViewModel.CalendarSlice viewModel = EventsToViewModel(query.CalendarId, eventData);

            return base.Ok(viewModel);

            static ViewModel.CalendarSlice EventsToViewModel(
                Guid calendarId,
                DataAccess.Result<IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>>> eventsData)
            {
                IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>> events = eventsData.Item;
                
                IEnumerable<ViewModel.Event> eventViewModels = events.Select(@event => 
                    @event.TheOne switch
                    {
                        OneOffEvent oneOff => OneOffViewModel(from: oneOff, eventsData.Versions),
                        RecurringEvent.Occurrence occurrence => OccurrenceViewModel(from: occurrence, eventsData.Versions),
                        _ => throw BadMatch.ShouldNotHappen(),
                    }).ToList();

                ViewModel.CalendarSlice viewModel = new(calendarId, eventViewModels);

                return viewModel;
            }

            static ViewModel.Event OneOffViewModel(OneOffEvent from, DataAccess.Result.VersionData versions)
                => new ViewModel.Event.OneOff(from.Id, from.Title, from.Summary, from.Date, from.TimeseetCode,
                    from.TimeFrame.IsAllDay, from.TimeFrame.StartTime, from.TimeFrame.EndTime);

            static ViewModel.Event OccurrenceViewModel(RecurringEvent.Occurrence from, DataAccess.Result.VersionData versions)
                => new ViewModel.Event.RecurringOccurrence(from.Parent.Id, from.Title, from.Summary, from.Date,
                    from.Parent.TimeseetCode, from.TimeFrame.IsAllDay, from.TimeFrame.StartTime, from.TimeFrame.EndTime);
        }

        [HttpPut("add-oneoff-event")]
        public async Task<IActionResult> AddOneOffEvent(
            Commands.AddOneOffEvent command,
            [FromServices] CommandHandler<Commands.AddOneOffEvent, DataAccess.Result.Version> addOneOffEvent)
        {
            DataAccess.Result.Version version = await addOneOffEvent(command);

            // TODO: do proper result handling, version propagation, etc
            return Ok(version.Value);
        }

        [HttpPut("delete-oneoff-event")]
        public async Task<IActionResult> DeleteOneOffEvent(
            Commands.DeleteOneOffEvent command,
            [FromServices] CommandHandler<Commands.DeleteOneOffEvent, Void> deleteOneOffEvent)
        {
            await deleteOneOffEvent(command);

            return Ok();
        }

        [HttpPut("change-oneoff-event")]
        public async Task<IActionResult> ChangeOneOffEvent(
            Commands.ChangeOneOffEvent command,
            [FromServices] CommandHandler<Commands.ChangeOneOffEvent, DataAccess.Result.Version> changeOneOffEvent)
        {
            DataAccess.Result.Version updatedVersion = await changeOneOffEvent(command);

            return Ok(updatedVersion.Value);
        }
    }
}