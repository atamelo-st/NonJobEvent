using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NonJobAppointment.Domain;

public class Calendar
{
    private readonly IEnumerable<Appointment> appointments;

    public Calendar(IEnumerable<Appointment> appointments)
    {
        this.appointments = appointments;
    }

    IEnumerable<AppointmentBase> GetAppointments()
    {
        throw new NotImplementedException();
    }
}