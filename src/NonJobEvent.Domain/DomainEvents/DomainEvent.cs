using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NonJobEvent.Domain.DomainEvents;

public abstract partial record DomainEvent
{
    public Guid Id { get; }

    private DomainEvent()
    {
        this.Id = Guid.NewGuid();
    }

    // TODO: consider flattening event payload instead of directly using domain classes
    public sealed record OneOffEventAdded(OneOffEvent AddedEvent, Calendar calendar) : DomainEvent;
}
