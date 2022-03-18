using System;
using Arbitrer.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;

namespace Arbitrer
{
  public static class Extensions
  {
    public static ArbitrerOptionsBuilder AddRabbitMQArbitrerMessageDispatcher(this ArbitrerOptionsBuilder builder, Action<MessageDispatcherOptions> config)
    {
      builder.Service.Configure<MessageDispatcherOptions>(config);
      return builder;
    }

    public static ArbitrerOptionsBuilder ResolveCallsFromExternalService(this ArbitrerOptionsBuilder builder)
    {
      builder.Service.AddHostedService<RequestsManager>();
      return builder;
    }


  }
}