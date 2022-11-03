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
      switch (arbitrer.GetLocation<TRequest>())
      {
        case HandlerLocation.Local : return await next().ConfigureAwait(false);
        case HandlerLocation.Remote : return await arbitrer.InvokeRemoteHandler<TRequest, TResponse>(request);
        default: throw new InvalidHandlerException();
      }
    }
  }
}