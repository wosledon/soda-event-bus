namespace Soda.EventBus.Core;

public interface IAsyncEventHandler<IEvent>
{
    Task HandleAsync(IEvent @event);

    Task HandleExceptionAsync(IEvent @event, Exception ex);
}
