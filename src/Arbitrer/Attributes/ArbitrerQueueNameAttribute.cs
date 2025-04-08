using System;

namespace Arbitrer
{
  /// <summary>
  /// Represents an attribute used to specify the name of the Arbitrer queue for a class.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  public class ArbitrerQueueNameAttribute : System.Attribute
  {
    public string Name { get; set; }
  }
}