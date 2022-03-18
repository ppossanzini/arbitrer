
using Microsoft.Extensions.DependencyInjection;


namespace Arbitrer
{


  public class ArbitrerOptionsBuilder
  {
    internal IServiceCollection Service { get; set; }
    public ArbitrerOptions Options { get; set; }

  }

}