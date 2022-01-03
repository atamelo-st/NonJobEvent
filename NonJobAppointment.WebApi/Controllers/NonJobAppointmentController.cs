using Microsoft.AspNetCore.Mvc;
using NonJobAppointment.Domain;

namespace NonJobAppointment.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NonJobAppointmentController : ControllerBase
    {
        private readonly ILogger<NonJobAppointmentController> _logger;

        public NonJobAppointmentController(ILogger<NonJobAppointmentController> logger)
        {
            _logger = logger;
        }

        [HttpPut]
        public IActionResult Create()
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public IActionResult Get(DateOnly from, DateOnly to)
        {
            IEnumerable<ViewModel.Appointment> appointments = null!;

            return Ok(appointments);
        }
    }
}