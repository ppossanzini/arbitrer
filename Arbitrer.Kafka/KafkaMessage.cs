using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace Arbitrer.Kafka
{
  public class KafkaMessage<T>
  {
    [JsonProperty(ItemTypeNameHandling = TypeNameHandling.All)]
    public T Message { get; set; }

    public string CorrelationId { get; set; }
    public string ReplyTo { get; set; }
  }

  public class KafkaReply
  {
    public object Reply { get; set; }
    public string CorrelationId { get; set; }
  }
  
  public class KafkaReply<T>
  {
    public T Reply { get; set; }
    public string CorrelationId { get; set; }
  }
}