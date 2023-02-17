using System;
using System.Security.Cryptography;
using System.Text;
using Arbitrer.Kafka;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

    public static void CreateTopicAsync(this IServiceProvider services, MessageDispatcherOptions options, string topicName, short replicationFactor = 1)
    {
      using (var adminClient = new AdminClientBuilder(options.GetAdminConfig()).Build())
        try
        {
          adminClient.CreateTopicsAsync(new TopicSpecification[]
          {
            new TopicSpecification
            {
              Name = topicName,
              ReplicationFactor = replicationFactor,
              NumPartitions = options.TopicPartition ?? 1
            }
          });
        }
        catch (CreateTopicsException e)
        {
          var logger = services.GetService<ILogger<MessageDispatcher>>();
          logger?.LogError($"An error occurred creating topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
        }
    }
  }
}