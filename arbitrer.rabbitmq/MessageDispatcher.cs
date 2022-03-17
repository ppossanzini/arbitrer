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

namespace Arbitrer.RabbitMQ
{

  public class MessageDispatcher : IExternalMessageDispatcher, IDisposable
  {

    private readonly MessageDispatcherOptions options;
    private readonly ILogger<MessageDispatcher> logger;
    private IConnection _connection = null;
    private IModel _channel = null;
    private string _replyQueueName = null;
    private EventingBasicConsumer _consumer = null;
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
        logger.LogInformation($"Creating RabbitMQ Conection to '{options.HostName}'...");
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

      _channel = _connection.CreateModel();
      _channel.ExchangeDeclare("rpc_exchange", ExchangeType.Topic);

      _replyQueueName = _channel.QueueDeclare(queue: options.QueueName).QueueName;
      _consumer = new EventingBasicConsumer(_channel);
      _consumer.Received += (s, ea) =>
      {
        if (!_callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out TaskCompletionSource<string> tcs))
          return;
        var body = ea.Body.ToArray();
        var response = Encoding.UTF8.GetString(body);
        tcs.TrySetResult(response);
      };

      this._consumerId = _channel.BasicConsume(queue: _replyQueueName, autoAck: true, consumer: _consumer);
    }


    public async Task<TResponse> Dispatch<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse>
    {
      var message = JsonConvert.SerializeObject(request, options.SerializerSettings);

      var correlationId = Guid.NewGuid().ToString();

      var tcs = new TaskCompletionSource<string>();
      _callbackMapper.TryAdd(correlationId, tcs);

      _channel.BasicPublish(
        exchange: "rpc_exchange",
        routingKey: typeof(TRequest).FullName.Replace(".", "_"),
        body: Encoding.UTF8.GetBytes(message),
        basicProperties: GetBasicProperties(correlationId));

      cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out var tmp));
      var result = await tcs.Task;
      return JsonConvert.DeserializeObject<TResponse>(result, options.SerializerSettings);

    }

    private IBasicProperties GetBasicProperties(string correlationId)
    {
      var props = _channel.CreateBasicProperties();
      props.CorrelationId = correlationId;
      props.ReplyTo = _replyQueueName;
      return props;
    }

    public void Dispose()
    {
      try
      {
        _channel?.BasicCancel(_consumerId);
        _channel.Close();
        _connection.Close();
      }
      catch { }
    }
  }

}