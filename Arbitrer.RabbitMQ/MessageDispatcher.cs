using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Threading;
using System.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using Arbitrer.Messages;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Arbitrer.RabbitMQ
{
  public class MessageDispatcher : IExternalMessageDispatcher, IDisposable
  {
    private readonly MessageDispatcherOptions options;
    private readonly ILogger<MessageDispatcher> logger;
    private IConnection _connection = null;
    private IModel _sendChannel = null;
    private string _replyQueueName = null;
    private AsyncEventingBasicConsumer _sendConsumer = null;
    private string _consumerId = null;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper = new ConcurrentDictionary<string, TaskCompletionSource<string>>();


    public MessageDispatcher(IOptions<MessageDispatcherOptions> options, ILogger<MessageDispatcher> logger)
    {
      this.options = options.Value;
      this.logger = logger;

      this.InitConnection();
    }

    private void InitConnection()
    {
      // Ensuring we have a connetion object
      if (_connection == null)
      {
        logger.LogInformation($"Creating RabbitMQ Connection to '{options.HostName}'...");
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
      }

      _sendChannel = _connection.CreateModel();
      _sendChannel.ExchangeDeclare(Consts.ArbitrerExchangeName, ExchangeType.Topic);
      // _channel.ConfirmSelect();

      var queueName = $"{options.QueueName}.{Process.GetCurrentProcess().Id}.{DateTime.Now.Ticks}";
      _replyQueueName = _sendChannel.QueueDeclare(queue: queueName).QueueName;
      _sendConsumer = new AsyncEventingBasicConsumer(_sendChannel);
      _sendConsumer.Received += (s, ea) =>
      {
        if (!_callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out TaskCompletionSource<string> tcs))
          return Task.CompletedTask;
        var body = ea.Body.ToArray();
        var response = Encoding.UTF8.GetString(body);
        tcs.TrySetResult(response);
        return Task.CompletedTask;
      };

      _sendChannel.BasicReturn += (s, ea) =>
      {
        if (!_callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out TaskCompletionSource<string> tcs)) return;
        tcs.TrySetException(new Exception($"Unable to deliver required action: {ea.RoutingKey}"));
      };

      this._consumerId = _sendChannel.BasicConsume(queue: _replyQueueName, autoAck: true, consumer: _sendConsumer);
    }


    public async Task<Messages.ResponseMessage<TResponse>> Dispatch<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
      where TRequest : IRequest<TResponse>
    {
      var message = JsonConvert.SerializeObject(request, options.SerializerSettings);

      var correlationId = Guid.NewGuid().ToString();

      var tcs = new TaskCompletionSource<string>();
      _callbackMapper.TryAdd(correlationId, tcs);

      _sendChannel.BasicPublish(
        exchange: Consts.ArbitrerExchangeName,
        routingKey: typeof(TRequest).TypeQueueName(),
        mandatory: true,
        body: Encoding.UTF8.GetBytes(message),
        basicProperties: GetBasicProperties(correlationId));


      cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out var tmp));
      var result = await tcs.Task;

      // if (typeof(TResponse).IsClass)
        return JsonConvert.DeserializeObject<Messages.ResponseMessage<TResponse>>(result, options.SerializerSettings);

      
    }

    public async Task Notify<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : INotification
    {
      var message = JsonConvert.SerializeObject(request, options.SerializerSettings);

      logger.LogInformation($"Sending message to: {Consts.ArbitrerExchangeName}/{request.GetType().TypeQueueName()}");

      _sendChannel.BasicPublish(
        exchange: Consts.ArbitrerExchangeName,
        routingKey: request.GetType().TypeQueueName(),
        mandatory: false,
        body: Encoding.UTF8.GetBytes(message)
      );
    }


    private IBasicProperties GetBasicProperties(string correlationId)
    {
      var props = _sendChannel.CreateBasicProperties();
      props.CorrelationId = correlationId;
      props.ReplyTo = _replyQueueName;
      return props;
    }

    public void Dispose()
    {
      try
      {
        _sendChannel?.BasicCancel(_consumerId);
        _sendChannel?.Close();
        _connection.Close();
      }
      catch
      {
      }
    }
  }
}