using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Soda.EventBus.Infrastructure;

namespace Soda.EventBus.Local.Core;

public interface ILocalEventBusManager<in TEvent> where TEvent : IEvent
{
    void Publish(TEvent @event);
    Task PublishAsync(TEvent @event);

    void AutoHandle();
}

public class LocalEventBusManager<TEvent>(IServiceProvider serviceProvider) : ILocalEventBusManager<TEvent>
    where TEvent : IEvent
{
    readonly IServiceProvider _servicesProvider = serviceProvider;

    private readonly Channel<TEvent> _eventChannel = Channel.CreateUnbounded<TEvent>();

    public void Publish(TEvent @event)
    {
        Debug.Assert(_eventChannel != null, nameof(_eventChannel) + " != null");
        _eventChannel.Writer.WriteAsync(@event);
    }

    private CancellationTokenSource Cts { get; } = new();

    public void Cancel()
    {
        Cts.Cancel();
    }

    public async Task PublishAsync(TEvent @event)
    {
        await _eventChannel.Writer.WriteAsync(@event);
    }

    public void AutoHandle()
    {
        // 确保只启动一次
        if (!Cts.IsCancellationRequested) return;

        Task.Run(async () =>
        {
            while (!Cts.IsCancellationRequested)
            {
                var reader = await _eventChannel.Reader.ReadAsync();
                await HandleAsync(reader);
            }
        }, Cts.Token);
    }

    async Task HandleAsync(TEvent @event)
    {
        var handler = _servicesProvider.GetService<IAsyncEventHandler<TEvent>>();

        if (handler is null)
        {
            throw new NullReferenceException($"No handler for event {@event.GetType().Name}");
        }
        try
        {
            await handler.HandleAsync(@event);
        }
        catch (Exception ex)
        {
            handler.HandleException(@event, ex);
        }
    }
}