using Grpc.Net.Client;

namespace Arbitrer.GRPC;

public class RemoteServiceDefinition
{
  public string Uri { get; set; }
  public GrpcChannelOptions ChannelOptions { get; set; }
}