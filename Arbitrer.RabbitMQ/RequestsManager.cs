using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Arbitrer.RabbitMQ
{
  /// <summary>
  /// The RequestsManager class is responsible for managing requests and notifications in a distributed system. It implements the IHostedService interface.
  /// </summary>
  public class RequestsManager : IHostedService
  {
    private ArbitrerOptions _arbitrerOptions;

    /// <summary>
    /// Represents the logger for the RequestsManager class.
    /// </summary>
    /// <typeparam name="RequestsManager">The type of the class using the logger.</typeparam>
    private readonly ILogger<RequestsManager> _logger;

    /// <summary>
    /// The private readonly field that holds an instance of the IArbitrer interface.
    /// </summary>
    private readonly IArbitrer _arbitrer;

    /// <summary>
    /// Represents a service provider.
    /// </summary>
    private readonly IServiceProvider _provider;

    /// <summary>
    /// Represents a private instance of an IConnection object.
    /// </summary>
    private IConnection _connection = null;

    /// <summary>
    /// Represents the channel used for communication.
    /// </summary>
    private IChannel _channel = null;

    private Thread _watchguard = null;

    private Dictionary<Type, AsyncEventingBasicConsumer> _consumers = new Dictionary<Type, AsyncEventingBasicConsumer>();

    /// <summary>
    /// Represents a SHA256 hash algorithm instance used for hashing data.
    /// </summary>
    private readonly SHA256 _hasher = SHA256.Create();

    /// <summary>
    /// Represents the options for the message dispatcher.
    /// </summary>
    private readonly MessageDispatcherOptions _options;

    /// <summary>
    /// Constructs a new instance of the RequestsManager class.
    /// </summary>
    /// <param name="options">The options for the message dispatcher.</param>
    /// <param name="logger">The logger to be used for logging.</param>
    /// <param name="arbitrer">The object responsible for coordinating requests.</param>
    /// <param name="provider">The service provider for resolving dependencies.</param>
    public RequestsManager(IOptions<MessageDispatcherOptions> options, ILogger<RequestsManager> logger, IArbitrer arbitrer, IServiceProvider provider,
      IOptions<ArbitrerOptions> arbitrerOptions)
    {
      this._arbitrerOptions = arbitrerOptions.Value;
      this._options = options.Value;
      this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this._arbitrer = arbitrer;
      this._provider = provider;
    }

    /// <summary>
    /// Starts the asynchronous process of connecting to RabbitMQ and consuming messages from queues.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
      await CheckConnection(cancellationToken);

      await CheckRequestsConsumers(cancellationToken);

      await ValidateConnectionQos(cancellationToken);

      _watchguard = new Thread(() => Watchguard()) { IsBackground = true };
      _watchguard.Start();
    }

    private async void Watchguard()
    {
      await Task.Delay(TimeSpan.FromMinutes(2));
      while (true)
      {
        var toremove = new HashSet<Type>();

        foreach (var k in _consumers)
        {
          if (!k.Value.IsRunning)
          {
            _logger.LogError($"Stopping consumer for {k.Key}: The consumer is stopped for {k.Value.ShutdownReason?.Exception?.Message ?? "unknown reason"}");
            toremove.Add(k.Key);
          }
        }

        foreach (var t in toremove)
        {
          if (_consumers.ContainsKey(t))
            _consumers.Remove(t);
        }

        await CheckRequestsConsumers(CancellationToken.None);

        toremove.Clear();
        await Task.Delay(TimeSpan.FromMinutes(2));
      }
    }

    private async Task CheckRequestsConsumers(CancellationToken cancellationToken)
    {
      foreach (var t in _arbitrer.GetLocalRequestsTypes())
      {
        if (t is null) continue;
        var isNotification = t.IsNotification();
        var isDurableNotification = isNotification && _arbitrerOptions.QueueNames.ContainsKey(t);
        var queueName = t.ArbitrerQueueName(_arbitrerOptions);

        var arguments = new Dictionary<string, object>();
        var timeout = t.QueueTimeout();
        if (timeout != null)
        {
          arguments.Add("x-consumer-timeout", timeout);
        }


        await _channel.QueueDeclareAsync(queue: queueName, durable: _options.Durable,
          exclusive: isNotification && !isDurableNotification,
          autoDelete: _options.AutoDelete, arguments: arguments, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(queueName, Constants.ArbitrerExchangeName, t.ArbitrerTypeName(_arbitrerOptions), cancellationToken: cancellationToken);


        var consumer = new AsyncEventingBasicConsumer(_channel);
        _consumers.Add(t, consumer);

        var consumerMethod = typeof(RequestsManager)
          .GetMethod(isNotification ? nameof(ConsumeChannelNotification) : nameof(ConsumeChannelMessage), BindingFlags.Instance | BindingFlags.NonPublic)?
          .MakeGenericMethod(t);


        consumer.ReceivedAsync += async (s, ea) =>
        {
          try
          {
            if (consumerMethod != null)
              await (Task)consumerMethod.Invoke(this, new object[] { s, ea });
          }
          catch (Exception e)
          {
            _logger.LogError(e, e.Message);
          }
        };

        await _channel.BasicConsumeAsync(queue: queueName, autoAck: isNotification, consumer: consumer, cancellationToken: cancellationToken);
      }
    }

    private async Task ValidateConnectionQos(CancellationToken cancellationToken)
    {
      try
      {
        if (_options.PerChannelQos == 0)
        {
          var qos = _arbitrer.GetLocalRequestsTypes().Count();
          var maxMessages = qos * _options.PerConsumerQos > ushort.MaxValue ? ushort.MaxValue : (ushort)(qos * _options.PerConsumerQos);
          _logger.LogInformation($"Configuring Qos for channels with: prefetch = 0 and fetch size = {maxMessages}");
          await _channel.BasicQosAsync(0, maxMessages, true, cancellationToken: cancellationToken);
        }
        else
        {
          await _channel.BasicQosAsync(0, _options.PerChannelQos > ushort.MaxValue ? ushort.MaxValue : (ushort)_options.PerChannelQos, true,
            cancellationToken: cancellationToken);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError("Current RabbitMQ does not support Qos for channels");
        _logger.LogError(ex.Message);
        _logger.LogError(ex.StackTrace);
      }

      try
      {
        _logger.LogInformation($"Configuring Qos for consumers with: prefetch = 0 and fetch size = {Math.Max(_options.PerConsumerQos, (ushort)1)}");
        await _channel.BasicQosAsync(0, Math.Max(_options.PerConsumerQos, (ushort)1), false, cancellationToken: cancellationToken);
      }
      catch (Exception ex)
      {
        _logger.LogError("Current RabbitMQ does not support Qos for consumers");
        _logger.LogError(ex.Message);
        _logger.LogError(ex.StackTrace);
      }
    }

    private async Task CheckConnection(CancellationToken cancellationToken)
    {
      if (_connection != null && _connection.IsOpen)
      {
        _connection = null;
      }

      if (_connection == null)
      {
        _logger.LogInformation($"ARBITRER: Creating RabbitMQ Connection to '{_options.HostName}'...");
        var factory = new ConnectionFactory
        {
          HostName = _options.HostName,
          UserName = _options.UserName,
          Password = _options.Password,
          VirtualHost = _options.VirtualHost,
          Port = _options.Port,

          ClientProvidedName = _options.ClientName
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(Constants.ArbitrerExchangeName, ExchangeType.Topic, cancellationToken: cancellationToken);

        _logger.LogInformation("ARBITRER: ready !");
      }
    }


    /// <summary>
    /// ConsumeChannelNotification is a private asynchronous method that handles the consumption of channel notifications. </summary>
    /// <typeparam name="T">The type of messages to be consumed</typeparam> <param name="sender">The object that triggered the event</param> <param name="ea">The event arguments containing the consumed message</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    /// /
    private async Task ConsumeChannelNotification<T>(object sender, BasicDeliverEventArgs ea)
    {
      var mediator = _provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
      var arbitrer = mediator as ArbitredMediatr;
      try
      {
        var msg = ea.Body.ToArray();

        _logger.LogDebug("Elaborating notification : {0}", Encoding.UTF8.GetString(msg));
        var message = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(msg), _options.SerializerSettings);

        arbitrer?.StopPropagating();
        await mediator.Publish(message);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error executing message of type {typeof(T)} from external service");
      }
      finally
      {
        arbitrer?.ResetPropagating();
      }
    }

    /// <summary>
    /// Consumes a message from a channel and processes it asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the message being consumed.</typeparam>
    /// <param name="sender">The object that raised the event.</param>
    /// <param name="ea">An object that contains the event data.</param>
    /// <returns>A task representing the asynchronous processing of the message.</returns>
    /// <remarks>
    /// This method deserializes the message using the specified <c>DeserializerSettings</c>,
    /// sends it to the mediator for processing, and publishes a response message to the
    /// specified reply-to queue. If an exception occurs during processing, an error response
    /// message will be published.
    /// </remarks>
    private async Task ConsumeChannelMessage<T>(object sender, BasicDeliverEventArgs ea)
    {
      string responseMsg = null;
      var replyProps = new BasicProperties();
      try
      {
        replyProps.CorrelationId = ea.BasicProperties.CorrelationId;

        var msg = ea.Body.ToArray();
        _logger.LogDebug("Elaborating message : {0}", Encoding.UTF8.GetString(msg));
        var message = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(msg), _options.SerializerSettings);

        var mediator = _provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(message);
        responseMsg = JsonConvert.SerializeObject(new Messages.ResponseMessage { Content = response, Status = Messages.StatusEnum.Ok },
          _options.SerializerSettings);
        _logger.LogDebug("Elaborating sending response : {0}", responseMsg);
      }
      catch (Exception ex)
      {
        responseMsg = JsonConvert.SerializeObject(new Messages.ResponseMessage { Exception = ex, Status = Messages.StatusEnum.Exception },
          _options.SerializerSettings);
        _logger.LogError(ex, $"Error executing message of type {typeof(T)} from external service");
      }
      finally
      {
        if (!string.IsNullOrWhiteSpace(ea.BasicProperties.ReplyTo))
          await _channel.BasicPublishAsync(exchange: "", routingKey: ea.BasicProperties.ReplyTo, basicProperties: replyProps,
            body: Encoding.UTF8.GetBytes(responseMsg ?? ""), mandatory: true);
        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
      }
    }

    /// <summary>
    /// Stops the asynchronous operation and closes the channel and connection.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
      try
      {
        _channel?.CloseAsync(cancellationToken);
      }
      catch
      {
      }

      try
      {
        _connection?.CloseAsync(cancellationToken);
      }
      catch
      {
      }
    }
  }
}