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
    private readonly MessageDispatcherOptions _options;
    private readonly ILogger<MessageDispatcher> _logger;
    private readonly IServiceProvider _provider;
    private IProducer<Null, string> _producer;
    private IConsumer<Null, string> _consumer;
    private string _replyTopicName;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

    public MessageDispatcher(IOptions<MessageDispatcherOptions> options, ILogger<MessageDispatcher> logger, IServiceProvider provider)
    {
      this._options = options.Value;
      this._logger = logger;
      _provider = provider;
      this.InitConnection();
    }


    public void InitConnection()
    {
      _logger.LogInformation($"Creating Kafka Connection to '{_options.BootstrapServers}'...");
      
      // Ensuring we have a connection object
      
      _replyTopicName = $"{Process.GetCurrentProcess().Id}.{DateTime.Now.Ticks}";
      var config = this._options.GetConsumerConfig();
      config.GroupId = _replyTopicName;
      
      _producer = new ProducerBuilder<Null, string>(this._options.GetProducerConfig()).Build();
      _consumer = new ConsumerBuilder<Null, string>(config).Build();

      _provider.CreateTopicAsync(_options, _replyTopicName);
        
      _consumer.Subscribe(_replyTopicName);
      _consumerThread = new Thread(() =>
        {
          while (true)
          {
            var consumeResult = _consumer.Consume();
            if (consumeResult != null)
            {
              
              _logger.LogDebug("Response Message: {Msg}", consumeResult.Message.Value);
              var reply = JsonConvert.DeserializeObject<KafkaReply>(consumeResult.Message.Value, this._options.SerializerSettings);

              if (reply != null)
                if (_callbackMapper.TryRemove(reply.CorrelationId, out TaskCompletionSource<string> tcs))
                  tcs?.TrySetResult(consumeResult.Message.Value);
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
        CorrelationId = correlationId, 
        ReplyTo = _replyTopicName
      }, _options.SerializerSettings);


      var tcs = new TaskCompletionSource<string>();
      var rr = _callbackMapper.TryAdd(correlationId, tcs);

      await _producer.ProduceAsync(
        topic: typeof(TRequest).TypeQueueName(),
        message: new Message<Null, string> {Value = message}, cancellationToken);

      cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out var tmp));
      var result = await tcs.Task;

      var r = JsonConvert.DeserializeObject<KafkaReply<ResponseMessage<TResponse>>>(result, this._options.SerializerSettings);
      return r!.Reply;
    }

    public async Task Notify<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : INotification
    {
      var message = JsonConvert.SerializeObject(request, _options.SerializerSettings);

      _logger.LogInformation($"Sending message to: {Consts.ArbitrerExchangeName}/{request.GetType().TypeQueueName()}");

      await _producer.ProduceAsync(
        topic: typeof(TRequest).TypeQueueName(),
        message: new Message<Null, string> {Value = message}, cancellationToken);
    }

    public void Dispose()
    {
      try
      {
      }
      finally
      {
        try
        {
          _producer.Dispose();
        }
        catch
        {
        }

        try
        {
          DisposeConsumer();
        }
        catch
        {
        }
      }
    }

    private void DisposeConsumer()
    {
       _provider.DeleteTopicAsync(this._options, this._replyTopicName);
      _consumer.Unsubscribe();
      _consumer.Close();
      _consumer.Dispose();
    }
  }
}