using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbitrer.Messages;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arbitrer.Azure.Messaging.ServiceBus
{
  public class MessageDispatcher : IExternalMessageDispatcher, IDisposable
  {
    private readonly MessageDispatcherOptions _options;
    private readonly ILogger<MessageDispatcher> _logger;
    private ServiceBusAdministrationClient _adminclient;

    private QueueProperties _replyqueue;

    private ServiceBusClient _client;
    private ServiceBusSender _sender;


    public MessageDispatcher(IOptions<MessageDispatcherOptions> options, ILogger<MessageDispatcher> logger)
    {
      this._options = options.Value;
      this._logger = logger;
    }

    private async void InitConnection()
    {
      if (_adminclient == null)
      {
        this._adminclient = new global::Azure.Messaging.ServiceBus.Administration.ServiceBusAdministrationClient(this._options.ConnectionString);
        TopicProperties topic = null;
        if (!await this._adminclient.TopicExistsAsync(Consts.ArbitrerTopicName))
        {
          topic = await this._adminclient.CreateTopicAsync(Consts.ArbitrerTopicName);
        }

        var queueName = $"{_options.QueueName}.{Process.GetCurrentProcess().Id}.{DateTime.Now.Ticks}";
        this._replyqueue = await this._adminclient.CreateQueueAsync(new CreateQueueOptions(queueName)
        {
          AutoDeleteOnIdle = new TimeSpan(0, 0, 10)
        });
      }
      
      
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

    public Task<ResponseMessage<TResponse>> Dispatch<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse>
    {
      throw new NotImplementedException();
    }

    public Task Notify<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : INotification
    {
      throw new NotImplementedException();
    }

    public void Dispose()
    {
    }
  }
}