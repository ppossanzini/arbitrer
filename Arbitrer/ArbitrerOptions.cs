using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;

namespace Arbitrer
{

  public class ArbitrerOptions
  {

    public ArbitrerBehaviourEnum Behaviour { get; set; } = ArbitrerBehaviourEnum.ImplicitLocal;

    internal HashSet<Type> LocalRequests { get; private set; } = new HashSet<Type>();
    internal HashSet<Type> RemoteRequests { get; private set; } = new HashSet<Type>();
  }


  public enum ArbitrerBehaviourEnum
  {
    ImplicitLocal,
    ImplicitRemote,
    Explicit
  }


}