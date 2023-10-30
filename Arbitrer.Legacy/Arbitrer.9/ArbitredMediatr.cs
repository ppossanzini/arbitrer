using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arbitrer
{
  public class ArbitredMediatr : Mediator
  {
    private readonly IArbitrer arbitrer;
    private readonly ILogger<ArbitredMediatr> logger;
    private bool allowRemoteRequest = true;

    public ArbitredMediatr(ServiceFactory serviceFactory, IArbitrer arbitrer, ILogger<ArbitredMediatr> logger) : base(
      serviceFactory)
    {
      this.arbitrer = arbitrer;
      this.logger = logger;
    }
    
    public void StopPropagating()
    {
      allowRemoteRequest = false;
    }

    public void ResetPropagating()
    {
      allowRemoteRequest = true;
    }

    protected override async Task PublishCore(IEnumerable<Func<INotification, CancellationToken, Task>> allHandlers, INotification notification,
      CancellationToken cancellationToken)
    {
      try
      {
        if (allowRemoteRequest && arbitrer.HasRemoteHandler(notification.GetType()))
        {
          logger.LogDebug("Propagating: {Json}", JsonConvert.SerializeObject(notification));
          await arbitrer.SendRemoteNotification(notification);
        }
        else
          await base.PublishCore(allHandlers, notification, cancellationToken);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, ex.Message);
        throw;
      }
    }
  }
}