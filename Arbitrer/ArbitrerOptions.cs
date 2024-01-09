using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;

namespace Arbitrer
{

  public class ArbitrerOptions
  {
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