using System.Threading.Tasks;

namespace Web.Cqrs
{
    public interface IHandle<TEvent>
        where TEvent : Event
    {
        Task Handle(TEvent @event);
    }
}