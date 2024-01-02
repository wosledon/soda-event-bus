using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Soda.EventBus.Infrastructure;

namespace Soda.EventBus.EntityframeworkCore;

public static class EventBusEntityframeworkCoreExtensions
{

}

public class EventBusDbContext<TDbContext> : DbContext
    where TDbContext : DbContext
{
    public EventBusDbContext(DbContextOptions<TDbContext> options) : base(options)
    {
    }

    public override int SaveChanges()
    {
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await HandleEvents();

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await HandleEvents();

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task HandleEvents()
    {
        foreach (var entityEntry in ChangeTracker.Entries<AggregateRoot>())
        {
            while (entityEntry.Entity.GetLocalEvent(out var @event))
            {
                if (@event is null) break;

                await HandleEvent(@event);
            }

            entityEntry.Entity.ClearLocalEvents();
        }
    }

    private async Task HandleEvent(object @event)
    {
        var eventHandlerType = typeof(IAsyncEventHandler<>).MakeGenericType(@event.GetType());
        var eventHandlers = this.GetService<IEnumerable<object>>()
            .Where(s => eventHandlerType.IsAssignableFrom(s.GetType()));

        foreach (var eventHandler in eventHandlers)
        {
            var method = eventHandler.GetType().GetMethod(nameof(IAsyncEventHandler<IEvent>.HandleAsync));
            var exceptionHandleMethod = eventHandlerType.GetMethod(nameof(IAsyncEventHandler<IEvent>.HandleException));

            try
            {
                await (Task)method!.Invoke(eventHandler, new[] { @event })!;
            }
            catch (Exception ex)
            {
                exceptionHandleMethod!.Invoke(eventHandler, new[] { @event, ex });
            }
        }
    }
}

public abstract class AggregateRoot
{
    public ConcurrentQueue<object> LocalEvents { get; } = new();

    public void AddLocalEvent<TEvent>(TEvent eventData) where TEvent : IEvent
    {
        LocalEvents.Enqueue(eventData);
    }

    public bool GetLocalEvent(out object? @event)
    {
        LocalEvents.TryDequeue(out var eventData);

        @event = eventData;
        return @event is not null;
    }

    public void ClearLocalEvents()
    {
        LocalEvents.Clear();
    }
}
