using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Arbitrer
{
  public class ArbitredMediatr : Mediator
  {
    private readonly IArbitrer _arbitrer;
    private bool allowRemoteRequest = true;

    public ArbitredMediatr(ServiceFactory serviceFactory, IArbitrer arbitrer) : base(serviceFactory)
    {
      _arbitrer = arbitrer;
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
      if (allowRemoteRequest && _arbitrer.HasRemoteHandler(notification.GetType()))
        await _arbitrer.SendRemoteNotification(notification);
      else
        await base.PublishCore(allHandlers, notification, cancellationToken);
    }
  }
}