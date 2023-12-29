namespace Soda.EventBus.Infrastructure;

public interface IEventBus
{
    void Publish<TEvent>(TEvent @event) where TEvent : IEvent;
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent;
    
    void OnSubscribe<TEvent>() where TEvent : IEvent;
}