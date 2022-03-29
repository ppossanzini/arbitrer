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
using System.Diagnostics;

namespace Arbitrer.RabbitMQ
{

  public class MessageDispatcher : IExternalMessageDispatcher, IDisposable
  {

    private readonly MessageDispatcherOptions options;
    private readonly ILogger<MessageDispatcher> logger;
    private IConnection _connection = null;
    private IModel _channel = null;
    private string _replyQueueName = null;
    private AsyncEventingBasicConsumer _consumer = null;
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
      _channel.ExchangeDeclare(Consts.ArbitrerExchangeName, ExchangeType.Topic);

      var queueName = $"{options.QueueName}.{Process.GetCurrentProcess().Id}.{DateTime.Now.Ticks}";
      _replyQueueName = _channel.QueueDeclare(queue: queueName).QueueName;
      _consumer = new AsyncEventingBasicConsumer(_channel);
      _consumer.Received += async (s, ea) =>
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
        exchange: Consts.ArbitrerExchangeName,
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