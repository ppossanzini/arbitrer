using System;

namespace Arbitrer
{
  [AttributeUsage(AttributeTargets.Class)]
  public class ArbitrerQueueTimeoutAttribute : System.Attribute
  {
    public int ConsumerTimeout { get; set; }
  }
}