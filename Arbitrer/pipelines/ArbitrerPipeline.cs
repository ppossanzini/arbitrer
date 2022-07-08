using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Arbitrer.Pipelines
{

  public class ArbitrerPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
  {

    private readonly IArbitrer arbitrer;

    public ArbitrerPipeline(IArbitrer arbitrer)
    {
      this.arbitrer = arbitrer;
    }

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
      return arbitrer.GetLocation<TRequest>() switch
      {
        HandlerLocation.Local => await next().ConfigureAwait(false),
        HandlerLocation.Remote => await arbitrer.InvokeRemoteHandler<TRequest, TResponse>(request),
        _ => throw new InvalidHandlerException()
      };
    }
  }
}