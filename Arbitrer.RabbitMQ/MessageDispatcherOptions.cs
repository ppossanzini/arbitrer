using Newtonsoft.Json;

namespace Arbitrer.RabbitMQ
{
  public class MessageDispatcherOptions
  {
    public string HostName { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string VirtualHost { get; set; }
    public int Port { get; set; }
    public string QueueName { get; set; }

    public bool AutoDelete = false;
    public bool Durable = true;

    public int DeDuplicationTTL { get; set; } = 5000;
    public bool DeDuplicationEnabled { get; set; } = true;

    internal bool EnableServiceWorker { get; set; } = false;

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