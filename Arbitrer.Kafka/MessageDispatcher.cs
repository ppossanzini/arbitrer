using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using Confluent.Kafka;


namespace Arbitrer.Kafka
{
  public class MessageDispatcher : IExternalMessageDispatcher, IDisposable
  {
    private readonly MessageDispatcherOptions options;
    private readonly ILogger<MessageDispatcher> logger;
    private IProducer<Null, string> _producer;
    private string _replyQueueName = null;
    private IConsumer<Null, string> _consumer;
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
      // Ensuring we have a connection object
      _producer = new ProducerBuilder<Null, string>(this.options.Producer).Build();
      _consumer = new ConsumerBuilder<Null, string>(this.options.Consumer).Build();

      logger.LogInformation($"Creating Kafka Connection to '{options.Producer?.BootstrapServers}'...'{options.Consumer.BootstrapServers}'");

      _consumer.Subscribe();

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

      await _producer.ProduceAsync(
        topic: typeof(TRequest).TypeQueueName(),
        message: new Message<Null, string> {Value = message}, cancellationToken);

      cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out var tmp));
      var result = await tcs.Task;

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