using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Arbitrer
{

  public interface IExternalMessageDispatcher
  {
    Task<Messages.ResponseMessage> Dispatch<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) 
      where TRequest : IRequest<TResponse>;

  }
}