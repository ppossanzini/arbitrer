using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Arbitrer.RabbitMQ
{
  /// <summary>
  /// Represents the options for configuring the message dispatcher.
  /// </summary>
  public class MessageDispatcherOptions
  {
    /// <summary>
    /// Gets or sets the name of the host.
    /// </summary>
    public string HostName { get; set; }

    /// <summary>
    /// Gets or sets the user name. </summary> <value>
    /// The user name. </value>
    /// /
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    /// <value>
    /// The password.
    /// </value>
    public string Password { get; set; }

    /// <summary>
    /// Gets or sets the virtual host.
    /// </summary>
    /// <value>
    /// The virtual host.
    /// </value>
    public string VirtualHost { get; set; }

    /// <summary>
    /// Gets or sets the port number.
    /// </summary>
    /// <value>
    /// The port number.
    /// </value>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the name of the queue.
    /// </summary>
    /// <value>
    /// The name of the queue.
    /// </value>
    public string QueueName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the object should be automatically deleted.
    /// </summary>
    public bool AutoDelete = false;

    public uint  MaxMessageSize { get; set; } = 100 * 1024 * 1024;
    
    /// <summary>
    /// The Durable variable represents the durability status of an object.
    /// If Durable is set to true, it means the object is durable, otherwise, it is not.
    /// </summary>
    /// <remarks>
    /// Durability is a Capability that specifies whether an object is able to withstand wear, decay, or damage over time.
    /// Setting Durable to true indicates that the object is designed to be long-lasting and can resist various forms of deterioration.
    /// Conversely, setting Durable to false suggests that the object is not intended to have a long lifespan or may be susceptible to damage.
    /// </remarks>
    public bool Durable = true;

    public uint PerChannelQos { get; set; } = 0;
    public ushort PerConsumerQos { get; set; } = 1;

    public string ClientName { get; set; }
    
    /// <summary>
    /// Gets or sets the serializer settings for JSON serialization and deserialization.
    /// </summary>
    public JsonSerializerSettings SerializerSettings { get; set; }
    
    
    public HashSet<Type> DispatchOnly { get; private set; } = new HashSet<Type>();
    public HashSet<Type> DontDispatch { get; private set; } = new HashSet<Type>();
    
    /// Represents the options for message dispatcher.
    /// /
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