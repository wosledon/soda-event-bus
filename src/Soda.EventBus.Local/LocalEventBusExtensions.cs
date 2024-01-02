using Microsoft.Extensions.DependencyInjection;
using Soda.EventBus.Infrastructure;
using Soda.EventBus.Local.Core;

namespace Soda.EventBus.Local;

public static class LocalEventBusExtensions
{
    public static void AddLocalEventBus(this IServiceCollection services, Action<LocalEventBusOptions>? options = null)
    {
        var localEventBusOptions = new LocalEventBusOptions();
        options?.Invoke(localEventBusOptions);
        if (localEventBusOptions.Pool)
        {
            services.AddSingleton<LocalEventBusPool>();
        }
        services.AddSingleton<ILocalEventBus, LocalEventBus>();
        services.AddSingleton<IEventBus, LocalEventBus>();
    }
}