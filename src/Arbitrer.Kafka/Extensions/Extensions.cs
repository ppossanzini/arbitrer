using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Arbitrer.Kafka;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arbitrer
{
  public static class Extensions
  {
    /// <summary>
    /// Adds the Arbitrer Kafka Message Dispatcher to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The configuration action to configure the MessageDispatcherOptions.</param>
    /// <returns>The service collection with the Arbitrer Kafka Message Dispatcher added.</returns>
    public static IServiceCollection AddArbitrerKafkaMessageDispatcher(this IServiceCollection services, Action<MessageDispatcherOptions> config)
    {
      services.Configure<MessageDispatcherOptions>(config);
      services.AddSingleton<IExternalMessageDispatcher, MessageDispatcher>();
      return services;
    }

    /// <summary>
    /// Resolves arbitrer calls by adding the RequestsManager as a hosted service to the service collection.
    /// </summary>
    /// <param name="services">The service collection to resolve arbitrer calls for.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection ResolveArbitrerCalls(this IServiceCollection services)
    {
      services.AddHostedService<RequestsManager>();
      return services;
    }
    
    
    public static MessageDispatcherOptions DispatchOnlyTo(this MessageDispatcherOptions options,
      Func<IEnumerable<Assembly>> assemblySelect)
    {
      var types = (
        from a in assemblySelect()
        from t in a.GetTypes()
        where typeof(IBaseRequest).IsAssignableFrom(t)
        select t).AsEnumerable();

      foreach (var t in types)
        options.DispatchOnly.Add(t);

      return options;
    }

    public static MessageDispatcherOptions DispatchOnlyTo(this MessageDispatcherOptions options,
      Func<IEnumerable<Type>> typesSelect)
    {
      foreach (var type in typesSelect().Where(t => typeof(IBaseRequest).IsAssignableFrom(t)))
        options.DispatchOnly.Add(type);

      return options;
    }

    public static MessageDispatcherOptions DenyDispatchTo(this MessageDispatcherOptions options,
      Func<IEnumerable<Type>> typesSelect)
    {
      foreach (var type in typesSelect().Where(t => typeof(IBaseRequest).IsAssignableFrom(t)))
        options.DispatchOnly.Add(type);

      return options;
    }

    public static MessageDispatcherOptions DenyDispatchTo(this MessageDispatcherOptions options,
      Func<IEnumerable<Assembly>> assemblySelect)
    {
      var types = (
        from a in assemblySelect()
        from t in a.GetTypes()
        where typeof(IBaseRequest).IsAssignableFrom(t)
        select t).AsEnumerable();

      foreach (var t in types)
        options.DontDispatch.Add(t);

      return options;
    }

    /// Creates a Kafka topic asynchronously.
    /// @param services The service provider to retrieve additional services from.
    /// @param options The options used for configuring the message dispatcher.
    /// @param topicName The name of the topic to be created.
    /// @param replicationFactor The replication factor for the topic. Defaults to 1.
    /// @remarks
    /// This method creates a Kafka topic using the specified options. The topic name
    /// and replication factor are required parameters. If no replication factor is
    /// specified, it defaults to 1.
    /// @throws CreateTopicsException If an error occurs during topic creation.
    /// @see MessageDispatcherOptions
    /// @see System.IServiceProvider
    /// @see Confluent.Kafka.Admin.AdminClientBuilder
    /// @see Confluent.Kafka.Admin.TopicSpecification
    /// @see Microsoft.Extensions.Logging.ILogger
    /// /
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

    /// <summary>
    /// Deletes a topic asynchronously.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <param name="options">The message dispatcher options.</param>
    /// <param name="topicName">The name of the topic to delete.</param>
    public static async void DeleteTopicAsync(this IServiceProvider services, MessageDispatcherOptions options, string topicName)
    {
      using (var adminClient = new AdminClientBuilder(options.GetAdminConfig()).Build())
      {
        await adminClient.DeleteTopicsAsync(new string[] { topicName });
      }
    }
  }
}