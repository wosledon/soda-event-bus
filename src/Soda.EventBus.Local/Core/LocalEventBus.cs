using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Soda.EventBus.Infrastructure;

namespace Soda.EventBus.Local.Core;

public interface ILocalEventBus : IEventBus
{

}

public class LocalEventBus(IServiceProvider serviceProvider, LocalEventBusOptions options) : ILocalEventBus
{
    private LocalEventBusPool? EventBusPool => serviceProvider.GetService<LocalEventBusPool>();


    public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
    {
        if (options.Pool)
        {
            Debug.Assert(EventBusPool != null, nameof(EventBusPool) + " != null");
            EventBusPool.Publish(@event);
        }
        else
        {
            var manager = serviceProvider.GetService<LocalEventBusManager<TEvent>>();
            if (manager is null) throw new NullReferenceException($"No manager for event {typeof(TEvent).Name}, please add singleton service it.");
            manager.Publish(@event);
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        if (options.Pool)
        {
            Debug.Assert(EventBusPool != null, nameof(EventBusPool) + " != null");
            await EventBusPool.PublishAsync(@event);
        }
        else
        {
            var manager = serviceProvider.GetService<LocalEventBusManager<TEvent>>();
            if (manager is null) throw new NullReferenceException($"No manager for event {typeof(TEvent).Name}, please add singleton service it.");
            await manager.PublishAsync(@event);
        }
    }

    public void OnSubscribe<TEvent>() where TEvent : IEvent
    {
        if (options.Pool)
        {
            Debug.Assert(EventBusPool != null, nameof(EventBusPool) + " != null");
            EventBusPool.OnSubscribe<TEvent>();
        }
        else
        {
            var manager = serviceProvider.GetService<LocalEventBusManager<TEvent>>();
            if (manager is null) throw new NullReferenceException($"No manager for event {typeof(TEvent).Name}, please add singleton service it.");
            manager.AutoHandle();
        }
    }
}
