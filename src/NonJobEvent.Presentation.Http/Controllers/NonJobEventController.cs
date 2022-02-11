using Microsoft.AspNetCore.Mvc;
using NonJobEvent.Application;
using NonJobEvent.Application.Api;
using NonJobEvent.Application.Handlers;
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

        [HttpGet("stream-calendar-events")]
        public IAsyncEnumerable<object> Stream(
            Queries.GetCalendarEvents query,
            [FromServices] IGetCalendarEventsQueryHandler getCalendarEvents)
        {
            // TODO: design app-level streaming protocol for the calendar event objects
            throw new NotImplementedException();
        }

        [HttpGet("get-calendar-events")]
        public async Task<IActionResult> Get(
            Queries.GetCalendarEvents query,
            [FromServices] IGetCalendarEventsQueryHandler getCalendarEvents)
        {
            var eventData = await getCalendarEvents.HandleAsync(query);

            ViewModel.CalendarSlice viewModel = EventsToViewModel(query.CalendarId, eventData);

            return base.Ok(viewModel);

            static ViewModel.CalendarSlice EventsToViewModel(
                Guid calendarId,
                Persistence.Result<IEnumerable<OneOf<OneOffEvent, RecurringEvent.Occurrence>>> eventsData)
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

            static ViewModel.Event OneOffViewModel(OneOffEvent from, Persistence.Result.VersionData versions)
                => new ViewModel.Event.OneOff(from.Id, from.Title, from.Summary, from.Date, from.TimeseetCode,
                    from.TimeFrame.IsAllDay, from.TimeFrame.StartTime, from.TimeFrame.EndTime);

            static ViewModel.Event OccurrenceViewModel(RecurringEvent.Occurrence from, Persistence.Result.VersionData versions)
                => new ViewModel.Event.RecurringOccurrence(from.Parent.Id, from.Title, from.Summary, from.Date,
                    from.Parent.TimeseetCode, from.TimeFrame.IsAllDay, from.TimeFrame.StartTime, from.TimeFrame.EndTime);
        }

        [HttpPut("add-oneoff-event")]
        public async Task<IActionResult> AddOneOffEvent(
            Commands.AddOneOffEvent command,
            [FromServices] IAddOneOffEventCommandHandler addOneOffEvent)
        {
            Persistence.Result.Version version = await addOneOffEvent.HandleAsync(command);

            // TODO: do proper result handling, version propagation, etc
            return Ok(version.Value);
        }

        [HttpPut("delete-oneoff-event")]
        public async Task<IActionResult> DeleteOneOffEvent(
            Commands.DeleteOneOffEvent command,
            [FromServices] IDeleteOneOffEventCommandHandler deleteOneOffEvent)
        {
            await deleteOneOffEvent.HandleAsync(command);

            return Ok();
        }

        [HttpPut("change-oneoff-event")]
        public async Task<IActionResult> ChangeOneOffEvent(
            Commands.ChangeOneOffEvent command,
            [FromServices] IChangeOneOffEventCommandHandler changeOneOffEvent)
        {
            Persistence.Result.Version updatedVersion = await changeOneOffEvent.HandleAsync(command);

            return Ok(updatedVersion.Value);
        }
    }
}