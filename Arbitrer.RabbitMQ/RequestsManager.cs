using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Arbitrer.RabbitMQ
{
  public class RequestsManager : IHostedService
  {
    private readonly ILogger<RequestsManager> logger;
    private readonly IArbitrer arbitrer;
    private readonly IServiceProvider provider;

    private IConnection _connection = null;
    private IModel _channel = null;
    private List<string> _subscriptions = new List<string>();
    private HashSet<string> _deduplicationcache = new HashSet<string>();    
    private readonly SHA256 _hasher = SHA256.Create();

    private readonly MessageDispatcherOptions options;

    public RequestsManager(IOptions<MessageDispatcherOptions> options, ILogger<RequestsManager> logger, IArbitrer arbitrer, IServiceProvider provider)
    {
      this.options = options.Value;
      this.logger = logger;
      this.arbitrer = arbitrer;
      this.provider = provider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      if (_connection == null)
      {
        logger.LogInformation($"ARBITRER: Creating RabbitMQ Conection to '{options.HostName}'...");
        var factory = new ConnectionFactory
        {
          HostName = options.HostName,
          UserName = options.UserName,
          Password = options.Password,
          VirtualHost = options.VirtualHost,
          Port = options.Port,
          DispatchConsumersAsync = true,
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(Consts.ArbitrerExchangeName, ExchangeType.Topic);

        logger.LogInformation($"ARBITRER: ready !");
      }


      foreach (var t in arbitrer.GetLocalRequestsTypes())
      {
        var isNotification = typeof(INotification).IsAssignableFrom(t);
        var queuename = $"{t.TypeQueueName()}${(isNotification ? Guid.NewGuid().ToString() : "")}";

        _channel.QueueDeclare(queue: queuename, durable: options.Durable, exclusive: isNotification, autoDelete: options.AutoDelete, arguments: null);
        _channel.QueueBind(queuename, Consts.ArbitrerExchangeName, t.TypeQueueName());


        var consumer = new AsyncEventingBasicConsumer(_channel);

        var consumermethod = typeof(RequestsManager)
          .GetMethod(isNotification ? "ConsumeChannelNotification" : "ConsumeChannelMessage", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(t);

        consumer.Received += async (s, ea) =>
        {
         
          await (Task) consumermethod.Invoke(this, new object[] {s, ea});
        };
        _channel.BasicConsume(queue: queuename, autoAck: isNotification, consumer: consumer);
      }

      _channel.BasicQos(0, 1, false);
      return Task.CompletedTask;
    }

    private async Task ConsumeChannelNotification<T>(object sender, BasicDeliverEventArgs ea)
    {
      var msg = ea.Body.ToArray();

      if (options.DeDuplicationEnabled)
      {
        var hash = msg.GetHash(_hasher);
        if (_deduplicationcache.Contains(hash))
        {
          logger.LogDebug($"duplicated message received : {ea.Exchange}/{ea.RoutingKey}");
          return;
        }

        lock (_deduplicationcache)
          _deduplicationcache.Add(hash);

        // Do not await this task
        Task.Run(async () =>
        {
          await Task.Delay(options.DeDuplicationTTL);
          lock (_deduplicationcache)
            _deduplicationcache.Remove(hash);
          
        });
      }
      
      logger.LogDebug("Elaborating notification : {0}", Encoding.UTF8.GetString(msg));
      var message = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(msg), options.SerializerSettings);

      var replyProps = _channel.CreateBasicProperties();
      replyProps.CorrelationId = ea.BasicProperties.CorrelationId;

      var mediator = provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
      try
      {
        var arbitrer = mediator as ArbitredMediatr;
        arbitrer?.StopPropagating();
        await mediator.Publish(message);
        arbitrer?.ResetPropagating();
      }
      catch (Exception ex)
      {
        logger.LogError(ex, $"Error executing message of type {typeof(T)} from external service");
      }
      finally
      {
      }
    }

    private async Task ConsumeChannelMessage<T>(object sender, BasicDeliverEventArgs ea)
    {
      var msg = ea.Body.ToArray();
      logger.LogDebug("Elaborating message : {0}", Encoding.UTF8.GetString(msg));
      var message = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(msg), options.SerializerSettings);

      var replyProps = _channel.CreateBasicProperties();
      replyProps.CorrelationId = ea.BasicProperties.CorrelationId;

      var mediator = provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
      string responseMsg = null;
      try
      {
        var response = await mediator.Send(message);
        responseMsg = JsonConvert.SerializeObject(new Messages.ResponseMessage {Content = response, Status = Messages.StatusEnum.Ok}, options.SerializerSettings);
        logger.LogDebug("Elaborating sending response : {0}", responseMsg);
      }
      catch (Exception ex)
      {
        responseMsg = JsonConvert.SerializeObject(new Messages.ResponseMessage {Exception = ex, Status = Messages.StatusEnum.Exception}, options.SerializerSettings);
        logger.LogError(ex, $"Error executing message of type {typeof(T)} from external service");
      }
      finally
      {
        _channel.BasicPublish(exchange: "", routingKey: ea.BasicProperties.ReplyTo, basicProperties: replyProps, body: Encoding.UTF8.GetBytes(responseMsg ?? ""));
        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
      }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      foreach (var s in _subscriptions)
        _channel.BasicCancel(s);

      try
      {
        _channel?.Close();
      }
      catch
      {
      }

      try
      {
        _connection?.Close();
      }
      catch
      {
      }

      return Task.CompletedTask;
    }
  }
}