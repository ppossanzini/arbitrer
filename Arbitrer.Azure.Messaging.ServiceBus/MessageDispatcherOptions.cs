using System;
using Newtonsoft.Json;

namespace Arbitrer.Azure.Messaging.ServiceBus
{
  public class MessageDispatcherOptions
  {
    public string ConnectionString { get; set; }
    public string QueueName { get; set; } = String.Empty;

    public JsonSerializerSettings SerializerSettings { get; set; }

    public MessageDispatcherOptions()
    {
      SerializerSettings = new JsonSerializerSettings()
      {
        MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore,
        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
        DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
        DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc
      };
      SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
    }
  }
}