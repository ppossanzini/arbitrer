using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka.Admin;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Arbitrer.Kafka
{
  public class RequestsManager : IHostedService
  {
    private readonly ILogger<RequestsManager> _logger;
    private readonly IArbitrer _arbitrer;
    private readonly IServiceProvider _provider;

    private readonly MessageDispatcherOptions _options;
    private IProducer<Null, string> _producer;
    private IConsumer<Null, string> _requestConsumer;
    private IConsumer<Null, string> _notificationConsumer;

    private Thread _notificationConsumerThread;
    private Thread _requestConsumerThread;

    private Dictionary<string, MethodInfo> _methods = new Dictionary<string, MethodInfo>();

    public RequestsManager(ILogger<RequestsManager> logger, IOptions<MessageDispatcherOptions> options, IArbitrer arbitrer, IServiceProvider provider)
    {
      _logger = logger;
      _options = options.Value;
      this._arbitrer = arbitrer;
      this._provider = provider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _producer = new ProducerBuilder<Null, string>(this._options.GetProducerConfig()).Build();
      _logger.LogInformation($"Creating Kafka Connection to '{_options.BootstrapServers}'...");

      {
        var config = this._options.GetConsumerConfig();
        config.GroupId = "Arbitrer";
        _requestConsumer = new ConsumerBuilder<Null, string>(config).Build();
      }

      {
        var config = this._options.GetConsumerConfig();
        config.GroupId = $"Notification.{Process.GetCurrentProcess().Id}.{DateTime.Now.Ticks}";
        _notificationConsumer = new ConsumerBuilder<Null, string>(config).Build();
      }

      var notificationsSubscriptions = new List<string>();
      var requestSubscriptions = new List<string>();

      foreach (var t in _arbitrer.GetLocalRequestsTypes())
      {
        _provider.CreateTopicAsync(_options, t.TypeQueueName());
        var isNotification = typeof(INotification).IsAssignableFrom(t);

        if (isNotification)
        {
          notificationsSubscriptions.Add(t.TypeQueueName());
          var consumermethod = typeof(RequestsManager)
            .GetMethod("ConsumeChannelNotification", BindingFlags.Instance | BindingFlags.NonPublic)
            !.MakeGenericMethod(t);
          _methods.Add(t.TypeQueueName(), consumermethod);
        }
        else
        {
          requestSubscriptions.Add(t.TypeQueueName());
          var consumermethod = typeof(RequestsManager)
            .GetMethod("ConsumeChannelMessage", BindingFlags.Instance | BindingFlags.NonPublic)
            !.MakeGenericMethod(t);
          _methods.Add(t.TypeQueueName(), consumermethod);
        }
      }
      
      _requestConsumer.Subscribe(requestSubscriptions);
      _notificationConsumer.Subscribe(notificationsSubscriptions);
      
      _requestConsumerThread = new Thread(() =>
      {
        while (true)
        {
          var notification = _requestConsumer.Consume();
          _methods.TryGetValue(notification.Topic, out MethodInfo method);
          method!.Invoke(this, new object[] {notification.Message.Value});
        }
      }) {IsBackground = true};
      _requestConsumerThread.Start();

      _notificationConsumerThread = new Thread(() =>
      {
        while (true)
        {
          var notification = _notificationConsumer.Consume();
          _methods.TryGetValue(notification.Topic, out MethodInfo method);
          method!.Invoke(this, new object[] {notification.Message.Value});
        }
      }) {IsBackground = true};
      
      _notificationConsumerThread.Start();


      return Task.CompletedTask;
    }

    private async Task ConsumeChannelNotification<T>(string msg)
    {
      _logger.LogDebug("Elaborating notification : {Msg}", msg);
      var message = JsonConvert.DeserializeObject<KafkaMessage<T>>(msg, _options.SerializerSettings);
      if (message == null)
      {
        _logger.LogError("Unable to deserialize message {Msg}", msg);
        return;
      }

      var mediator = _provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
      try
      {
        var arbitrer = mediator as ArbitredMediatr;
        arbitrer?.StopPropagating();
        await mediator.Publish(message.Message);
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

    private async Task ConsumeChannelMessage<T>(string msg)
    {
      _logger.LogDebug("Elaborating message : {Msg}", msg);
      var message = JsonConvert.DeserializeObject<KafkaMessage<T>>(msg, _options.SerializerSettings);

      if (message == null)
      {
        _logger.LogError("Unable to deserialize message {Msg}", msg);
        return;
      }

      var mediator = _provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
      string responseMsg = null;
      try
      {
        var response = await mediator.Send(message.Message);
        responseMsg = JsonConvert.SerializeObject(
          new KafkaReply
          {
            Reply = new Messages.ResponseMessage {Content = response, Status = Messages.StatusEnum.Ok},
            CorrelationId = message.CorrelationId
          }, _options.SerializerSettings);
        _logger.LogDebug("Elaborating sending response : {Msg}", responseMsg);
      }
      catch (Exception ex)
      {
        responseMsg = JsonConvert.SerializeObject(
          new KafkaReply()
          {
            Reply = new Messages.ResponseMessage {Exception = ex, Status = Messages.StatusEnum.Exception},
            CorrelationId = message.CorrelationId
          }
          , _options.SerializerSettings);
        _logger.LogError(ex, $"Error executing message of type {typeof(T)} from external service");
      }
      finally
      {
        await _producer.ProduceAsync(message.ReplyTo, new Message<Null, string>() {Value = responseMsg});
      }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      return Task.CompletedTask;
    }
  }
}