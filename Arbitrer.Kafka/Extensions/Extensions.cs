using System;
using System.Security.Cryptography;
using System.Text;
using Arbitrer.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace Arbitrer
{
  public static class Extensions
  {
    public static IServiceCollection AddArbitrerKafkaMessageDispatcher(this IServiceCollection services, Action<MessageDispatcherOptions> config)
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