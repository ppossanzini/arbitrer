using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Arbitrer.Pipelines
{
  public class ArbitrerPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
  {
    private readonly IArbitrer _arbitrer;
    private readonly ILogger<Arbitrer> _logger;

    public ArbitrerPipeline(IArbitrer arbitrer, ILogger<Arbitrer> logger)
    {
      this._arbitrer = arbitrer;
      _logger = logger;
    }

    // Implementation for legacy version for .netstandard 2.0 compatibility
    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
      switch (_arbitrer.GetLocation<TRequest>())
      {
        case HandlerLocation.Local: return await next().ConfigureAwait(false);
        case HandlerLocation.Remote:
          try
          {
            return await _arbitrer.InvokeRemoteHandler<TRequest, TResponse>(request);
          }
          catch (Exception e)
          {
            _logger.LogError(e, e.Message);
            throw;
          }
        default: throw new InvalidHandlerException();
      }
    }

    // Implementation for version > 11
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
      switch (_arbitrer.GetLocation<TRequest>())
      {
        case HandlerLocation.Local: return await next().ConfigureAwait(false);
        case HandlerLocation.Remote:
          try
          {
            return await _arbitrer.InvokeRemoteHandler<TRequest, TResponse>(request);
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, ex.Message);
            throw;
          }
        default: throw new InvalidHandlerException();
      }
    }
  }
}