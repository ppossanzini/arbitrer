using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Arbitrer
{
  public interface IExternalMessageDispatcher
  {
    bool CanDispatch<TRequest>();

    Task<Messages.ResponseMessage<TResponse>> Dispatch<TRequest, TResponse>(TRequest request, string queueName, CancellationToken cancellationToken = default);

    Task Notify<TRequest>(TRequest request, string queueName, CancellationToken cancellationToken = default) where TRequest : INotification;
  }
}