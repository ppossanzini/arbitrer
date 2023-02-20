using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using Confluent.Kafka;
using Newtonsoft.Json;

namespace Arbitrer.Kafka
{
  public class MessageDispatcherOptions
  {
    public int? TopicPartition { get; set; }
    public string BootstrapServers { get; set; }
    public Offset Offset { get; set; } = Offset.End;
      
    public JsonSerializerSettings SerializerSettings { get; set; }

    public AdminClientConfig GetAdminConfig()
    {
      return new AdminClientConfig()
      {
        BootstrapServers = this.BootstrapServers,
      };
    }

    public ProducerConfig GetProducerConfig()
    {
      return new ProducerConfig()
      {
        BootstrapServers = this.BootstrapServers
      };
    }

    public ConsumerConfig GetConsumerConfig()
    {
      return new ConsumerConfig()
      {
        BootstrapServers = this.BootstrapServers, 
        EnableAutoCommit = false,
        EnableAutoOffsetStore = true,
        SessionTimeoutMs = 45000,
        AutoOffsetReset = AutoOffsetReset.Earliest
      };
    }



    public MessageDispatcherOptions()
    {
      SerializerSettings = new JsonSerializerSettings()
      {
        MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore,
        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
        DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
        DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc,
        TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All
      };
      SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
    }
  }
}