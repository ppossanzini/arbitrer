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
    public string BootstrapServerHostName { get; set; }
    public string BootstrapServerPort { get; set; }
    public bool EnableAutoCommit { get; set; } = false;
    public int? AutoCommitIntervalMs { get; set; }
    public bool EnableAutoOffsetStore { get; set; } = true;

    public int? TopicPartition { get; set; }

    public JsonSerializerSettings SerializerSettings { get; set; }
    public ProducerConfig Producer { get; set; }
    public ConsumerConfig Consumer { get; set; }

    public string GroupdId { get; set; } = null;
    public Offset Offset { get; set; } = Offset.End;

    //public TopicPartition ProducerTopicPartition { get; set; }
    //public TopicPartition ConsumerTopicPartition { get; set; }
    //public IEnumerable<TopicPartition> ConsumerTopicsPartitions { get; set; }
    //public TopicPartitionOffset ConsumerTopicPartitionOffset { get; set; }
    //public IEnumerable<TopicPartitionOffset> ConsumerTopicsPartitionsOffset { get; set; }

    //#region PRODUCER SPECIFIC
    //public IEnumerable<int>? ProducerPartitions { get; set; }
    //public IEnumerable<TopicPartition>? ProducerTopicPartitions { get; set; }
    //#endregion


    //#region CONSUMER SPECIFIC
    //public string GroupdId { get; set; }
    //public int ConsumerTopicPartitionIndex { get; set; }
    //public IEnumerable<string>? TopicsNames { get; set; }
    //public IEnumerable<int>? ConsumerTopicsPartitionsIndexes { get; set; }
    //public Offset Offset { get; set; }
    //#endregion

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

      Producer = new ProducerConfig()
      {
        BootstrapServers = $"{BootstrapServerHostName}:{BootstrapServerPort}",
      };

      Consumer = new ConsumerConfig()
      {
        EnableAutoCommit = EnableAutoCommit,
        AutoCommitIntervalMs = AutoCommitIntervalMs,
        EnableAutoOffsetStore = EnableAutoOffsetStore,
        SessionTimeoutMs = 45000,
        AutoOffsetReset = AutoOffsetReset.Earliest,
      };

      //ProducerTopicPartition = new TopicPartition(TopicName, new Partition(TopicPartition ?? 0));

      //ProducerTopicPartitions = ProducerPartitions.Select(i => new TopicPartition(TopicName, new Partition(i)));

      //ConsumerTopicPartition = new TopicPartition(TopicName, new Partition(TopicPartition ?? 0));

      //ConsumerTopicsPartitions = TopicsNames.Select(i => new TopicPartition(i, new Partition(ConsumerTopicPartitionIndex)));

      //ConsumerTopicPartitionOffset = new TopicPartitionOffset(TopicName, ConsumerTopicPartitionIndex, Offset);

      //ConsumerTopicsPartitionsOffset = ConsumerTopicsPartitionsIndexes.Select(i => new TopicPartitionOffset(TopicName, i, Offset));
    }
  }

  //public class ProducerTopicOptions
  //{
  //  public string BootstrapServerHostName { get; set; }
  //  public string BootstrapServerPort { get; set; }
  //  public string TopicName { get; set; }
  //}

  //public class ProducerPartitionsOptions
  //{
  //  public string BootstrapServerHostName { get; set; }
  //  public string BootstrapServerPort { get; set; }
  //  public string TopicName { get; set; }
  //  public IEnumerable<int>? ProducerPartitions { get; set; }
  //  public IEnumerable<TopicPartition>? ProducerTopicPartitions { get; set; }
  //}

  //public class ConsumerTopicOptions
  //{
  //  public string BootstrapServerHostName { get; set; }
  //  public string BootstrapServerHostPort { get; set; }
  //  public string GroupdId { get; set; }
  //  public bool EnableAutoCommit { get; set; } = false;
  //  public string TopicName { get; set; }
  //  public Offset Offset { get; set; }
  //}

  //public class ConsumerTopicsOptions
  //{
  //  public string BootstrapServerHostName { get; set; }
  //  public string BootstrapServerHostPort { get; set; }
  //  public string GroupdId { get; set; }
  //  public bool EnableAutoCommit { get; set; } = false;
  //  public IEnumerable<string> TopicsNames { get; set; }
  //  public Offset Offset { get; set; }
  //}

  //public class ConsumerTopicPartitionOptions
  //{
  //  public string BootstrapServerHostName { get; set; }
  //  public string BootstrapServerHostPort { get; set; }
  //  public string GroupdId { get; set; }
  //  public bool EnableAutoCommit { get; set; } = false;
  //  public string TopicName { get; set; }
  //  public int TopicPartition { get; set; }
  //  public Offset Offset { get; set; }
  //}

  //public class ConsumerTopicsPartitionOptions
  //{
  //  public string BootstrapServerHostName { get; set; }
  //  public string BootstrapServerHostPort { get; set; }
  //  public string GroupdId { get; set; }
  //  public bool EnableAutoCommit { get; set; } = false;
  //  public IEnumerable<string> TopicsNames { get; set; }
  //  public int TopicPartition { get; set; }
  //  public Offset Offset { get; set; }
  //}
}