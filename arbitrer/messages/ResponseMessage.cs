

namespace Arbitrer.Messages
{
  public class ResponseMessage<T>
  {
    public StatusEnum Status { get; set; }
    public T Content { get; set; }
  }


  public class ResponseMessage
  {
    public StatusEnum Status { get; set; }
    public object Content { get; set; }
  }

  public enum StatusEnum
  {
    Ok,
    Exception
  }
}