using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Arbitrer
{
  public class Arbitrer : IArbitrer
  {
    private readonly ArbitrerOptions options;
    private readonly IExternalMessageDispatcher messageDispatcher;
    private readonly ILogger<Arbitrer> logger;

    public Arbitrer(IOptions<ArbitrerOptions> options, IExternalMessageDispatcher messageDispatcher, ILogger<Arbitrer> logger)
    {
      this.options = options.Value;
      this.messageDispatcher = messageDispatcher;
      this.logger = logger;
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
      logger.LogDebug($"Invoking remote handler for: {typeof(TRequest).FullName}");
      var result = (Messages.ResponseMessage) await messageDispatcher.Dispatch<TRequest, TResponse>(request);
      logger.LogDebug($"Remote request for {typeof(TRequest).FullName} completed!");

      if (result.Status == Messages.StatusEnum.Exception)
      {
        if (typeof(JObject).IsAssignableFrom(result.Content.GetType()))
          throw (result.Content as JObject).ToObject<Exception>();

        throw (result.Content ?? new Exception("Error executing remote command")) as Exception;
      }

      if (typeof(JObject).IsAssignableFrom(result.Content.GetType()))
        return (result.Content as JObject).ToObject<TResponse>();
      return (TResponse) result.Content;
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