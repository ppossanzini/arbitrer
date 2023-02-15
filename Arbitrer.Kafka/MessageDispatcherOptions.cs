using System.Security.Cryptography.X509Certificates;
using Confluent.Kafka;
using Newtonsoft.Json;

namespace Arbitrer.Kafka
{
  public class MessageDispatcherOptions
  {
    public string BootstrapServer { get; set; }
    // public string UserName { get; set; }
    // public string Password { get; set; }
    // public string VirtualHost { get; set; }
    // public int Port { get; set; }
    // public string QueueName { get; set; }

    // public bool AutoDelete = false;
    // public bool Durable = true;

    public JsonSerializerSettings SerializerSettings { get; set; }
    public ProducerConfig Producer { get; set; }
    public ConsumerConfig Consumer { get; set; }

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