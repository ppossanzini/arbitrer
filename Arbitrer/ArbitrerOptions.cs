using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;

namespace Arbitrer
{
  public class ArbitrerOptions
  {
    public string DefaultQueuePrefix { get; set; } = String.Empty;
    
    /// <summary>
    /// Gets or sets the behaviour of the arbitrer.
    /// </summary>
    /// <value>
    /// The behaviour of the arbitrer.
    /// </value>
    public ArbitrerBehaviourEnum Behaviour { get; set; } = ArbitrerBehaviourEnum.ImplicitLocal;

    /// <summary>
    /// Gets or sets the collection of local requests.
    /// </summary>
    /// <value>The local requests.</value>
    public HashSet<Type> LocalRequests { get; private set; } = new HashSet<Type>();

    /// <summary>
    /// Gets the set of remote requests supported by the application.
    /// </summary>
    public HashSet<Type> RemoteRequests { get; private set; } = new HashSet<Type>();

    /// <summary>
    /// Get the prefix of remote queue
    /// </summary>
    public Dictionary<string, string> QueuePrefixes { get; private set; } = new Dictionary<string, string>();
  }


  /// <summary>
  /// Specifies the possible behaviours of an arbitrator.
  /// </summary>
  public enum ArbitrerBehaviourEnum
  {
    ImplicitLocal,
    ImplicitRemote,
    Explicit
  }
}