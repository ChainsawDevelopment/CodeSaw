using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;

namespace Web.Cqrs
{
    public class EventAccumulator : IEventBus
    {
        private readonly ILifetimeScope _scope;
        private readonly List<Event> _events;

        public EventAccumulator(ILifetimeScope scope)
        {
            _scope = scope;
            _events = new List<Event>();
        }

        public void Publish(Event @event)
        {
            _events.Add(@event);
        }

        public async Task Flush()
        {
            foreach (var @event in _events)
            {
                await (Task) ((dynamic)this).DispatchEvent((dynamic)@event);
            }
        }

        private async Task DispatchEvent<TEvent>(TEvent @event) where TEvent : Event
        {
            var handlers = _scope.Resolve<IEnumerable<IHandle<TEvent>>>();
            foreach (var handler in handlers)
            {
                await handler.Handle(@event);
            }
        }
    }
}