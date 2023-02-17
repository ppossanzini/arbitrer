using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbitrer.Messages;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using MediatR;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Arbitrer.Azure.Messaging.ServiceBus
{
  public class MessageDispatcher : IExternalMessageDispatcher, IDisposable
  {
    private readonly MessageDispatcherOptions options;
    private readonly ILogger<MessageDispatcher> _logger;
    private ServiceBusAdministrationClient _adminclient;

    private ServiceBusClient _client;
    private ServiceBusSender _sender;
    private ServiceBusSessionProcessor _receiver;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

    private readonly string ReplyQuename = null;

    public MessageDispatcher(IOptions<MessageDispatcherOptions> options, ILogger<MessageDispatcher> logger)
    {
      this.options = options.Value;
      this._logger = logger;
      this.ReplyQuename = $"{this.options.QueueName}.{Process.GetCurrentProcess().Id}.{DateTime.Now.Ticks}";
    }

    private async void InitConnection()
    {
      if (_adminclient == null)
      {
        this._adminclient = new global::Azure.Messaging.ServiceBus.Administration.ServiceBusAdministrationClient(this.options.ConnectionString);
        TopicProperties topic = null;
        if (!await this._adminclient.TopicExistsAsync(Consts.ArbitrerTopicName))
        {
          topic = await this._adminclient.CreateTopicAsync(Consts.ArbitrerTopicName);
        }

        await this._adminclient.CreateQueueAsync(new CreateQueueOptions(ReplyQuename)
        {
          AutoDeleteOnIdle = new TimeSpan(0, 0, 10),
          Name = ReplyQuename
        });
      }

      _client = new ServiceBusClient(options.ConnectionString);
      _sender = _client.CreateSender(Consts.ArbitrerTopicName);

      // messages for this receiver will are reply from pattern "Request/Reply"
      _receiver = _client.CreateSessionProcessor(ReplyQuename,
        new ServiceBusSessionProcessorOptions() {ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete});

      _receiver.ProcessMessageAsync += a =>
      {
        if (!_callbackMapper.TryRemove(a.SessionId, out TaskCompletionSource<string> tcs))
          return Task.CompletedTask;

        var body = a.Message.Body.ToArray();
        var response = Encoding.UTF8.GetString(body);
        tcs.TrySetResult(response);
        return Task.CompletedTask;
      };

      await _receiver.StartProcessingAsync();
      
    }

    public async Task<ResponseMessage<TResponse>> Dispatch<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
      where TRequest : IRequest<TResponse>
    {
      var message = JsonConvert.SerializeObject(request, options.SerializerSettings);
      var sessionId = Guid.NewGuid().ToString();

      var tcs = new TaskCompletionSource<string>();
      _callbackMapper.TryAdd(sessionId, tcs);

      await _sender.SendMessageAsync(new ServiceBusMessage(message)
      {
        PartitionKey = typeof(TRequest).TypeQueueName(),
        To = typeof(TRequest).TypeQueueName(),
        SessionId = sessionId,
        ReplyToSessionId = sessionId,
        ReplyTo = ReplyQuename
      }, cancellationToken);

      cancellationToken.Register(() => _callbackMapper.TryRemove(sessionId, out var tmp));
      var result = await tcs.Task;

      return JsonConvert.DeserializeObject<Messages.ResponseMessage<TResponse>>(result, options.SerializerSettings);
    }

    public async Task Notify<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : INotification
    {
      var message = JsonConvert.SerializeObject(request, options.SerializerSettings);

      _logger.LogInformation($"Sending message to: {Consts.ArbitrerTopicName}/{request.GetType().TypeQueueName()}");

      await _sender.SendMessageAsync(new ServiceBusMessage(message)
      {
        PartitionKey = typeof(TRequest).TypeQueueName(),
        To = typeof(TRequest).TypeQueueName(),
      }, cancellationToken);
    }

    public void Dispose()
    {
    }
  }
}