using Microsoft.Extensions.DependencyInjection;

namespace Soda.EventBus;

public static class EventBusExtensions
{
    public static void AddEventBus(this IServiceCollection services, Action<EventBusOptions>? configure = null)
    {
        var options = new EventBusOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

    }
}

public class EventBusOptions
{
    public EventBusOptions()
    {

    }

}