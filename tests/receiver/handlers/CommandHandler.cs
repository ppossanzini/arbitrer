using cqrs.models.Commands;
using MediatR;

namespace receiver.handlers
{
  public class CommandHandler : IRequestHandler<MediatRRequest1, bool>
  {
    public Task<bool> Handle(MediatRRequest1 request, CancellationToken cancellationToken)
    {
      return Task.FromResult(true);
    }
  }
}