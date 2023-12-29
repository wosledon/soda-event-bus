namespace Soda.EventBus.Infrastructure;

public interface IAsyncEventHandler<in TEvent> where TEvent : IEvent
{
    Task HandleAsync(IEvent @event);

    void HandleException(IEvent @event, Exception ex);
}
