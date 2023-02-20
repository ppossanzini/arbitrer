using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Arbitrer.Azure.Messaging.ServiceBus
{
  public class RequestsManager : IHostedService
  {
    private readonly ILogger<MessageDispatcher> _logger;
    private readonly IArbitrer _arbitrer;
    private readonly IServiceProvider _provider;

    private ServiceBusClient _client;
    private ServiceBusAdministrationClient _adminclient;
    private ServiceBusSender _sender;
    private ServiceBusSessionProcessor _receiver;
    
    private List<string> _subscriptions = new List<string>();
    private readonly SHA256 _hasher = SHA256.Create();

    private readonly MessageDispatcherOptions _options;

    public RequestsManager(IOptions<MessageDispatcherOptions> options, ILogger<MessageDispatcher> logger, IArbitrer arbitrer, IServiceProvider provider)
    {
      this._options = options.Value;
      this._logger = logger;
      this._arbitrer = arbitrer;
      this._provider = provider;
    }

    public async  Task StartAsync(CancellationToken cancellationToken)
    {
      if (_receiver == null)
      {
        _logger.LogInformation($"ARBITRER: Creating ServiceBus connection...");
        _client = new ServiceBusClient(_options.ConnectionString);
        _adminclient = new ServiceBusAdministrationClient(_options.ConnectionString);
        _logger.LogInformation($"ARBITRER: connected !");
      }

      var notificationQueue =  $"{this._options.QueueName}.Notifications";
      var messageQueue = $"{this._options.QueueName}.{Process.GetCurrentProcess().Id}.{DateTime.Now.Ticks}";
      

      _logger.LogInformation($"ARBITRER: registering processors");
      foreach (var t in _arbitrer.GetLocalRequestsTypes())
      {
        var isNotification = typeof(INotification).IsAssignableFrom((t));
        if (!await _adminclient.QueueExistsAsync(t.TypeQueueName(), cancellationToken))
        {
          _adminclient.CreateQueueAsync(new CreateQueueOptions(){ EnablePartitioning = })
        }
        var queuename = $"{t.TypeQueueName()}${(isNotification ? Guid.NewGuid().ToString() : "")}";



        var p = _client.CreateProcessor(Consts.ArbitrerTopicName, queuename);
        p.
        
        _channel.QueueDeclare(queue: queuename, durable: _options.Durable, exclusive: isNotification, autoDelete: _options.AutoDelete, arguments: null);
        _channel.QueueBind(queuename, Consts.ArbitrerExchangeName, t.TypeQueueName());


        var consumer = new AsyncEventingBasicConsumer(_channel);

        var consumermethod = typeof(RequestsManager)
          .GetMethod(isNotification ? "ConsumeChannelNotification" : "ConsumeChannelMessage", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(t);

        consumer.Received += async (s, ea) => { await (Task) consumermethod.Invoke(this, new object[] {s, ea}); };
        _channel.BasicConsume(queue: queuename, autoAck: isNotification, consumer: consumer);
      }

      _channel.BasicQos(0, 1, false);
      _logger.LogInformation($"ARBITRER: ready!");
      return Task.CompletedTask;
    }

    private async Task ConsumeChannelNotification<T>(object sender, BasicDeliverEventArgs ea)
    {
      var msg = ea.Body.ToArray();

      if (_options.DeDuplicationEnabled)
      {
        var hash = msg.GetHash(_hasher);
        if (_deduplicationcache.Contains(hash))
        {
          _logger.LogDebug($"duplicated message received : {ea.Exchange}/{ea.RoutingKey}");
          return;
        }

        lock (_deduplicationcache)
          _deduplicationcache.Add(hash);

        // Do not await this task
        Task.Run(async () =>
        {
          await Task.Delay(_options.DeDuplicationTTL);
          lock (_deduplicationcache)
            _deduplicationcache.Remove(hash);
        });
      }

      _logger.LogDebug("Elaborating notification : {0}", Encoding.UTF8.GetString(msg));
      var message = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(msg), _options.SerializerSettings);

      var replyProps = _channel.CreateBasicProperties();
      replyProps.CorrelationId = ea.BasicProperties.CorrelationId;

      var mediator = _provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
      try
      {
        var arbitrer = mediator as ArbitredMediatr;
        arbitrer?.StopPropagating();
        await mediator.Publish(message);
        arbitrer?.ResetPropagating();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error executing message of type {typeof(T)} from external service");
      }
      finally
      {
      }
    }

    private async Task ConsumeChannelMessage<T>(object sender, BasicDeliverEventArgs ea)
    {
      var msg = ea.Body.ToArray();
      _logger.LogDebug("Elaborating message : {0}", Encoding.UTF8.GetString(msg));
      var message = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(msg), _options.SerializerSettings);

      var replyProps = _channel.CreateBasicProperties();
      replyProps.CorrelationId = ea.BasicProperties.CorrelationId;

      var mediator = _provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
      string responseMsg = null;
      try
      {
        var response = await mediator.Send(message);
        responseMsg = JsonConvert.SerializeObject(new Messages.ResponseMessage {Content = response, Status = Messages.StatusEnum.Ok}, _options.SerializerSettings);
        _logger.LogDebug("Elaborating sending response : {0}", responseMsg);
      }
      catch (Exception ex)
      {
        responseMsg = JsonConvert.SerializeObject(new Messages.ResponseMessage {Exception = ex, Status = Messages.StatusEnum.Exception}, _options.SerializerSettings);
        _logger.LogError(ex, $"Error executing message of type {typeof(T)} from external service");
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