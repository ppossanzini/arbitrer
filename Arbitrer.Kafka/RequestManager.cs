using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Arbitrer.Kafka
{
  public class RequestsManager : IHostedService
  {
    private readonly ILogger<MessageDispatcher> _logger;
    private readonly MessageDispatcherOptions _options;
    private IProducer<Null, string> _producer;
    private IConsumer<Null, string> _consumer;
    private MessageDispatcher _messageDispatcher;
    private string _topicName = null;

    public RequestsManager(ILogger<MessageDispatcher> logger, IOptions<MessageDispatcherOptions> options)
    {
      _logger = logger;
      _options = options.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      //throw new NotImplementedException();
      _topicName = $"{Process.GetCurrentProcess().Id}.{DateTime.Now.Ticks}";
      _options.GroupdId = _topicName;
      _producer = new ProducerBuilder<Null, string>(this._options.Producer).Build();
      _consumer = new ConsumerBuilder<Null, string>(this._options.Consumer).Build();
      _logger.LogInformation($"Creating Kafka Connection to '{_options.Producer?.BootstrapServers}'...'{_options.Consumer.BootstrapServers}'");
      Task.Run(() =>
      {
        _messageDispatcher = new MessageDispatcher(_producer, _consumer, _topicName, _logger);
      }, cancellationToken);
      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      //throw new NotImplementedException();
      Task.Run(() => {
        try { _messageDispatcher.Dispose(); }
        catch{}
      }, cancellationToken);
      return Task.CompletedTask;
    }
  }
}
