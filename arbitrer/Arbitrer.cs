using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MediatR;

namespace Arbitrer
{

  public class Arbitrer : IArbitrer
  {
    private readonly ArbitrerOptions options;
    private readonly IExternalMessageDispatcher messageDispatcher;
    public Arbitrer(ArbitrerOptions options, IExternalMessageDispatcher messageDispatcher)
    {
      this.options = options;
      this.messageDispatcher = messageDispatcher;
    }

    public bool HasLocalHandler<T>() where T : IBaseRequest => this.HasLocalHandler(typeof(T));
    public bool HasLocalHandler(Type t) => this.options.LocalRequests.Any(i => i == t);
    public bool HasRemoteHandler<T>() where T : IBaseRequest => this.HasRemoteHandler(typeof(T));
    public bool HasRemoteHandler(Type t) => this.options.RemoteRequests.Any(i => i == t);

    public HandlerLocation GetLocation<T>() where T : IBaseRequest => this.GetLocation(typeof(T));
    public HandlerLocation GetLocation(Type t)
    {
      return options.Behaviour switch
      {
        ArbitrerBehaviourEnum.ImplicitLocal => this.HasRemoteHandler(t) ? HandlerLocation.Remote : HandlerLocation.Local,
        ArbitrerBehaviourEnum.ImplicitRemote => this.HasLocalHandler(t) ? HandlerLocation.Local : HandlerLocation.Remote,
        _ => this.HasLocalHandler(t) ? HandlerLocation.Local : this.HasRemoteHandler(t) ? HandlerLocation.Remote : HandlerLocation.NotFound,
      };
    }

    public async Task<TResponse> InvokeRemoteHandler<TRequest, TResponse>(TRequest request) where TRequest : IRequest<TResponse>
    {
      return (TResponse)await messageDispatcher.Dispatch<TRequest, TResponse>(request);
      throw new InvalidOperationException("OnRemoteCall has not been configured.");
    }

    public IEnumerable<Type> GetLocalRequestsTypes() => options.LocalRequests;

    public IEnumerable<Type> GetRemoteRequestsTypes() => options.RemoteRequests;
  }

  public enum HandlerLocation
  {
    NotFound,
    Local,
    Remote,
  }
}