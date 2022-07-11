using System;

namespace Arbitrer
{
  [AttributeUsage(AttributeTargets.Class)]
  public class ArbitrerQueueNameAttribute : System.Attribute
  {
    public string Name { get; set; }
  }
}