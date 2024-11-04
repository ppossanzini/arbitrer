using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Arbitrer.Pipelines
{
  /// <summary>
  /// Represents a pipeline behavior that integrates with an arbitrator for handling requests of type <typeparamref name="TRequest"/> and producing responses of type <typeparamref name
  /// ="TResponse"/>.
  /// </summary>
  /// <typeparam name="TRequest">The type of the request.</typeparam>
  /// <typeparam name="TResponse">The type of the response.</typeparam>
  public class ArbitrerPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IBaseRequest
  //where TRequest : notnull
  {
    private readonly IArbitrer arbitrer;
    private readonly ILogger<Arbitrer> _logger;

    public ArbitrerPipeline(IArbitrer arbitrer, ILogger<Arbitrer> logger)
    {
      this.arbitrer = arbitrer;
      _logger = logger;
    }


    /// <summary>
    /// Handles the request by invoking the appropriate handler based on the location of the request.
    /// </summary>
    /// <typeparam name="TRequest">The type of request.</typeparam>
    /// <typeparam name="TResponse">The type of response.</typeparam>
    /// <param name="request">The request data.</param>
    /// <param name="next">The next request handler delegate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response data.</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
      if (typeof(IExplicitQueue).IsAssignableFrom(request.GetType()))
      {
        var queueName = ((IExplicitQueue)request).QueueName;
        var req = ((IExplicitQueue)request).MessageObject;
        var type = request.GetType().GetGenericArguments()[0];

        return ((TResponse)this.GetType().GetMethod(nameof(InvokeHandler), BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(type)
          .Invoke(this, new object[] { next, req, queueName }));
      }

      return await InvokeHandler(next, request, null);
    }

    private async Task<TResponse> InvokeHandler<T>(RequestHandlerDelegate<TResponse> next, T req, string queueName)
    {
      try
      {
        switch (arbitrer.GetLocation(req.GetType()))
        {
          case HandlerLocation.Local: return await next().ConfigureAwait(false);
          case HandlerLocation.Remote: return await arbitrer.InvokeRemoteHandler<T, TResponse>(req, queueName);
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