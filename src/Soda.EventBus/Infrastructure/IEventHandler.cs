namespace Soda.EventBus.Infrastructure;

public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    void Handle(TEvent @event);

    void HandleException(TEvent @event, Exception ex);
}
