using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Arbitrer
{
  public class ArbitredMediatr : Mediator
  {
    private readonly IArbitrer _arbitrer;

    public ArbitredMediatr(ServiceFactory serviceFactory, IArbitrer arbitrer) : base(serviceFactory)
    {
      _arbitrer = arbitrer;
    }

    protected override async Task PublishCore(IEnumerable<Func<INotification, CancellationToken, Task>> allHandlers, INotification notification,
      CancellationToken cancellationToken)
    {
      if (_arbitrer.GetLocation(notification.GetType()) == HandlerLocation.Remote)
        await _arbitrer.SendRemoteNotification(notification);
      
      await base.PublishCore(allHandlers, notification, cancellationToken);
    }
  }
}