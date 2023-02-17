using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using Confluent.Kafka;
using admin = Confluent.Kafka.Admin;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Arbitrer.Messages;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Reflection.Metadata.Ecma335;

namespace Arbitrer.Kafka
{
  public class MessageDispatcher : IExternalMessageDispatcher, IDisposable
  {
    private Thread _consumerThread;
    private readonly MessageDispatcherOptions options;
    private readonly ILogger<MessageDispatcher> _logger;
    private IProducer<Null, string> _producer;
    private IConsumer<Null, string> _consumer;
    private string _replyTopicName;
    private string _topicName = null;
    private bool _isNotification = false;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _callbackMapper = new ConcurrentDictionary<string, TaskCompletionSource<object>>();

    public MessageDispatcher(IOptions<MessageDispatcherOptions> options, ILogger<MessageDispatcher> logger)
    {
      this.options = options.Value;
      this._logger = logger;

      //this.InitConnection(_consumer);
    }

    public MessageDispatcher(
      IProducer<Null, string> producer, 
      IConsumer<Null, string> consumer,
      string topicName,
      ILogger<MessageDispatcher> logger
      )
    {
      this._logger = logger;
      this._producer = producer;
      this._consumer = consumer;
      _topicName = topicName;
      this.InitConnection();
    }

    public void InitConnection()
    {
      // Ensuring we have a connection object
      //_producer = new ProducerBuilder<Null, string>(this.options.Producer).Build();
      //_consumer = new ConsumerBuilder<Null, string>(this.options.Consumer).Build();

      _logger.LogInformation($"Creating Kafka Connection to '{options.Producer?.BootstrapServers}'...'{options.Consumer.BootstrapServers}'");

      //_topicName = $"{Process.GetCurrentProcess().Id}.{DateTime.Now.Ticks}";
      
      if ( _isNotification ) { 
        this.options.GroupdId = $"{_topicName}.{Guid.NewGuid}";
        
        try { DisposeConsumer(); }
        catch{}

        _consumer = new ConsumerBuilder<Null, string>(this.options.Consumer).Build();
      }
      _consumer.Subscribe(_topicName);
      _consumerThread = new Thread(() =>
      {
        while (true)
        {
          var consumeResult = _consumer.Consume();
          if (consumeResult != null)
          {
            var reply = JsonConvert.DeserializeObject<KafkaReply>(consumeResult.Message.Value, this.options.SerializerSettings);

            if (!_callbackMapper.TryRemove(reply.CorrelationId, out TaskCompletionSource<object> tcs))
              tcs.TrySetResult(reply.Reply);
          } 
        }
      }
      );
      _consumerThread.IsBackground = true;
      _consumerThread.Start();

    }

    public async Task<Messages.ResponseMessage<TResponse>> Dispatch<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
      where TRequest : IRequest<TResponse>
    {
      var correlationId = Guid.NewGuid().ToString();
      var message = JsonConvert.SerializeObject(new KafkaMessage<TRequest>
      {
        Message = request,
        CorrelationId = correlationId
      }, options.SerializerSettings);


      var tcs = new TaskCompletionSource<object>();
      _callbackMapper.TryAdd(correlationId, tcs);

      await _producer.ProduceAsync(
        topic: _topicName, //typeof(TRequest).TypeQueueName(),
        message: new Message<Null, string> { Value = message }, cancellationToken);

      //this.options.Consumer.GroupId = _topicName;

      cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out var tmp));
      var result = await tcs.Task;

      return result as Messages.ResponseMessage<TResponse>;
    }

    public async Task Notify<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : INotification
    {
      _isNotification = true;
      var message = JsonConvert.SerializeObject(request, options.SerializerSettings);

      _logger.LogInformation($"Sending message to: {Consts.ArbitrerExchangeName}/{request.GetType().TypeQueueName()}");

      await _producer.ProduceAsync(
        topic: _topicName, //typeof(TRequest).TypeQueueName(),
        message: new Message<Null, string> { Value = message }, cancellationToken);

      //this.options.Consumer.GroupId = $"{_topicName}.{Guid.NewGuid()}";
    }

    public void Dispose()
    {
      try { _producer.Dispose(); }
      catch {}

      try { DisposeConsumer(); }
      catch {}
    }
    public void DisposeConsumer()
    {
      _consumer.Unsubscribe();
      _consumer.Close();
      _consumer.Dispose();
    }
  }
}