namespace Soda.EventBus.Core;

public interface IEventHandler<IEvent>
{
    void Handle(IEvent @event);

    void HandleException(IEvent @event, Exception ex);
}
