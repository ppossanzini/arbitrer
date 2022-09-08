using System;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace Arbitrer.Azure.Messaging.ServiceBus.Extensions
{
  public static class Extensions
  {
    public static IServiceCollection AddArbitrerAzureMessageServiceBus(this IServiceCollection services, Action<MessageDispatcherOptions> config)
    {
      services.Configure<MessageDispatcherOptions>(config);
      services.AddSingleton<IExternalMessageDispatcher, MessageDispatcher>();
      return services;
    }

    public static IServiceCollection ResolveArbitrerCalls(this IServiceCollection services)
    {
      services.AddHostedService<RequestsManager>();
      return services;
    }
  }
}