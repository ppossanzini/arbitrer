using MediatR;

namespace Arbitrer
{
  public class ExplicitQueueRequest<T> : IRequest, IExplicitQueue where T : IRequest
  {
    public T Message { get; set; }
    public string QueueName { get; set; }

    object IExplicitQueue.MessageObject
    {
      get => Message;
    }
  }

  public class ExplicitQueueRequest<T, TResponse> : IRequest<TResponse>, IExplicitQueue where T : IRequest<TResponse>
  {
    public T Message { get; set; }
    public string QueueName { get; set; }

    object IExplicitQueue.MessageObject
    {
      get => Message;
    }
  }
}