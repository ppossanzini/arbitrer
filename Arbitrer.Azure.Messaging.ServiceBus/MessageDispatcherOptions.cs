using System;

namespace Arbitrer.Azure.Messaging.ServiceBus
{
  public class MessageDispatcherOptions
  {
    public string ConnectionString { get; set; }
    public string QueueName { get; set; } = String.Empty;
  }
}