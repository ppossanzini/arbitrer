using cqrs.models.Commands;
using cqrs.models.Dto;
using MediatR;

namespace receiver.handlers
{
  public class CommandHandler : IRequestHandler<MediatRRequest1, bool>,
    IRequestHandler<MediatRRequest2, ResponseDTO>,
    IRequestHandler<MediatRRequest3, IEnumerable<ResponseDTO>>,
    IRequestHandler<MediatRRequestWithHandlerException, bool>
  {
    public Task<ResponseDTO> Handle(MediatRRequest2 request, CancellationToken cancellationToken)
    {
      return Task.FromResult(new ResponseDTO());
    }

    public Task<bool> Handle(MediatRRequest1 request, CancellationToken cancellationToken)
    {
      return Task.FromResult(true);
    }

    public Task<bool> Handle(MediatRRequestWithHandlerException request, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public Task<IEnumerable<ResponseDTO>> Handle(MediatRRequest3 request, CancellationToken cancellationToken)
    {
      return Task.FromResult(new[] {new ResponseDTO()}.AsEnumerable());
    }
  }
}