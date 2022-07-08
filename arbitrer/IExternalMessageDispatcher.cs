using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Arbitrer
{
  public interface IExternalMessageDispatcher
  {
    Task<Messages.ResponseMessage<TResponse>> Dispatch<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
      where TRequest : IRequest<TResponse>;

    Task Notify<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : INotification;
  }
}