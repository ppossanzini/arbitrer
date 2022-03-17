using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Arbitrer
{

  public interface IExternalMessageDispatcher
  {
    Task<TResponse> Dispatch<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse>;

  }
}