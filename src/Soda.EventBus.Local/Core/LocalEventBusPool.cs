using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Soda.EventBus.Infrastructure;

namespace Soda.EventBus.Local.Core;

public sealed class LocalEventBusPool(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private class ChannelKey
    {
        public required string Key { get; init; }
        public int Subscribers { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is ChannelKey key)
            {
                return string.Equals(key.Key, Key, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    private Channel<IEvent> Rent(string channel)
    {
        _channels.TryGetValue(new ChannelKey() { Key = channel }, out var value);

        if (value != null) return value;
        value = Channel.CreateUnbounded<IEvent>();
        _channels.TryAdd(new ChannelKey() { Key = channel }, value);
        return value;
    }

    private Channel<IEvent> Rent(ChannelKey channelKey)
    {
        _channels.TryGetValue(channelKey, out var value);
        if (value != null) return value;
        value = Channel.CreateUnbounded<IEvent>();
        _channels.TryAdd(channelKey, value);
        return value;
    }

    private readonly ConcurrentDictionary<ChannelKey, Channel<IEvent>> _channels = new();

    private CancellationTokenSource Cts { get; } = new();

    public void Cancel()
    {
        Cts.Cancel();
        _channels.Clear();
        Cts.TryReset();
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        await Rent(typeof(TEvent).Name).Writer.WriteAsync(@event);
    }

    public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
    {
        Rent(typeof(TEvent).Name).Writer.TryWrite(@event);
    }

    public void OnSubscribe<TEvent>() where TEvent : IEvent
    {
        var channelKey = _channels.FirstOrDefault(x => x.Key.Key == typeof(TEvent).Name).Key ??
                         new ChannelKey() { Key = typeof(TEvent).Name };
        channelKey.Subscribers++;

        Task.Run(async () =>
        {
            try
            {
                while (!Cts.IsCancellationRequested)
                {
                    var @event = await ReadAsync(channelKey);

                    var handler = _serviceProvider.GetService<IAsyncEventHandler<TEvent>>();
                    if (handler == null) throw new NullReferenceException($"No handler for Event {typeof(TEvent).Name}");
                    try
                    {
                        await handler.HandleAsync((TEvent)@event);
                    }
                    catch (Exception ex)
                    {
                        handler.HandleException((TEvent)@event, ex);
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Error on onSubscribe handler", e);
            }
        }, Cts.Token);
    }

    private async Task<IEvent> ReadAsync(string channel)
    {
        return await Rent(channel).Reader.ReadAsync(Cts.Token);
    }

    private async Task<IEvent> ReadAsync(ChannelKey channel)
    {
        return await Rent(channel).Reader.ReadAsync(Cts.Token);
    }
}