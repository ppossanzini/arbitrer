using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Arbitrer.Pipelines
{
  public class ArbitrerPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> //where TRequest : notnull
  {
    private readonly IArbitrer arbitrer;
    private readonly ILogger<Arbitrer> _logger;

    public ArbitrerPipeline(IArbitrer arbitrer, ILogger<Arbitrer> logger)
    {
      this.arbitrer = arbitrer;
      _logger = logger;
    }

    // Implementation for legacy version for .netstandard 2.0 compatibility
    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
      try
      {
        switch (arbitrer.GetLocation<TRequest>())
        {
          case HandlerLocation.Local: return await next().ConfigureAwait(false);
          case HandlerLocation.Remote: return await arbitrer.InvokeRemoteHandler<TRequest, TResponse>(request);
          default: throw new InvalidHandlerException();
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, ex.Message);
        throw;
      }
    }

    // Implementation for version > 11
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
      try
      {
        switch (arbitrer.GetLocation<TRequest>())
        {
          case HandlerLocation.Local: return await next().ConfigureAwait(false);
          case HandlerLocation.Remote: return await arbitrer.InvokeRemoteHandler<TRequest, TResponse>(request);
          default: throw new InvalidHandlerException();
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, ex.Message);
        throw;
      }
    }
  }
}